using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetRedemptions : IRequest
    {
        public string Name => "get_redemptions";

        private readonly Database _database;
        private readonly ILogger _logger;

        public GetRedemptions(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            var redemptions = new List<Redemption>();
            string sql = $"SELECT id, \"redemptionId\", \"actionName\", \"order\", state FROM \"Redemption\" WHERE \"userId\" = @user";
            await _database.Execute(sql, temp, (r) => redemptions.Add(new Redemption()
            {
                Id = r.GetGuid(0).ToString("D"),
                RedemptionId = r.GetString(1),
                ActionName = r.GetString(2),
                Order = r.GetInt32(3),
                State = r.GetBoolean(4)
            }));
            _logger.Information($"Fetched all redemptions for channel [channel: {sender}]");
            return new RequestResult(true, redemptions, notifyClientsOnAccount: false);
        }
    }
}