using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetTTSUsers : IRequest
    {
        public string Name => "get_tts_users";
        private readonly Database _database;
        private readonly ILogger _logger;

        public GetTTSUsers(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            data["user"] = sender;

            IDictionary<long, string> users = new Dictionary<long, string>();
            string sql = $"SELECT \"ttsVoiceId\", \"chatterId\" FROM \"TtsChatVoice\" WHERE \"userId\" = @user";
            await _database.Execute(sql, data, (r) => users.Add(r.GetInt64(1), r.GetString(0)));
            _logger.Information($"Fetched all chatters' selected tts voice for channel {data["user"]}.");
            return new RequestResult(true, users, notifyClientsOnAccount: false);
        }
    }
}