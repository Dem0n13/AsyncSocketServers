namespace Dem0n13.Utils
{
    public interface IPoolable<T>
        where T : IPoolable<T>
    {
        PoolToken<T> PoolToken { get; }
    }
}