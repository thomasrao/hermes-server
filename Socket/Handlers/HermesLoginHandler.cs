using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests.Messages;
using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class HermesLoginHandler : ISocketHandler
    {
        public int OperationCode { get; } = 1;

        private readonly ServerConfiguration _configuration;
        private readonly Database _database;
        private readonly HermesSocketManager _sockets;
        private readonly ILogger _logger;
        private readonly object _lock;

        public HermesLoginHandler(ServerConfiguration configuration, Database database, HermesSocketManager sockets, ILogger logger)
        {
            _configuration = configuration;
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

            lock (_lock)
            {
                if (sender.Id != null)
                    return;

                sender.Id = userId;
                sender.ApiKey = data.ApiKey;
                sender.WebLogin = data.WebLogin;
            }

            string sql2 = "select name, role from \"User\" where id = @user";
            await _database.Execute(sql2, new Dictionary<string, object>() { { "user", userId } }, sql =>
            {
                sender.Name = sql.GetString(0);
                sender.Admin = sql.GetString(1) == "ADMIN";
            });

            if (string.IsNullOrEmpty(sender.Name))
            {
                _logger.Error($"Could not find username using the user id [user id: {userId}][api key: {data.ApiKey}]");
                return;
            }

            var ack = new LoginAckMessage()
            {
                UserId = userId,
                OwnerId = _configuration.OwnerId,
                Admin = sender.Admin,
                WebLogin = data.WebLogin,
            };

            var connections = new List<Connection>();
            string sql3 = "select \"name\", \"type\", \"clientId\", \"accessToken\", \"grantType\", \"scope\", \"expiresAt\", \"default\" from \"Connection\" where \"userId\" = @user";
            await _database.Execute(sql3, new Dictionary<string, object>() { { "user", userId } }, sql =>
                connections.Add(new Connection()
                {
                    Name = sql.GetString(0),
                    Type = sql.GetString(1),
                    ClientId = sql.GetString(2),
                    AccessToken = sql.GetString(3),
                    GrantType = sql.GetString(4),
                    Scope = sql.GetString(5),
                    ExpiresIn = sql.GetDateTime(6),
                    Default = sql.GetBoolean(7)
                })
            );
            ack.Connections = connections.ToArray();

            IList<VoiceDetails> voices = new List<VoiceDetails>();
            string sql4 = "SELECT id, name FROM \"TtsVoice\"";
            await _database.Execute(sql4, (IDictionary<string, object>?) null, (r) => voices.Add(new VoiceDetails()
            {
                Id = r.GetString(0),
                Name = r.GetString(1)
            }));
            ack.TTSVoicesAvailable = voices.ToDictionary(e => e.Id, e => e.Name);

            await sender.Send(2, ack);

            string version = data.MajorVersion == null ? "unknown" : $"{data.MajorVersion}.{data.MinorVersion}";
            _logger.Information($"Hermes client logged in {(sender.Admin ? "as administrator " : "")}[name: {sender.Name}][id: {userId}][ip: {sender.IPAddress}][version: {version}][web: {data.WebLogin}]");

            ack = new LoginAckMessage()
            {
                AnotherClient = true,
                UserId = userId,
                OwnerId = _configuration.OwnerId,
                WebLogin = data.WebLogin
            };

            var recipients = _sockets.GetSockets(userId).ToList().Where(s => s.UID != sender.UID);
            var tasks = new List<Task>();
            foreach (var socket in recipients)
            {
                try
                {
                    tasks.Add(socket.Send(2, ack));
                }
                catch (Exception)
                {
                }
            }
            await Task.WhenAll(tasks);
        }
    }
}