using System;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public sealed class TcpClientArgsPool : Pool<AsyncClientArgs>
    {
        private readonly int _bufferSize;
        private readonly EventHandler<SocketAsyncEventArgs> _ioCompleted;

        public TcpClientArgsPool(int bufferSize, EventHandler<SocketAsyncEventArgs> ioCompleted, int initialCount, int maxCapacity)
            : base(maxCapacity, PoolReleasingMethod.Manual)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException("bufferSize", "The buffer size must be greater than 0");
            if (ioCompleted == null)
                throw new ArgumentNullException("ioCompleted");
            if (initialCount < 0 || maxCapacity < initialCount)
                throw new ArgumentOutOfRangeException("initialCount", "The initial count has invalid value");
            
            _bufferSize = bufferSize;
            _ioCompleted = ioCompleted;
            TryAllocatePush(initialCount);
        }

        protected override AsyncClientArgs ObjectConstructor()
        {
            var args = new AsyncClientArgs(_bufferSize, this);
            args.Completed += _ioCompleted;
            return args;
        }

        protected override void CleanUp(AsyncClientArgs item)
        {
            item.UTF8Message = null;
            item.AcceptSocket = null;
        }
    }
}