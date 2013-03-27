using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Dem0n13.Utils;
using NLog;

namespace Dem0n13.SocketServer
{
    public class UdpClientArgs : PoolObject<UdpClientArgs>
    {
        private static readonly char[] TrimChars = new[] { char.MinValue }; // \0
        private static readonly Logger Logger = LogManager.GetLogger("SocketServer");
        private static readonly Encoding UTF8 = Encoding.UTF8;

        public UdpClientArgs(int bufferSize)
            : this(bufferSize, null)
        {
        }

        public UdpClientArgs(int bufferSize, Pool<UdpClientArgs> pool)
            : base(pool)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize", "The buffer size must not be negative.");
            
            DataBuffer = new byte[bufferSize];
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            EndPoint = new IPEndPoint(0L, 0);
        }

        /// <summary>
        /// Gets instanse of the <see cref="System.Net.Sockets.Socket"/> to use with socket operations
        /// </summary>
        public readonly Socket Socket;

        /// <summary>
        /// Gets the data buffer to use with socket operations
        /// </summary>
        public readonly byte[] DataBuffer;

        /// <summary>
        /// Gets or sets the remote endpoint for socket operations
        /// </summary>
        public EndPoint EndPoint;

        /// <summary>
        /// Gets or sets the message encoded in UTF8, stored in the DataBuffer
        /// </summary>
        public string UTF8Message
        {
            get { return UTF8.GetString(DataBuffer).Trim(TrimChars); }
            set
            {
                Array.Clear(DataBuffer, 0, DataBuffer.Length);
                if (string.IsNullOrEmpty(value)) return;

                var bytes = UTF8.GetBytes(value);
                var length = bytes.Length;

                // выбираем длину сообщения. Слишком длинное обрезается
                if (length > DataBuffer.Length)
                {
                    Logger.Error("The message '{0}' was cut off: {1} > {2}", value, length, DataBuffer.Length);
                    length = DataBuffer.Length;
                }

                Buffer.BlockCopy(bytes, 0, DataBuffer, 0, length);
            }
        }
    }
}