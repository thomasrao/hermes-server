using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class RequestHandler : ISocketHandler
    {
        public int OpCode { get; } = 3;
        private readonly RequestManager _requests;
        private readonly HermesSocketManager _sockets;
        private readonly ILogger _logger;

        public RequestHandler(RequestManager requests, HermesSocketManager sockets, ILogger logger)
        {
            _requests = requests;
            _sockets = sockets;
            _logger = logger;
        }


        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (sender.Id == null)
                return;
            if (message is not RequestMessage data)
                return;

            RequestResult? result = null;
            _logger.Debug("Executing request handler: " + data.Type);
            try
            {
                result = await _requests.Grant(sender.Id, data);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Failed to grant a request of type '{data.Type}'.");
            }

            if (result == null || !result.Success)
                return;

            var ack = new RequestAckMessage()
            {
                Request = data,
                Data = result.Result,
                Nounce = data.Nounce
            };

            if (!result.NotifyClientsOnAccount)
            {
                await sender.Send(4, ack);
                return;
            }

            var recipients = _sockets.GetSockets(sender.Id);
            foreach (var socket in recipients)
            {
                try
                {
                    _logger.Verbose($"Sending {data.Type} to socket [ip: {socket.IPAddress}].");
                    await socket.Send(4, ack);
                }
                catch (Exception)
                {
                    _logger.Warning($"Failed to send {data.Type} to socket [ip: {socket.IPAddress}].");
                }
            }
        }
    }
}