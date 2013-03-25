using System;
using System.Net.Sockets;
using System.Text;
using Dem0n13.Utils;
using NLog;

namespace Dem0n13.SocketServer
{
    public class TcpClientArgs : SocketAsyncEventArgs, IPoolable<TcpClientArgs>
    {
        private static readonly char[] TrimChars = new[] { char.MinValue }; // символы, обрезаемые в сообщениях: \0
        private static readonly Logger Logger = LogManager.GetLogger("SocketServer");
        private static readonly Encoding UTF8 = Encoding.UTF8;

        private readonly PoolToken<TcpClientArgs> _poolToken;
        public PoolToken<TcpClientArgs> PoolToken { get { return _poolToken; } }

        public TcpClientArgs()
            : this(null)
        {
        }

        public TcpClientArgs(Pool<TcpClientArgs> pool)
        {
            _poolToken = new PoolToken<TcpClientArgs>(this, pool);
        }

        /// <summary>
        /// Возвращает или задает сообщение в кодировке UTF8, хранящееся в буфере
        /// </summary>
        public string UTF8Message
        {
            get { return UTF8.GetString(Buffer, Offset, Count).Trim(TrimChars); }
            set
            {
                Array.Clear(Buffer, Offset, Count);
                if (string.IsNullOrEmpty(value)) return;
                
                var bytes = UTF8.GetBytes(value);
                var length = bytes.Length;

                // выбираем длину сообщения. Слишком длинное обрезается
                if (length > Count)
                {
                    Logger.Error("The message '{0}' was cut off: {1} > {2}", value, length, Count);
                    length = Count;
                }

                System.Buffer.BlockCopy(bytes, 0, Buffer, Offset, length);
            }
        }
    }
}