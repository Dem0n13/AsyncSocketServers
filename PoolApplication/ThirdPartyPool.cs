using System;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace PoolApplication
{
    public class ThirdPartyPool : Pool<SocketAsyncEventArgs>
    {
        private readonly int _bufferSize;

        public ThirdPartyPool(int bufferSize, int initialCount, int maxCapacity)
            : base(maxCapacity)
        {
            if (initialCount > maxCapacity)
                throw new IndexOutOfRangeException();

            _bufferSize = bufferSize;
            TryAllocatePush(initialCount);
        }

        protected override SocketAsyncEventArgs ObjectConstructor()
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            return args;
        }

        protected override void CleanUp(SocketAsyncEventArgs @object)
        {
            Array.Clear(@object.Buffer, 0, _bufferSize);
        }
    }
}
