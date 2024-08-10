using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CommonSocketLibrary.Common;
using HermesSocketLibrary.Socket.Data;
using HermesSocketServer.Socket;
using ILogger = Serilog.ILogger;

namespace HermesSocketLibrary
{
    public class Server
    {
        private readonly HermesSocketManager _sockets;
        private readonly SocketHandlerManager _handlers;
        private readonly JsonSerializerOptions _options;
        private readonly ILogger _logger;


        public Server(
            HermesSocketManager sockets,
            SocketHandlerManager handlers,
            JsonSerializerOptions options,
            ILogger logger
        )
        {
            _sockets = sockets;
            _handlers = handlers;
            _options = options;
            _logger = logger;
        }


        public async Task Handle(WebSocketUser socket, HttpContext context)
        {
            _logger.Information($"Socket connected [ip: {socket.IPAddress}][uid: {socket.UID}]");
            _sockets.Add(socket);
            var buffer = new byte[1024 * 8];

            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    var result = await socket.Receive(new ArraySegment<byte>(buffer));
                    if (result == null || result.MessageType == WebSocketMessageType.Close || !socket.Connected)
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count).TrimEnd('\0');
                    var obj = JsonSerializer.Deserialize<WebSocketMessage>(message, _options);
                    if (obj == null)
                        continue;

                    if (obj.OpCode != 0)
                        _logger.Information($"rxm: {message} [ip: {socket.IPAddress}][id: {socket.Id}][name: {socket.Name}][token: {socket.ApiKey}][uid: {socket.UID}]");

                    /**
                    * 0: Heartbeat
                    * 1: Login RX
                    * 2: Login Ack TX
                    * 3: Request RX
                    * 4: Request Ack TX
                    * 5: Logging RX/TX
                    */
                    if (obj.Data == null)
                    {
                        await socket.Send(5, new LoggingMessage("Received no data in the message.", HermesLoggingLevel.Warn));
                        continue;
                    }
                    else if (obj.OpCode == 0)
                        obj.Data = JsonSerializer.Deserialize<HeartbeatMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 1)
                        obj.Data = JsonSerializer.Deserialize<HermesLoginMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 3)
                        obj.Data = JsonSerializer.Deserialize<RequestMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 5)
                        obj.Data = JsonSerializer.Deserialize<LoggingMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 6)
                        obj.Data = JsonSerializer.Deserialize<ChatterMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 7)
                        obj.Data = JsonSerializer.Deserialize<EmoteDetailsMessage>(obj.Data.ToString(), _options);
                    else if (obj.OpCode == 8)
                        obj.Data = JsonSerializer.Deserialize<EmoteUsageMessage>(obj.Data.ToString(), _options);
                    else
                    {
                        await socket.Send(5, new LoggingMessage("Received an invalid message: " + message, HermesLoggingLevel.Error));
                        continue;
                    }
                    await _handlers.Execute(socket, obj.OpCode, obj.Data);
                }
                catch (WebSocketException wse)
                {
                    _logger.Error(wse, $"Error trying to process a socket message [code: {wse.ErrorCode}][ip: {socket.IPAddress}][id: {socket.Id}][name: {socket.Name}][token: {socket.ApiKey}][uid: {socket.UID}]");
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"Error trying to process a socket message [ip: {socket.IPAddress}][id: {socket.Id}][name: {socket.Name}][token: {socket.ApiKey}][uid: {socket.UID}]");
                }
            }

            try
            {
                if (socket.Connected)
                    await socket.Close(socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.Information(e, $"Client failed to disconnect [ip: {socket.IPAddress}][id: {socket.Id}][name: {socket.Name}][token: {socket.ApiKey}][uid: {socket.UID}]");
            }
            finally
            {
                socket.Dispose();
                _sockets.Remove(socket);
            }
            _logger.Information($"Client disconnected [ip: {socket.IPAddress}][id: {socket.Id}][name: {socket.Name}][token: {socket.ApiKey}][uid: {socket.UID}]");
        }
    }
}