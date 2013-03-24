using System;
using System.Runtime.ConstrainedExecution;

namespace Dem0n13.Utils
{
    public sealed class PoolToken<T> : CriticalFinalizerObject, IUnique<T> 
        where T : IPoolable<T>
    {
        private readonly int _id = IdGenerator<T>.Current.GetNext();
        private readonly T _obj;
        private readonly Pool<T> _pool;
        private bool _cancelled;
        
        public PoolToken(IPoolable<T> obj, Pool<T> pool)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            if (pool == null)
                throw new ArgumentNullException("pool");

            _obj = (T) obj;
            _pool = pool;
        }

        public int Id { get { return _id; } }

        #region For Pool<T> access

        internal void Cancel()
        {
            _cancelled = true;
        }

        #endregion

        ~PoolToken()
        {
            if (_cancelled)
            {
                IdGenerator<T>.Current.Release(_id);
            }
            else
            {
                GC.ReRegisterForFinalize(this);
                GC.ReRegisterForFinalize(_obj);
                _pool.ReleaseUnsafe(_obj);
            }
        }
    }
}