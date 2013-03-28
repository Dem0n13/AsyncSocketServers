using System;
using System.Net;
using System.Net.Sockets;
using Dem0n13.SocketServer;

namespace Dem0n13.ClientApplication
{
    internal class ConsoleApplication
    {
        private const int ServerPort = 50000;
        private const int BufferSize = 1024;

        static void Main()
        {
            var serverAddres = IPAddress.Loopback;
            var serverEndPoint = new IPEndPoint(serverAddres, ServerPort);
            EndPoint emptyEndPoint = new IPEndPoint(IPAddress.None, ServerPort);

            var client = new AsyncClientArgs(BufferSize);
            var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 500;
            Console.WriteLine("Enter the message for server");

            string request;
            do
            {
                Console.Write("> ");
                request = Console.ReadLine();
                client.UTF8Message = request;
                socket.SendTo(client.Buffer, serverEndPoint);
                try
                {
                    socket.ReceiveFrom(client.Buffer, ref emptyEndPoint);
                    Console.WriteLine("< " + client.UTF8Message);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Error: " + ex.SocketErrorCode);
                }
            } while (!string.IsNullOrEmpty(request));
        }
    }
}
