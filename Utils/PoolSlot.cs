using System;

namespace Dem0n13.Utils
{
    public sealed class PoolSlot<T> : IUnique<T> 
    {
        private readonly int _id = IdGenerator<T>.Current.GetNext();
        private readonly T _object;
        
        public PoolSlot(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            
            _object = obj;
        }

        public int Id { get { return _id; } }

        public T Object
        {
            get { return _object; }
        }

        ~PoolSlot()
        {
            IdGenerator<T>.Current.Release(_id);
        }
    }
}