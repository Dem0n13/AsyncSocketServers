using System;

namespace Dem0n13.Utils
{
    public sealed class PoolToken<T>
        where T : IPoolable<T>
    {
        private readonly int _id = IdGenerator<T>.Current.GetNext();
        private readonly T _obj;
        private readonly Pool<T> _pool;

        private bool _canResurrect;
        
        public PoolToken(IPoolable<T> obj, Pool<T> pool)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            
            _obj = (T) obj;
            _pool = pool;
            _canResurrect = pool != null;
        }

        #region For Pool<T> access

        internal bool TryGetStatus(Pool<T> pool, out bool inPool)
        {
            inPool = !_canResurrect;
            return pool == _pool;
        }

        internal void SetStatus(bool inPool)
        {
            _canResurrect = !inPool;
        }

        #endregion

        ~PoolToken()
        {
            if (_canResurrect)
            {
                GC.ReRegisterForFinalize(this);
                GC.ReRegisterForFinalize(_obj);
                _pool.ReleaseUnsafe(_obj);
            }
            else
            {
                IdGenerator<T>.Current.Release(_id);
            }
        }
    }
}