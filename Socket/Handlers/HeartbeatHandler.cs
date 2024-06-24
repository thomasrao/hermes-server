using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class HeartbeatHandler : ISocketHandler
    {
        public int OpCode { get; } = 0;

        private ILogger _logger;

        public HeartbeatHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not HeartbeatMessage data)
                return;

            sender.LastHeartbeatReceived = DateTime.UtcNow;
            _logger.Verbose($"Received heartbeat from socket [ip: {sender.IPAddress}].");

            if (data.Respond)
                await sender.Send(0, new HeartbeatMessage()
                {
                    DateTime = DateTime.UtcNow,
                    Respond = false
                });
        }
    }
}