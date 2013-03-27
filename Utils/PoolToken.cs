using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;

namespace Dem0n13.Utils
{
    public sealed class PoolToken<T> : CriticalFinalizerObject, IUnique<T> 
        where T : IPoolable<T>
    {
        private readonly int _id = IdGenerator<T>.Current.GetNext();
        private readonly T _obj;
        private readonly Pool<T> _pool;

        private bool _inPool;
        private bool _canResurrect;
        
        public PoolToken(IPoolable<T> obj, Pool<T> pool)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            
            _obj = (T) obj;
            _pool = pool;
            _canResurrect = pool != null;
        }

        public int Id { get { return _id; } }

        #region For Pool<T> access

        internal void Cancel()
        {
            _canResurrect = false;
        }

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
            Debug.WriteLine(_canResurrect);
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