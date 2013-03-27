using System;
using System.Net;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public sealed class UdpClientArgsPool : Pool<AsyncClientArgs>
    {
        private readonly int _bufferSize;
        private readonly EventHandler<SocketAsyncEventArgs> _ioCompleted;

        public UdpClientArgsPool(int bufferSize, EventHandler<SocketAsyncEventArgs> ioCompleted, int initialCount, int maxCapacity)
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
            args.RemoteEndPoint = new IPEndPoint(0L, 0);
            args.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            args.AcceptSocket.Shutdown(SocketShutdown.Receive);
            return args;
        }

        protected override void CleanUp(AsyncClientArgs item)
        {
            item.UTF8Message = null;
        }
    }
}
