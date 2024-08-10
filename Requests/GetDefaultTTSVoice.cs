using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetDefaultTTSVoice : IRequest
    {
        public string Name => "get_default_tts_voice";
        private Database _database;
        private ILogger _logger;

        public GetDefaultTTSVoice(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            string sql = $"SELECT \"ttsDefaultVoice\" FROM \"User\" WHERE id = @user";
            string? value = (string?)await _database.ExecuteScalar(sql, temp);
            _logger.Information($"Fetched the default TTS voice for channel [channel: {sender}]");
            return new RequestResult(true, value, notifyClientsOnAccount: false);
        }
    }
}