using System;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public sealed class TcpClientArgsPool : Pool<TcpClientArgs>
    {
        private readonly int _bufferSize;
        private readonly EventHandler<SocketAsyncEventArgs> _ioCompleted;

        public TcpClientArgsPool(int initialCount, int bufferSize, EventHandler<SocketAsyncEventArgs> ioCompleted)
        {
            if (initialCount < 0)
                throw new ArgumentException("Начальное количество элементов не может быть отрицательным", "initialCount");
            if (bufferSize < 1)
                throw new ArgumentException("Размер буфера имеет неверное значение " + bufferSize, "bufferSize");
            if (ioCompleted == null)
                throw new ArgumentNullException("ioCompleted");

            _bufferSize = bufferSize;
            _ioCompleted = ioCompleted;
            Allocate(initialCount);
        }

        protected override TcpClientArgs CreateNew()
        {
            var args = new TcpClientArgs();
            args.SetBuffer(new byte[_bufferSize], 0, _bufferSize);
            args.Completed += _ioCompleted;
            return args;
        }

        protected override void CleanUp(TcpClientArgs item)
        {
            item.UTF8Message = null;
            item.AcceptSocket = null;
        }
    }
}