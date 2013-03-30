namespace Dem0n13.Utils
{
    public abstract class PoolObject : IPoolable
    {
        bool IPoolable.InPool { get; set; }
    }
}
