using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetEnabledTTSVoices : IRequest
    {
        public string Name => "get_enabled_tts_voices";

        private readonly Database _database;
        private readonly ILogger _logger;

        public GetEnabledTTSVoices(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            var voices = new List<string>();
            string sql = $"SELECT v.name FROM \"TtsVoiceState\" s "
                + "INNER JOIN \"TtsVoice\" v ON s.\"ttsVoiceId\" = v.id "
                + "WHERE \"userId\" = @user AND state = true";
            await _database.Execute(sql, temp, (r) => voices.Add(r.GetString(0)));
            _logger.Information($"Fetched all enabled TTS voice for channel [channel: {sender}]");
            return new RequestResult(true, voices, notifyClientsOnAccount: false);
        }
    }
}