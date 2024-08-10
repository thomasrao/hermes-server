using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class LoggingHandler : ISocketHandler
    {
        public int OperationCode { get; } = 5;

        private ILogger _logger;

        public LoggingHandler(ILogger logger)
        {
            _logger = logger;
        }


        public async Task Execute<T>(WebSocketUser sender, T data, HermesSocketManager sockets)
        {
            if (data is not LoggingMessage message || sender.Id == null)
                return;

            Action<Exception?, string> logging;
            if (message.Level == HermesLoggingLevel.Trace)
                logging = _logger.Verbose;
            else if (message.Level == HermesLoggingLevel.Debug)
                logging = _logger.Debug;
            else if (message.Level == HermesLoggingLevel.Info)
                logging = _logger.Information;
            else if (message.Level == HermesLoggingLevel.Warn)
                logging = _logger.Warning;
            else if (message.Level == HermesLoggingLevel.Error)
                logging = _logger.Error;
            else if (message.Level == HermesLoggingLevel.Critical)
                logging = _logger.Fatal;
            else {
                _logger.Warning("Failed to receive a logging level from client.");
                return;
            }
            
            logging.Invoke(message.Exception, message.Message + $" [ip: {sender.IPAddress}][id: {sender.Id}][name: {sender.Name}][token: {sender.ApiKey}][uid: {sender.UID}]");
        }
    }
}