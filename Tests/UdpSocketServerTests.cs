using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Dem0n13.SocketServer;
using NUnit.Framework;

namespace Dem0n13.Tests
{
    [TestFixture]
    public class UdpSocketServerTests
    {
        private const int BufferSize = 1024;
        private readonly IPEndPoint _ipEndPoint = new IPEndPoint(IPAddress.Loopback, 50000);
        private ISocketServer _server;
        private EndPoint _emptyEndPoint = new IPEndPoint(IPAddress.None, 50000);

        [SetUp]
        public void Init()
        {
            _server = new UdpSocketServer<MockLogicServer, MockLogicServer>(IPAddress.Loopback, 50000, 10, BufferSize);
        }

        [Test]
        public void StartStop()
        {
            Assert.IsFalse(_server.Started);

            _server.Start();
            Assert.IsTrue(_server.Started);

            _server.Start();
            Assert.IsTrue(_server.Started);

            _server.Stop();
            Assert.IsFalse(_server.Started);

            _server.Stop();
            Assert.IsFalse(_server.Started);
        }

        [Test]
        public void RequestResponse()
        {
            _server.Start();

            var client = new UdpClientArgsPool(1, BufferSize).Take();

            for (var i = 0; i < 100; i++)
            {
                var message = "Request" + i;
                client.UTF8Message = message;
                client.Socket.SendTo(client.DataBuffer, _ipEndPoint);

                client.UTF8Message = null;
                client.Socket.ReceiveFrom(client.DataBuffer, ref _emptyEndPoint);
                Assert.AreEqual(message, client.UTF8Message);
            }

            _server.Stop();
        }

        [Test]
        public void MultiRequestResponse()
        {
            _server.Start();

            var clients = new UdpClientArgsPool(10, BufferSize);

            for (var i = 0; i < 10; i++)
            {
                new Thread(o =>
                               {
                                   var client = clients.Take();
                                   client.Socket.ReceiveTimeout = 500;
                                   for (var r = 0; r < 100; r++)
                                   {
                                       var message = "Request_"+o+"_" + r;
                                       client.UTF8Message = message;
                                       client.Socket.SendTo(client.DataBuffer, _ipEndPoint);

                                       client.UTF8Message = null;
                                       try
                                       {
                                           client.Socket.ReceiveFrom(client.DataBuffer, ref _emptyEndPoint);
                                           Assert.AreEqual(message, client.UTF8Message);
                                       }
                                       catch (SocketException ex)
                                       {
                                           Debug.WriteLine("{0}, Thread: {1}, It: {2}", ex.SocketErrorCode, o, r);
                                       }
                                   }

                                   clients.Release(client);
                                   
                               }).Start(i);
            }

            Thread.Yield();
            clients.WaitAll();

            _server.Stop();
        }

        private class MockLogicServer : ILogicServer
        {
            public string GetResponse(string request)
            {
                return request;
            }
        }
    }
}