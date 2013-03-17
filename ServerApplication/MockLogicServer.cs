using Dem0n13.SocketServer;

namespace Dem0n13.ServerApplication
{
    public class MockLogicServer : ILogicServer
    {
        public string GetResponse(string request)
        {
            return request;
        }
    }
}