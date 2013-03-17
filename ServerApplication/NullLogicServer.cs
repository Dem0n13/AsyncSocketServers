using Dem0n13.SocketServer;

namespace Dem0n13.ServerApplication
{
    public class NullLogicServer : ILogicServer
    {
        public string GetResponse(string request)
        {
            return "The server is not running";
        }
    }
}