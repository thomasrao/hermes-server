using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using CommonSocketLibrary.Common;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket
{
    public class WebSocketUser : IDisposable
    {
        private readonly WebSocket _socket;
        private readonly JsonSerializerOptions _options;
        private readonly ILogger _logger;

        private readonly IPAddress? _ipAddress;
        private CancellationTokenSource _cts;
        private bool _connected;

        public WebSocketCloseStatus? CloseStatus { get => _socket.CloseStatus; }
        public string? CloseStatusDescription { get => _socket.CloseStatusDescription; }
        public WebSocketState State { get => _socket.State; }
        public IPAddress? IPAddress { get => _ipAddress; }
        public bool Connected { get => _connected; }
        public string UID { get; }
        public string ApiKey { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool Admin { get; set; }
        public bool WebLogin { get; set; }
        public DateTime LastHeartbeatReceived { get; set; }
        public DateTime LastHearbeatSent { get; set; }
        public CancellationToken Token { get => _cts.Token; }


        public WebSocketUser(WebSocket socket, IPAddress? ipAddress, JsonSerializerOptions options, ILogger logger)
        {
            _socket = socket;
            _ipAddress = ipAddress;
            _options = options;
            _connected = true;
            _logger = logger;
            Admin = false;
            WebLogin = false;
            _cts = new CancellationTokenSource();
            UID = Guid.NewGuid().ToString("D");
            LastHeartbeatReceived = DateTime.UtcNow;
        }


        public async Task Close(WebSocketCloseStatus status, string? message, CancellationToken token)
        {
            try
            {
                await _socket.CloseAsync(status, message ?? CloseStatusDescription, token);
            }
            catch (WebSocketException wse) when (wse.Message.StartsWith("The WebSocket is in an invalid state "))
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to close socket.");
            }
            finally
            {
                _connected = false;
                await _cts.CancelAsync();
                _cts = new CancellationTokenSource();
            }
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public async Task Send<Data>(int opcode, Data data)
        {
            var message = GenerateMessage(opcode, data);
            var content = JsonSerializer.Serialize(message, _options);

            var bytes = Encoding.UTF8.GetBytes(content);
            var array = new ArraySegment<byte>(bytes);
            var total = bytes.Length;
            var current = 0;

            while (current < total)
            {
                var size = Encoding.UTF8.GetBytes(content.Substring(current), array);
                await _socket.SendAsync(array, WebSocketMessageType.Text, current + size >= total, Token);
                current += size;
            }

            _logger.Verbose($"TX #{opcode}: {content}");
        }

        public async Task<WebSocketReceiveResult?> Receive(ArraySegment<byte> bytes)
        {
            try
            {
                return await _socket.ReceiveAsync(bytes, Token);
            }
            catch (WebSocketException wse) when (wse.Message.StartsWith("The remote party "))
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to receive a web socket message.");
            }
            return null;
        }

        private WebSocketMessage GenerateMessage<Data>(int opcode, Data data)
        {
            return new WebSocketMessage()
            {
                OpCode = opcode,
                Data = data
            };
        }
    }
}