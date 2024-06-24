using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class CreateTTSUser : IRequest
    {
        public string Name => "create_tts_user";
        private Database _database;
        private ILogger _logger;

        public CreateTTSUser(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            if (long.TryParse(data["chatter"].ToString(), out long chatter))
                data["chatter"] = chatter;
            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();
            data["user"] = sender;

            var check = await _database.ExecuteScalar("SELECT state FROM \"TtsVoiceState\" WHERE \"userId\" = @user AND \"ttsVoiceId\" = @voice", data) ?? false;
            if (check is not bool state || !state){
                return new RequestResult(false, null);
            }
            
            string sql = "INSERT INTO \"TtsChatVoice\" (\"userId\", \"chatterId\", \"ttsVoiceId\") VALUES (@user, @chatter, @voice)";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Selected a tts voice for {data["chatter"]} in channel {data["user"]}: {data["voice"]}");
            return new RequestResult(result == 1, null);
        }
    }
}