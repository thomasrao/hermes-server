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

            var userIdDict = new Dictionary<string, object>() { { "user", userId } };
            string? ttsDefaultVoice = null;
            string sql2 = "select name, role, \"ttsDefaultVoice\" from \"User\" where id = @user";
            await _database.Execute(sql2, userIdDict, sql =>
            {
                sender.Name = sql.GetString(0);
                sender.Admin = sql.GetString(1) == "ADMIN";
                ttsDefaultVoice = sql.GetString(2);
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

            ack.Connections = new List<Connection>();
            string sql3 = "select \"name\", \"type\", \"clientId\", \"accessToken\", \"grantType\", \"scope\", \"expiresAt\", \"default\" from \"Connection\" where \"userId\" = @user";
            await _database.Execute(sql3, userIdDict, sql =>
                ack.Connections.Add(new Connection()
                {
                    Name = sql.GetString(0),
                    Type = sql.GetString(1),
                    ClientId = sql.GetString(2),
                    AccessToken = sql.GetString(3),
                    GrantType = sql.GetString(4),
                    Scope = sql.GetString(5),
                    ExpiresAt = sql.GetDateTime(6),
                    Default = sql.GetBoolean(7)
                })
            );

            ack.TTSVoicesAvailable = new Dictionary<string, string>();
            string sql4 = "SELECT id, name FROM \"TtsVoice\"";
            await _database.Execute(sql4, (IDictionary<string, object>?) null, (r) => ack.TTSVoicesAvailable.Add(r.GetString(0), r.GetString(1)));

            ack.EnabledTTSVoices = new List<string>();
            string sql5 = $"SELECT v.name FROM \"TtsVoiceState\" s "
                + "INNER JOIN \"TtsVoice\" v ON s.\"ttsVoiceId\" = v.id "
                + "WHERE \"userId\" = @user AND state = true";
            await _database.Execute(sql5, userIdDict, (r) => ack.EnabledTTSVoices.Add(r.GetString(0)));

            ack.WordFilters = new List<TTSWordFilter>();
            string sql6 = $"SELECT id, search, replace FROM \"TtsWordFilter\" WHERE \"userId\" = @user";
            await _database.Execute(sql6, userIdDict, (r) => ack.WordFilters.Add(new TTSWordFilter()
            {
                Id = r.GetString(0),
                Search = r.GetString(1),
                Replace = r.GetString(2)
            }));

            if (ttsDefaultVoice != null)
                ack.DefaultTTSVoice = ttsDefaultVoice;

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