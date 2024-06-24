using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSUser : IRequest
    {
        public string Name => "update_tts_user";
        private readonly Database _database;
        private readonly ILogger _logger;

        public UpdateTTSUser(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            if (long.TryParse(data["chatter"].ToString(), out long chatterId))
                data["chatter"] = chatterId;
            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();
            data["user"] = sender;

            var check = await _database.ExecuteScalar("SELECT state FROM \"TtsVoiceState\" WHERE \"userId\" = @user AND \"ttsVoiceId\" = @voice", data) ?? false;
            if (check is not bool state || !state)
            {
                return new RequestResult(false, null);
            }

            string sql = "UPDATE \"TtsChatVoice\" SET \"ttsVoiceId\" = @voice WHERE \"userId\" = @user AND \"chatterId\" = @chatter";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Updated {data["chatter"]}'s selected tts voice to {data["voice"]} in channel {data["user"]}.");
            return new RequestResult(result == 1, null);
        }
    }
}