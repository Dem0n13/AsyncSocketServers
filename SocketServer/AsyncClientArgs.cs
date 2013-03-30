using System;
using System.Net.Sockets;
using System.Text;
using Dem0n13.Utils;
using NLog;

namespace Dem0n13.SocketServer
{
    public class AsyncClientArgs : SocketAsyncEventArgs, IPoolable
    {
        private static readonly char[] TrimChars = new[] { char.MinValue }; // \0
        private static readonly Logger Logger = LogManager.GetLogger("SocketServer");
        private static readonly Encoding UTF8 = Encoding.UTF8;

        bool IPoolable.InPool { get; set; }

        public AsyncClientArgs(int bufferSize)
        {
            SetBuffer(new byte[bufferSize], 0, bufferSize);
        }

        /// <summary>
        /// Gets or sets the message encoded in UTF8, stored in the DataBuffer
        /// </summary>
        public string UTF8Message
        {
            get { return UTF8.GetString(Buffer, Offset, Count).Trim(TrimChars); }
            set
            {
                Array.Clear(Buffer, Offset, Count);
                if (value == null) return;
                
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