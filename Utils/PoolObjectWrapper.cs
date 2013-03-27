namespace Dem0n13.Utils
{
    public sealed class PoolObjectWrapper<T> : IPoolable<PoolObjectWrapper<T>>
    {
        private readonly PoolToken<PoolObjectWrapper<T>> _poolToken;
        private readonly T _obj;
        
        public PoolToken<PoolObjectWrapper<T>> PoolToken { get { return _poolToken; } }
        public T Object { get { return _obj; } }

        public PoolObjectWrapper(T obj)
            : this(obj, null)
        {
        }

        public PoolObjectWrapper(T obj, Pool<PoolObjectWrapper<T>> pool)
        {
            _obj = obj;
            _poolToken = new PoolToken<PoolObjectWrapper<T>>(this, pool);
        }
    }
}