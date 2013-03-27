namespace Dem0n13.Utils
{
    public abstract class PoolObject<T> : IPoolable<T>
        where T : IPoolable<T>
    {
        private readonly PoolToken<T> _poolToken;
        PoolToken<T> IPoolable<T>.PoolToken { get { return _poolToken; } }

        protected PoolObject(Pool<T> pool)
        {
            _poolToken = new PoolToken<T>(this, pool);
        }
    }
}