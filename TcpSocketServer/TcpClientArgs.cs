using System;
using System.Net.Sockets;
using System.Text;
using Dem0n13.Utils;
using NLog;

namespace Dem0n13.SocketServer
{
    public class TcpClientArgs : SocketAsyncEventArgs
    {
        private static readonly char[] TrimChars = new[] { char.MinValue }; // символы, обрезаемые в сообщениях: \0
        private static readonly Logger Logger = LogManager.GetLogger("SocketServer");
        private static readonly Encoding UTF8 = Encoding.UTF8;
        
        private readonly UniqueObject<TcpClientArgs> _uniqueToken = new UniqueObject<TcpClientArgs>();

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
                    Logger.Error("Сообщение {0} было обрезано: {1} > {2}", value, length, Count);
                    length = Count;
                }

                System.Buffer.BlockCopy(bytes, 0, Buffer, Offset, length);
            }
        }

        public override int GetHashCode()
        {
            return _uniqueToken.Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override string ToString()
        {
            return _uniqueToken.ToString();
        }
    }
}