namespace Dem0n13.Utils
{
    /// <summary>
    /// Provides the set of options to help optimize some operations of the <see cref="Pool{T}"/> instance.
    /// </summary>
    public enum PoolReleasingMethod
    {
        /// <summary>
        /// The assumption that the items will be returned to the pool automatically by garbage collection.
        /// </summary>
        Auto,

        /// <summary>
        /// The assumption that the items will be returned by the programmer explicity using <see cref="Pool{T}.Release"/>.
        /// </summary>
        Manual
    }
}