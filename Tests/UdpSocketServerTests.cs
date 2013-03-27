using System.Net;
using System.Net.Sockets;
using Dem0n13.SocketServer;
using Dem0n13.Utils;
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
            _server = new UdpSocketServer<MockLogicServer, MockLogicServer>(IPAddress.Loopback, 50000, BufferSize, 10);
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

            var client = new AsyncClientArgs();
            client.SetBuffer(new byte[BufferSize], 0, BufferSize);
            client.AcceptSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            for (var i = 0; i < 100; i++)
            {
                var message = "Request" + i;
                client.UTF8Message = message;
                client.AcceptSocket.SendTo(client.Buffer, _ipEndPoint);

                client.UTF8Message = null;
                client.AcceptSocket.ReceiveFrom(client.Buffer, ref _emptyEndPoint);
                
                Assert.AreEqual(message, client.UTF8Message);
            }

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