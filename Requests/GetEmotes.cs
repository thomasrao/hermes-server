using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetEmotes : IRequest
    {
        public string Name => "get_emotes";
        private Database _database;
        private ILogger _logger;

        public GetEmotes(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            IList<EmoteInfo> emotes = new List<EmoteInfo>();
            string sql = $"SELECT id, name FROM \"Emote\"";
            await _database.Execute(sql, (IDictionary<string, object>?) null, (r) => emotes.Add(new EmoteInfo()
            {
                Id = r.GetString(0),
                Name = r.GetString(1)
            }));
            _logger.Information($"Fetched all emotes for channel [channel: {sender}]");
            return new RequestResult(true, emotes, notifyClientsOnAccount: false);
        }
    }
}