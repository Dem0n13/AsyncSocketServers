using System;

namespace Dem0n13.Utils
{
    public sealed class PoolSlot<T> : IDisposable
    {
        private readonly T _object;
        private readonly Pool<T> _pool; 

        private bool _inPool;

        #region Only for Pool<T> usage

        /// <summary>
        /// Initializes a new instance of the <see cref="PoolSlot{T}"/> for specified object and pool.
        /// </summary>
        /// <param name="object"></param>
        /// <param name="pool"></param>
        internal PoolSlot(T @object, Pool<T> pool)
        {
            _object = @object;
            _pool = pool;
        }
        
        /// <summary>
        /// Gets value of availability flag and checks pool.
        /// </summary>
        /// <param name="pool">Slot's pool</param>
        /// <returns>true, if item "in pool", overwise, false</returns>
        internal bool GetStatus(Pool<T> pool)
        {
            if (_pool != pool)
                throw new ArgumentException("This slot not for specified pool", "pool");
            return _inPool;
        }

        /// <summary>
        /// Sets value of availability flag.
        /// </summary>
        /// <param name="inPool">true, if item "in pool", overwise, false</param>
        internal void SetStatus(bool inPool)
        {
            _inPool = inPool;
        }

        #endregion

        /// <summary>
        /// Original stored object
        /// </summary>
        public T Object
        {
            get { return _object; }
        }

        /// <summary>
        /// Returns object's slot back to pool
        /// </summary>
        public void Dispose()
        {
            _pool.Release(this);
        }
    }
}
