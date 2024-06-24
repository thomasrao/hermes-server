using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetChatterIds : IRequest
    {
        public string Name => "get_chatter_ids";
        private Database _database;
        private ILogger _logger;

        public GetChatterIds(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            IList<long> ids = new List<long>();
            string sql = $"SELECT id FROM \"Chatter\"";
            await _database.Execute(sql, data, (r) => ids.Add(r.GetInt64(0)));
            _logger.Information($"Fetched all chatters.");
            return new RequestResult(true, ids, notifyClientsOnAccount: false);
        }
    }
}