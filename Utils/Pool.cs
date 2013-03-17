using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        where T : class
    {
        private readonly ConcurrentStack<T> _storage; // storing objects "in pool"
        private readonly Dictionary<T, bool> _isInPool; // storing all objects and their states (true - "in pool", otherwise - false)
        private int _currentCount; // current object count "in pool" (perfomance _storage.Count improvement)

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
            get { return _isInPool.Count; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/>.
        /// </summary>
        protected Pool()
        {
            _storage = new ConcurrentStack<T>();
            _isInPool = new Dictionary<T, bool>();
        }

        /// <summary>
        /// Puts the object back to the <see cref="Pool{T}"/>.
        /// </summary>
        /// <param name="item">The object to return</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public void Release(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            bool isInPool;
            if (!_isInPool.TryGetValue(item, out isInPool))
                throw new ArgumentException("This object is not from this pool", "item");
            if (isInPool)
                throw new InvalidOperationException("This object is already in the pool");
            
            CleanUp(item);
            Push(item);
        }

        /// <summary>
        /// Gets available object from pool or creates new one.
        /// </summary>
        /// <returns>Taken from the pool object</returns>
        public T Take()
        {
            T item;
            return TryPop(out item) ? item : Allocate();
        }

        /// <summary>
        /// Waits for the pool to releasing all objects.
        /// Ensures that all objects are release before returning.
        /// </summary>
        public void WaitAll()
        {
            while (_currentCount != _isInPool.Count)
                if (!Thread.Yield())
                    Thread.Sleep(100);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}", GetType().Name, _currentCount, _isInPool.Count);
        }

        /// <summary>
        /// Creates and adds the specified count of objects to pool.
        /// </summary>
        /// <param name="count">Count of objects to add</param>
        protected void Allocate(int count)
        {
            for (var i = 0; i < count; i++)
                Push(Allocate());
        }

        /// <summary>
        /// Creates, adds to pool and returns from pool one object 
        /// </summary>
        /// <returns>Taken from the pool new object</returns>
        protected T Allocate()
        {
            var item = CreateNew();
            _isInPool.Add(item, false);
            return item;
        }

        #region For overiding

        /// <summary>
        /// Initializes a new object, ready to be placed in the pool
        /// </summary>
        /// <returns>The initialized object</returns>
        protected abstract T CreateNew();

        /// <summary>
        /// Provides clean up of the object before returning to the pool
        /// </summary>
        /// <param name="item">Objects</param>
        protected virtual void CleanUp(T item)
        {
        }

        #endregion

        #region Storage wrapper to optimize the counting CurrentCount and states

        private bool TryPop(out T item)
        {
            if (_storage.TryPop(out item))
            {
                Interlocked.Decrement(ref _currentCount);
                _isInPool[item] = false;
                return true;
            }
            return false;
        }

        private void Push(T item)
        {
            _isInPool[item] = true;
            _storage.Push(item);
            Interlocked.Increment(ref _currentCount);
        }

        #endregion
    }
}
