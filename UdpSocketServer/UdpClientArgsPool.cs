using System;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public sealed class UdpClientArgsPool : Pool<UdpClientArgs>
    {
        private readonly int _bufferSize;

        public UdpClientArgsPool(int bufferSize, int initialCount, int maxCapacity)
            : base(maxCapacity, PoolReleasingMethod.Manual)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", "The buffer size must be greater than 0");
            if (initialCount < 0 || maxCapacity < initialCount)
                throw new ArgumentOutOfRangeException("initialCount", "The initial count has invalid value");
            
            _bufferSize = bufferSize;
            TryAllocatePush(initialCount);
        }

        protected override void CleanUp(UdpClientArgs item)
        {
            item.UTF8Message = null;
        }

        protected override UdpClientArgs ObjectConstructor()
        {
            return new UdpClientArgs(_bufferSize, this);
        }
    }
}
