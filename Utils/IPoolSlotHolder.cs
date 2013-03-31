namespace Dem0n13.Utils
{
    public interface IPoolSlotHolder<T>
    {
        /// <summary>
        /// Object's slot for <see cref="Pool{T}"/> instance usage
        /// </summary>
        PoolSlot<T> PoolSlot { get; set; }
    }
}