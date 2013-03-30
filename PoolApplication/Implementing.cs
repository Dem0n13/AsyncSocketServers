using System;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace PoolApplication
{
    public class Implementing : SocketAsyncEventArgs, IPoolable
    {
        public bool InPool { get; set; }
    }

    public class ImplementingPool : Pool<Implementing>
    {
        private readonly int _bufferSize;
        
        public ImplementingPool(int bufferSize, int initialCount, int maxCapacity)
            : base(maxCapacity)
        {
            _bufferSize = bufferSize;
            TryAllocatePush(initialCount);
        }

        protected override Implementing ObjectConstructor()
        {
            var item = new Implementing();
            item.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            return item;
        }

        protected override void CleanUp(Implementing item)
        {
            Array.Clear(item.Buffer, 0, _bufferSize);
        }
    }
}