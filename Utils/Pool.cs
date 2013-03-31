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
    {
        private readonly ConcurrentStack<PoolSlot<T>> _storage; // storing objects "in pool"
        private readonly ConcurrentDictionary<int, bool> _registry; // storing all objects' ids and their statuses (true - "in pool", otherwise - false)
        private readonly LockFreeSemaphore _ioSemaphore; // ligth semaphore for push/pop operations
        private readonly LockFreeSemaphore _allocSemaphore; // light semaphore for allocate operations

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> with specified upper limit.
        /// </summary>
        /// <param name="maxCapacity"></param>
        protected Pool(int maxCapacity)
        {
            if (maxCapacity < 1)
                throw new ArgumentOutOfRangeException("maxCapacity", "Max capacity must be greater than 0");

            _storage = new ConcurrentStack<PoolSlot<T>>();
            _registry = new ConcurrentDictionary<int, bool>();
            _ioSemaphore = new LockFreeSemaphore(0, maxCapacity);
            _allocSemaphore = new LockFreeSemaphore(maxCapacity, maxCapacity);
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
            get { return _registry.Count; }
        }

        /// <summary>
        /// Puts the object back to the pool.
        /// </summary>
        /// <param name="slot">The object to return</param>
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        public void Release(PoolSlot<T> slot)
        {
            if (slot == null)
                throw new ArgumentNullException("slot");
            bool inPool;
            if (!_registry.TryGetValue(slot.Id, out inPool))
                throw new ArgumentException("Specified object is not from this pool", "slot");
            if (inPool)
                throw new InvalidOperationException("Specified object is already in the pool");
            
            ReleaseUnsafe(slot);
        }

        /// <summary>
        /// Puts the object without any checks back to the pool.
        /// Only for usage by instances of <see cref="PoolSlot{T}"/>
        /// </summary>
        /// <param name="slot"> </param>
        internal void ReleaseUnsafe(PoolSlot<T> slot)
        {
            CleanUp(slot.Object);
            Push(slot);
        }

        /// <summary>
        /// Gets available object from pool or creates new one.
        /// </summary>
        /// <returns>Pool slot</returns>
        public PoolSlot<T> Take()
        {
            PoolSlot<T> item;
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
            while (_ioSemaphore.CurrentCount != _registry.Count)
                Wait();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}/{2}/{3}", GetType().Name, _ioSemaphore.CurrentCount,
                                 _registry.Count, _ioSemaphore.MaxCount);
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
                Push(Allocate());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to create, register with status "Out of pool" and return a new instance of <see cref="T"/>
        /// </summary>
        /// <param name="slot"></param>
        /// <returns>true if the operation was successfully, otherwise, false</returns>
        protected bool TryAllocatePop(out PoolSlot<T> slot)
        {
            if (_allocSemaphore.TryTake())
            {
                slot = Allocate();
                _registry[slot.Id] = false;
                return true;
            }

            slot = default(PoolSlot<T>);
            return false;
        }

        /// <summary>
        /// Waits for a free slot
        /// </summary>
        /// <returns>Pool slot</returns>
        protected PoolSlot<T> WaitPop()
        {
            PoolSlot<T> slot;
            while (!TryPop(out slot))
                Wait();
            return slot;
        }

        /// <summary>
        /// Provides a delay for other pool operations
        /// </summary>
        protected void Wait()
        {
            if (!Thread.Yield())
                Thread.Sleep(100);
        }

        private PoolSlot<T> Allocate()
        {
            var @object = ObjectConstructor();
            return new PoolSlot<T>(@object);
        }

        #endregion

        #region Storage wrappers

        private void Push(PoolSlot<T> item)
        {
            _registry[item.Id] = true;
            _storage.Push(item);
            _ioSemaphore.Release();
        }

        private bool TryPop(out PoolSlot<T> item)
        {
            if (_ioSemaphore.TryTake())
            {
                _storage.TryPop(out item);
                _registry[item.Id] = false;
                return true;
            }
            item = default(PoolSlot<T>);
            return false;
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
    }
}
