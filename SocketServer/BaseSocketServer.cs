using NLog;

namespace Dem0n13.SocketServer
{
    public abstract class BaseSocketServer<TLogicServer, TNullLogicServer> : ISocketServer
        where TLogicServer : class, ILogicServer, new()
        where TNullLogicServer : class, ILogicServer, new()
    {
        protected readonly Logger Log = LogManager.GetLogger("SocketServer");
        
        private readonly object _syncRoot = new object();

        public bool Started { get; protected set; }
        public ILogicServer LogicServer { get; protected set; }
        
        protected abstract void StartCore();
        protected abstract void StopCore();

        protected BaseSocketServer()
        {
            LogicServer = new TNullLogicServer();
        }

        /// <summary>
        /// Starts the socket server. Sets logic server to instance of the <see cref="TLogicServer"/>
        /// </summary>
        public void Start()
        {
            lock (_syncRoot)
            {
                if (Started) return;
                LogicServer = new TLogicServer();
                StartCore();
                Started = true;
                Log.Info("The server is started");
            }
        }

        /// <summary>
        /// Stops the socket server. Sets logic server to instance of the <see cref="TNullLogicServer"/>
        /// </summary>
        public void Stop()
        {
            lock (_syncRoot)
            {
                if (!Started) return;
                Started = false;
                StopCore();
                LogicServer = new TNullLogicServer();
                Log.Info("The server is stopped");
            }
        }

        /// <summary>
        /// Restarts the servers
        /// </summary>
        public void Restart()
        {
            lock (_syncRoot)
            {
                if (Started) StopCore();
                LogicServer = new TLogicServer();
                StartCore();
                Started = true;
                Log.Info("The server is restarted");
            }
        }
    }
}