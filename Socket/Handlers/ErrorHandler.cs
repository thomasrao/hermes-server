using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class ErrorHandler : ISocketHandler
    {
        public int OpCode { get; } = 0;

        private ILogger _logger;

        public ErrorHandler(ILogger logger)
        {
            _logger = logger;
        }


        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not ErrorMessage data)
                return;

            if (data.Exception == null)
                _logger.Error(data.Message);
            else
                _logger.Error(data.Exception, data.Message);
        }
    }
}