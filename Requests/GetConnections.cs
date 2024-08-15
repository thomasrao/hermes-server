using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Socket.Data;

namespace HermesSocketServer.Requests
{
    public class GetConnections : IRequest
    {
        public string Name => "get_connections";

        private Database _database;
        private Serilog.ILogger _logger;

        public GetConnections(Database database, Serilog.ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            var connections = new List<Connection>();
            string sql3 = "select \"name\", \"type\", \"clientId\", \"accessToken\", \"grantType\", \"scope\", \"expiresAt\", \"default\" from \"Connection\" where \"userId\" = @user";
            await _database.Execute(sql3, temp, sql =>
                connections.Add(new Connection()
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
            return new RequestResult(true, connections, false);
        }
    }
}