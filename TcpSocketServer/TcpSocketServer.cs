using System.Net;
using System.Net.Sockets;
using System.Threading;
using Dem0n13.Utils;

namespace Dem0n13.SocketServer
{
    public class TcpSocketServer<TLogicServer, TLogicNullServer> : BaseSocketServer<TLogicServer, TLogicNullServer>
        where TLogicServer : class, ILogicServer, new()
        where TLogicNullServer : class, ILogicServer, new()
    {
        public readonly int MaxBacklog = 100; // максимальная очередь ожидания сокета
        
        private readonly SocketAsyncEventArgs _listenArgs; // информация, связанная с прослушивающим сокетом
        private readonly IPEndPoint _serverEndPoint; // конечная точка сервера

        private readonly TcpClientArgsPool _clientPool;

        private Socket _listenSocket; // прослушивающий сокет
        private CancellationTokenSource _cancellationSource;

        public TcpSocketServer(IPAddress ip, int port, int bufferSize, int maxUserCount)
        {
            _serverEndPoint = new IPEndPoint(ip, port);
            _clientPool = new TcpClientArgsPool(bufferSize, IOCompleted, maxUserCount/4, maxUserCount);

            _listenArgs = new SocketAsyncEventArgs();
            _listenArgs.Completed += StartReceiving;
        }

        protected override void StartCore()
        {
            // создаем прослушивающий сокет
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.AcceptConnection, true);
            _listenSocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            _listenSocket.Bind(_serverEndPoint);
            _listenSocket.Listen(MaxBacklog);

            _cancellationSource = new CancellationTokenSource();

            // начать цикл приема входящих подключений
            StartAccepting();
        }

        protected override void StopCore()
        {
            _cancellationSource.Cancel();

            Log.Debug("Ожидание завершения всех задач");
            _listenSocket.Close();

            _clientPool.WaitAll();
        }

        #region Асинхронная работа с сокетами

        private void StartAccepting()
        {
            if (_cancellationSource.IsCancellationRequested) return;
            if (!_listenSocket.AcceptAsync(_listenArgs))
                StartReceiving(_listenSocket, _listenArgs);
        }

        private void StartReceiving(object sender, SocketAsyncEventArgs args)
        {
            // если входящее подключение принято
            switch (args.SocketError)
            {
                case SocketError.Success:
                    Log.Trace("{0} подключен", args.AcceptSocket.RemoteEndPoint);

                    var recieveArgs = _clientPool.Take();
                    recieveArgs.AcceptSocket = args.AcceptSocket;

                    if (!args.AcceptSocket.ReceiveAsync(recieveArgs))
                        ProcessReceive(recieveArgs);
                    break;
                case SocketError.OperationAborted:
                    break;
                default:
                    Log.Error("Подключение не принято: {0}", args.SocketError);
                    break;
            }

            // освобождаем принятый сокет и начинаем новый прием
            args.AcceptSocket = null;
            StartAccepting();
        }

        private void IOCompleted(object sender, SocketAsyncEventArgs args)
        {
            var client = (AsyncClientArgs)args;

            switch (client.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(client);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(client);
                    break;
                default:
                    Log.Error("Операция {0} не выполена", client.LastOperation);
                    CloseClientConnection(client);
                    break;
            }
        }

        private void ProcessReceive(AsyncClientArgs args)
        {
            // если данные получены
            if (args.SocketError == SocketError.Success)
            {
                if (args.BytesTransferred > 0)
                {
                    var message = args.UTF8Message;
                    Log.Debug("{0} > {1}", args.AcceptSocket.RemoteEndPoint, message);

                    // передаем сообщение логическому серверу и получаем ответ
                    message = LogicServer.GetResponse(message);

                    if (message == null)
                    {
                        CloseClientConnection(args);
                        return;
                    }

                    args.UTF8Message = message;

                    // посылаем сообщение обратно клиенту
                    if (!args.AcceptSocket.SendAsync(args))
                        ProcessSend(args);
                }
                else
                {
                    // клиент закончил передачу сообщения
                    CloseClientConnection(args);
                }
            }
            else
            {
                Log.Error("{0} > {1}", args.AcceptSocket.RemoteEndPoint, args.SocketError);
                CloseClientConnection(args);
            }
        }

        private void ProcessSend(AsyncClientArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                Log.Debug("{0} < {1}", args.AcceptSocket.RemoteEndPoint, args.UTF8Message);
            }
            else
            {
                Log.Error("{0} < {1}", args.AcceptSocket.RemoteEndPoint, args.SocketError);
            }

            CloseClientConnection(args);
        }

        private void CloseClientConnection(AsyncClientArgs args)
        {
            try
            {
                args.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                Log.Debug(ex.Message);
            }

            args.AcceptSocket.Close();

            // освобождаем ресурс для следующего клиента
            _clientPool.Release(args);
        }
    }

    #endregion
}