using System;

namespace Dem0n13.Utils
{
    public sealed class PoolToken<T>
        where T : IPoolable<T>
    {
        private readonly T _obj;
        private readonly Pool<T> _pool;

        private bool _inPool;
        
        public PoolToken(IPoolable<T> obj, Pool<T> pool)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            
            _obj = (T) obj;
            _pool = pool;
            _inPool = pool == null; // prevents resurrection, if object created "out of pool"
        }

        #region For Pool<T> access

        internal bool TryGetStatus(Pool<T> pool, out bool inPool)
        {
            inPool = _inPool;
            return pool == _pool;
        }

        internal void SetStatus(bool inPool)
        {
            _inPool = inPool;
        }

        #endregion

        ~PoolToken()
        {
            if (!_inPool)
            {
                GC.ReRegisterForFinalize(this);
                GC.ReRegisterForFinalize(_obj);
                _pool.ReleaseUnsafe(_obj);
            }
        }
    }
}