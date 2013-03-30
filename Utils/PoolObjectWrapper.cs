namespace Dem0n13.Utils
{
    public sealed class PoolObjectWrapper<T> : PoolObject
    {
        private readonly T _object;

        public T Object
        {
            get { return _object; }
        }

        public PoolObjectWrapper(T @object)
        {
            _object = @object;
        }
    }
}
