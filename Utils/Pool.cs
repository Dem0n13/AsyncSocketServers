using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentStack<T> _storage; // storing objects "in pool"
        private readonly LockFreeSemaphore _ioSemaphore; // ligth semaphore for push/pop operations
        private readonly LockFreeSemaphore _allocSemaphore; // light semaphore for allocate operations
        private readonly PoolReleasingMethod _releasingMethod;
        
        private volatile bool _isReleasingAllowed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> with specified upper limit.
        /// </summary>
        /// <param name="maxCapacity"></param>
        protected Pool(int maxCapacity)
            : this(maxCapacity, PoolReleasingMethod.Auto)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> with specified upper limit and the selected method of item returning.
        /// </summary>
        protected Pool(int maxCapacity, PoolReleasingMethod releasingMethod)
        {
            if (maxCapacity < 1)
                throw new ArgumentOutOfRangeException("maxCapacity", "Max capacity must be greater than 0");
            if (!Enum.IsDefined(typeof (PoolReleasingMethod), releasingMethod))
                throw new ArgumentOutOfRangeException("releasingMethod");

            _storage = new ConcurrentStack<T>();
            _ioSemaphore = new LockFreeSemaphore(0, maxCapacity);
            _allocSemaphore = new LockFreeSemaphore(maxCapacity, maxCapacity);
            _releasingMethod = releasingMethod;
            _isReleasingAllowed = true;
        }

        #region Public and internal members

        /// <summary>
        /// Gets the current number of the <see cref="T"/> in pool.
        /// </summary>
        public int CurrentCount
        {
            get { return _ioSemaphore.CurrentCount; }
        }

        /// <summary>
        /// Gets the number of the <see cref="T"/>, ever created in pool.
        /// </summary>
        public int TotalCount
        {
            get { return _allocSemaphore.MaxCount - _allocSemaphore.CurrentCount; }
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
            bool inPool;
            if (!item.PoolToken.TryGetStatus(this, out inPool))
                throw new ArgumentException("Specified object is not from this pool", "item");
            if (inPool)
                throw new InvalidOperationException("Specified object is already in the pool");
            
            ReleaseUnsafe(item);
        }

        /// <summary>
        /// Puts the object without any checks back to the pool.
        /// Only for usage by instances of <see cref="PoolToken{T}"/>
        /// </summary>
        /// <param name="item"> </param>
        internal void ReleaseUnsafe(T item)
        {
            if (_isReleasingAllowed)
            {
                CleanUp(item);
                Push(item);
            }
            else
            {
                Unregister(item.PoolToken);
            }
        }

        /// <summary>
        /// Gets available object from pool or creates new one.
        /// </summary>
        /// <returns>Pool item</returns>
        public T Take()
        {
            T item;
            if (TryPop(out item))
                return item;

            if (TryAllocatePop(out item))
                return item;

            return WaitPop();
        }

        /// <summary>
        /// Waits for the pool to releasing all objects.
        /// Ensures that all objects are release before returning.
        /// </summary>
        public void WaitAll()
        {
            while (_ioSemaphore.CurrentCount != TotalCount)
                Wait();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}/{3}", GetType().Name, _ioSemaphore.CurrentCount,
                                 TotalCount, _ioSemaphore.MaxCount);
        }

        #endregion

        #region Pool operations

        /// <summary>
        /// Attempts to create and adds the specified number of instances of <see cref="T"/> to the pool.
        /// </summary>
        /// <param name="count">Count of objects to add</param>
        /// <returns>true if the operation was successfull, otherwise, false</returns>
        protected bool TryAllocatePush(int count)
        {
            for (var i = 0; i < count; i++)
                if (!TryAllocatePush())
                    return false;
            return true;
        }

        /// <summary>
        /// Attempts to create and adds a new instance of <see cref="T"/> to the pool.
        /// </summary>
        /// <returns>true if the operation was successfull, otherwise, false</returns>
        protected bool TryAllocatePush()
        {
            if (_allocSemaphore.TryTake())
            {
                Push(ObjectConstructor());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to create, register with status "Out of pool" and return a new instance of <see cref="T"/>
        /// </summary>
        /// <param name="item"></param>
        /// <returns>true if the operation was successfully, otherwise, false</returns>
        protected bool TryAllocatePop(out T item)
        {
            if (_allocSemaphore.TryTake())
            {
                item = ObjectConstructor();
                return true;
            }

            item = default(T);
            return false;
        }

        /// <summary>
        /// Waits for a free item
        /// </summary>
        /// <returns>Pool item</returns>
        protected T WaitPop()
        {
            T item;
            while (!TryPop(out item))
                Wait();
            return item;
        }

        /// <summary>
        /// Provides a delay for other pool operations
        /// </summary>
        protected void Wait()
        {
            switch (_releasingMethod)
            {
                case PoolReleasingMethod.Auto:
                    GC.Collect();
                    Thread.Sleep(100);
                    break;
                case PoolReleasingMethod.Manual:
                    if (!Thread.Yield())
                        Thread.Sleep(100);
                    break;
            }
        }

        #endregion

        #region Storage wrappers

        private void Push(T item)
        {
            item.PoolToken.SetStatus(true);
            _storage.Push(item);
            _ioSemaphore.Release();
        }

        private bool TryPop(out T item)
        {
            if (_ioSemaphore.TryTake())
            {
                _storage.TryPop(out item);
                item.PoolToken.SetStatus(false);
                return true;
            }
            item = default(T);
            return false;
        }


        private void Unregister(PoolToken<T> token)
        {
            token.Cancel();
            _allocSemaphore.Release();
        }

        #endregion

        #region For overriding

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
            Wait();
            
            T item;
            while (TryPop(out item))
                Unregister(item.PoolToken);
        }
    }
}
