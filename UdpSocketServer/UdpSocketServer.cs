using System;
using System.Net;
using System.Net.Sockets;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public class UdpSocketServer<TLogicServer, TNullLogicServer> : BaseSocketServer<TLogicServer, TNullLogicServer>
        where TLogicServer : class, ILogicServer, new()
        where TNullLogicServer : class, ILogicServer, new()
    {
        private readonly IPEndPoint _serverEndPoint;
        private readonly UdpClientArgsPool _clientPool;
        
        private Socket _serverSocket;

        public UdpSocketServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
        {
            _serverEndPoint = new IPEndPoint(ip, port);
            _clientPool = new UdpClientArgsPool(bufferSize, IOCompleted, maxUserCount/4, maxUserCount);
        }
        
        protected override void StartCore()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverSocket.Bind(_serverEndPoint);
            StartReceiving();
        }

        protected override void StopCore()
        {
            _serverSocket.Close();
            Log.Debug("Waiting for current tasks completion");
            _clientPool.WaitAll();
        }

        private void StartReceiving()
        {
            var client = _clientPool.Take();
            try
            {
                if (!_serverSocket.ReceiveFromAsync(client))
                    ProcessReceive(client);
            }
            catch (ObjectDisposedException)
            {
                _clientPool.Release(client);
            }
        }

        private void IOCompleted(object sender, SocketAsyncEventArgs args)
        {
            var client = (AsyncClientArgs) args;

            switch (client.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(client);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSend(client);
                    break;
                default:
                    Log.Error("Операция {0} не разрешена", client.LastOperation);
                    _clientPool.Release(client);
                    break;
            }
        }

        private void ProcessReceive(AsyncClientArgs client)
        {
            switch (client.SocketError)
            {
                case SocketError.Success:
                    if (client.BytesTransferred > 0)
                    {
                        var request = client.UTF8Message;

                        // send request to logic server and get the response
                        var response = LogicServer.GetResponse(request);

                        // send response to client, if it is not empty
                        if (response != null)
                        {
                            client.UTF8Message = response;
                            if (!client.AcceptSocket.SendToAsync(client))
                                ProcessSend(client);
                        }

                        Log.Debug("{0}: {1} -> {2}", client.RemoteEndPoint, request, response);
                    }
                    break;

                case SocketError.OperationAborted:
                    _clientPool.Release(client);
                    return;

                default:
                    Log.Error("{0}: {1}", client.RemoteEndPoint, client.SocketError);
                    _clientPool.Release(client);
                    break;
            }
            StartReceiving();
        }

        private void ProcessSend(AsyncClientArgs client)
        {
            if (client.SocketError != SocketError.Success)
            {
                Log.Error("{0}: {1}", client.RemoteEndPoint, client.SocketError);
            }
            _clientPool.Release(client);
        }
    }
}
