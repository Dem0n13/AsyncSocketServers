using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dem0n13.SocketServer
{
    public class UdpSocketServer<TLogicServer, TNullLogicServer> : BaseSocketServer<TLogicServer, TNullLogicServer>
        where TLogicServer : class, ILogicServer, new()
        where TNullLogicServer : class, ILogicServer, new()
    {
        private const int ReceiveTimeout = 500;
        private readonly IPEndPoint _serverEndPoint;
        private readonly UdpClientArgsPool _clientPool;
        
        private Socket _serverSocket;
        private CancellationTokenSource _cancellationSource;

        public UdpSocketServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
        {
            _serverEndPoint = new IPEndPoint(ip, port);
            _clientPool = new UdpClientArgsPool(bufferSize, maxUserCount/4, maxUserCount);
        }

        protected override void StartCore()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _serverSocket.Bind(_serverEndPoint);
            _serverSocket.ReceiveTimeout = ReceiveTimeout;

            _cancellationSource = new CancellationTokenSource();
            new Thread(Receiving).Start(); // start the receiving loop in another thread
        }

        protected override void StopCore()
        {
            _cancellationSource.Cancel();

            Log.Debug("Waiting for current tasks completion");
            _clientPool.WaitAll();

            _serverSocket.Close();
        }

        private void Receiving()
        {
            var factory = new TaskFactory(_cancellationSource.Token,
                                          TaskCreationOptions.None,
                                          TaskContinuationOptions.None,
                                          TaskScheduler.Default);
            
            var currentClient = _clientPool.Take();

            while (!_cancellationSource.IsCancellationRequested)
            {
                try
                {
                    var received = _serverSocket.ReceiveFrom(currentClient.DataBuffer, ref currentClient.EndPoint);
                    if (received > 0)
                    {
                        // if there is incoming data - put processing in the thread pool (and continue releasing)
                        // the current thread gets a new client args from the pool and continue the receiving loop
                        factory.StartNew(ProcessClient, currentClient)
                            .ContinueWith(ReleaseClient, currentClient);
                        currentClient = _clientPool.Take();
                    }
                    else
                    {
                        Log.Warn("Data not received");
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                    Log.Error("Data not received: " + ex.SocketErrorCode);
                }
            }

            _clientPool.Release(currentClient);
        }

        private void ProcessClient(object state)
        {
            var client = (UdpClientArgs) state;
            var request = client.UTF8Message;
            
            // send request to logic server and get the response
            var response = LogicServer.GetResponse(request);

            // send response to client, if it is not empty
            if (response != null)
            {
                client.UTF8Message = response;
                client.Socket.SendTo(client.DataBuffer, client.EndPoint);
            }
            
            Log.Debug("{0}: {1} -> {2}", client.EndPoint, request, response);
        }

        
        private void ReleaseClient(Task task, object state)
        {
            Log.Trace("The task was completed: " + task.Status);
            var client = (UdpClientArgs)state;
            _clientPool.Release(client);
        }
    }
}
