using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides a thread safely pool of re-usable objects.
    /// Controls uniqueness of all objects in pool.
    /// Recommended to override GetHashCode () and Equals (object) class of stored objects in order to improve efficiency.
    /// </summary>
    /// <typeparam name="T">Type of stored objects</typeparam>
    public abstract class Pool<T>
        where T : IPoolable<T>
    {
        private readonly ConcurrentStack<PoolToken<T>> _tokens; // storing tokens "in pool"
        private readonly ConcurrentDictionary<int, bool> _states; // storing all objects' ids and their states (true - "in pool", otherwise - false)
        private readonly PoolReleasingMethod _releasingMethod;
        private int _currentCount; // current object count "in pool" (perfomance _storage.Count improvement)
        private volatile bool _isReleasingAllowed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/>.
        /// </summary>
        protected Pool(PoolReleasingMethod releasingMethod = PoolReleasingMethod.Default)
        {
            if (!Enum.IsDefined(typeof (PoolReleasingMethod), releasingMethod))
                throw new ArgumentOutOfRangeException("releasingMethod");

            _tokens = new ConcurrentStack<PoolToken<T>>();
            _states = new ConcurrentDictionary<int, bool>();
            _releasingMethod = releasingMethod;
            _isReleasingAllowed = true;
        }

        #region Public and internal members

        /// <summary>
        /// Gets the current number of the <see cref="T"/> in pool.
        /// </summary>
        public int CurrentCount
        {
            get { return _currentCount; }
        }

        /// <summary>
        /// Gets the number of the <see cref="T"/>, ever created in pool.
        /// </summary>
        public int TotalCount
        {
            get { return _states.Count; }
        }

        /// <summary>
        /// Puts the object back to the pool.
        /// </summary>
        /// <param name="item">The object to return</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public void Release(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            var token = item.PoolToken;
            bool inPool;
            if (!_states.TryGetValue(token.Id, out inPool))
                throw new ArgumentException("Specified object is not from this pool", "item");
            if (inPool)
                throw new InvalidOperationException("Specified object is already in the pool");
            
            Push(token);
        }

        /// <summary>
        /// Puts the object with specified <see cref="PoolToken{T}"/> back to the pool.
        /// Only for usage by instance of <see cref="PoolToken{T}"/>
        /// </summary>
        /// <param name="token">The token of the poolable object to put</param>
        internal void Release(PoolToken<T> token)
        {
            Debug.Assert(token != null);

            if (_isReleasingAllowed)
            {
                CleanUp(token.Object);
                Push(token);
            }
            else
            {
                Unregister(token);
            }
        }

        /// <summary>
        /// Gets available object from pool or creates new one.
        /// </summary>
        /// <returns>Taken from the pool object</returns>
        public T Take()
        {
            PoolToken<T> item;
            return TryPop(out item) ? item.Object : AllocatePop();
        }

        /// <summary>
        /// Waits for the pool to releasing all objects.
        /// Ensures that all objects are release before returning.
        /// </summary>
        public void WaitAll()
        {
            while (_currentCount != _states.Count)
                Wait();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}", GetType().Name, _currentCount, _states.Count);
        }

        #endregion

        #region Pool operations

        /// <summary>
        /// Creates and adds the specified count of instances of <see cref="T"/> to the pool.
        /// </summary>
        /// <param name="count">Count of objects to add</param>
        protected void AllocatePush(int count)
        {
            for (var i = 0; i < count; i++)
                AllocatePush();
        }

        /// <summary>
        /// Creates and adds a new instance of <see cref="T"/> to the pool.
        /// </summary>
        protected void AllocatePush()
        {
            var item = ObjectConstructor();
            Push(item.PoolToken);
        }

        /// <summary>
        /// Creates, registers "out of the pool" and returns a new instance of <see cref="T"/>.
        /// </summary>
        /// <returns>Created pooled object</returns>
        protected T AllocatePop()
        {
            var item = ObjectConstructor();
            Register(item.PoolToken);
            return item;
        }

        /// <summary>
        /// Try to remove and unregister one object from the pool.
        /// </summary>
        /// <returns>true if an element was removed and unregistered from the pool successfully; otherwise, false.</returns>
        protected bool TryPopUnregister()
        {
            PoolToken<T> token;
            return TryPop(out token) && Unregister(token);
        }

        /// <summary>
        /// Provides a delay 
        /// </summary>
        public void Wait()
        {
            Debug.WriteLine("Wait() " + _releasingMethod);
            switch (_releasingMethod)
            {
                case PoolReleasingMethod.Manual:
                    if (!Thread.Yield())
                        Thread.Sleep(100);
                    break;
                case PoolReleasingMethod.Default:
                case PoolReleasingMethod.Auto:
                    GC.Collect();
                    Thread.Sleep(100);
                    break;
            }
        }

        #endregion

        #region Tokens processing

        private void Push(PoolToken<T> token)
        {
            _states[token.Id] = true;
            _tokens.Push(token);
            Interlocked.Increment(ref _currentCount);
        }

        private bool TryPop(out PoolToken<T> token)
        {
            if (_tokens.TryPop(out token))
            {
                _states[token.Id] = false;
                Interlocked.Decrement(ref _currentCount);
                return true;
            }
            return false;
        }

        private void Register(PoolToken<T> token)
        {
            _states.TryAdd(token.Id, false);
        }

        private bool Unregister(PoolToken<T> token)
        {
            token.Cancel();
            bool state;
            return _states.TryRemove(token.Id, out state);
        }

        #endregion

        #region For overiding

        /// <summary>
        /// Initializes a new object, ready to be placed in the pool
        /// </summary>
        /// <returns>The initialized object</returns>
        protected abstract T ObjectConstructor();

        /// <summary>
        /// Provides clean up of the object before returning to the pool
        /// </summary>
        /// <param name="item">Objects</param>
        protected virtual void CleanUp(T item)
        {
        }

        #endregion

        ~Pool()
        {
            _isReleasingAllowed = false;
            
            while (TryPopUnregister())
            {
            }
        }
    }
}
