using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer
{
    public class ServerRequestManager : RequestManager
    {
        public ServerRequestManager(IServiceProvider serviceProvider, ILogger logger) : base(serviceProvider, logger)
        {
        }

        protected override string AssemblyName => "SocketServer";
    }
}