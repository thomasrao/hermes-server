using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetRedeemableActions : IRequest
    {
        public string Name => "get_redeemable_actions";

        private readonly JsonSerializerOptions _options;
        private readonly Database _database;
        private readonly ILogger _logger;

        public GetRedeemableActions(JsonSerializerOptions options, Database database, ILogger logger)
        {
            _options = options;
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            var redemptions = new List<RedeemableAction>();
            string sql = $"SELECT name, type, data FROM \"Action\" WHERE \"userId\" = @user";
            await _database.Execute(sql, temp, (r) => redemptions.Add(new RedeemableAction()
            {
                Name = r.GetString(0),
                Type = r.GetString(1),
                Data = JsonSerializer.Deserialize<IDictionary<string, string>>(r.GetString(2), _options)!
            }));
            _logger.Information($"Fetched all chatters' selected tts voice for channel [channel: {sender}]");
            return new RequestResult(true, redemptions, notifyClientsOnAccount: false);
        }
    }
}