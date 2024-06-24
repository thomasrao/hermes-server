using HermesSocketLibrary.db;
using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class HermesLoginHandler : ISocketHandler
    {
        public int OpCode { get; } = 1;

        private readonly Database _database;
        private readonly HermesSocketManager _sockets;
        private readonly ILogger _logger;
        private readonly object _lock;

        public HermesLoginHandler(Database database, HermesSocketManager sockets, ILogger logger)
        {
            _database = database;
            _sockets = sockets;
            _logger = logger;
            _lock = new object();
        }


        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not HermesLoginMessage data || data == null || data.ApiKey == null)
                return;
            if (sender.Id != null)
                return;

            string sql = "select \"userId\" from \"ApiKey\" where id = @key";
            var result = await _database.ExecuteScalar(sql, new Dictionary<string, object>() { { "key", data.ApiKey } });
            string? userId = result?.ToString();

            if (userId == null)
                return;

            var recipients = _sockets.GetSockets(userId).ToList();

            lock (_lock)
            {
                if (sender.Id != null)
                    return;

                sender.Id = userId;
            }

            string sql2 = "select \"name\" from \"User\" where id = @user";
            var result2 = await _database.ExecuteScalar(sql2, new Dictionary<string, object>() { { "user", userId } });
            string? name = result2?.ToString();

            if (string.IsNullOrEmpty(name))
                return;

            sender.Name = name;

            await sender.Send(2, new LoginAckMessage()
            {
                UserId = userId
            });

            var ack = new LoginAckMessage()
            {
                AnotherClient = true,
                UserId = userId
            };

            foreach (var socket in recipients)
            {
                try
                {
                    await socket.Send(2, ack);
                }
                catch (Exception)
                {
                }
            }

            _logger.Information($"Hermes client logged in [name: {name}][id: {userId}][ip: {sender.IPAddress}]");
        }
    }
}