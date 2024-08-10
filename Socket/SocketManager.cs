using System.Timers;
using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket
{
    public class HermesSocketManager
    {
        private IList<WebSocketUser> _sockets;
        private System.Timers.Timer _timer;
        private ILogger _logger;


        public HermesSocketManager(ILogger logger)
        {
            _sockets = new List<WebSocketUser>();
            _timer = new System.Timers.Timer(TimeSpan.FromSeconds(1));
            _timer.Elapsed += async (sender, e) => await HandleHeartbeats(e);
            _timer.Enabled = true;
            _logger = logger;
        }


        public void Add(WebSocketUser socket)
        {
            _sockets.Add(socket);
        }

        public IList<WebSocketUser> GetAllSockets()
        {
            return _sockets.AsReadOnly();
        }

        public IEnumerable<WebSocketUser> GetSockets(string userId)
        {
            foreach (var socket in _sockets)
            {
                if (socket.Id == userId)
                    yield return socket;
            }
        }

        public bool Remove(WebSocketUser socket)
        {
            return _sockets.Remove(socket);
        }

        private async Task HandleHeartbeats(ElapsedEventArgs e)
        {
            try
            {
                var signalTime = e.SignalTime.ToUniversalTime();
                for (var i = 0; i < _sockets.Count; i++)
                {
                    var socket = _sockets[i];
                    if (!socket.Connected)
                    {
                        _sockets.RemoveAt(i--);
                    }
                    else if (signalTime - socket.LastHeartbeatReceived > TimeSpan.FromSeconds(30))
                    {
                        if (socket.LastHeartbeatReceived > socket.LastHearbeatSent)
                        {
                            try
                            {
                                socket.LastHearbeatSent = DateTime.UtcNow;
                                await socket.Send(0, new HeartbeatMessage() { DateTime = signalTime, Respond = true });
                            }
                            catch (Exception)
                            {
                            }
                        }
                        else if (signalTime - socket.LastHeartbeatReceived > TimeSpan.FromSeconds(60))
                        {
                            _logger.Debug($"Closing socket [ip: {socket.IPAddress}] for not responding for 2 minutes.");
                            try
                            {
                                socket.Dispose();
                            }
                            catch (Exception)
                            {
                            }
                            finally
                            {
                                _sockets.RemoveAt(i--);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}