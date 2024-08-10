using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSUser : IRequest
    {
        public string Name => "update_tts_user";

        private readonly ServerConfiguration _configuration;
        private readonly Database _database;
        private readonly ILogger _logger;

        public UpdateTTSUser(ServerConfiguration configuration, Database database, ILogger logger)
        {
            _configuration = configuration;
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            if (data == null)
            {
                _logger.Warning("Data received from request is null. Ignoring it.");
                return new RequestResult(false, null);
            }

            if (long.TryParse(data["chatter"].ToString(), out long chatterId))
                data["chatter"] = chatterId;
            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();
            data["user"] = sender;

            var check = await _database.ExecuteScalar("SELECT state FROM \"TtsVoiceState\" WHERE \"userId\" = @user AND \"ttsVoiceId\" = @voice", data) ?? false;
            if ((check is not bool state || !state) && chatterId != _configuration.OwnerId)
            {
                return new RequestResult(false, null);
            }

            string sql = "UPDATE \"TtsChatVoice\" SET \"ttsVoiceId\" = @voice WHERE \"userId\" = @user AND \"chatterId\" = @chatter";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Updated chatter's [chatter: {data["chatter"]}] selected tts voice [voice: {data["voice"]}] in channel [channel: {sender}]");
            return new RequestResult(result == 1, null);
        }
    }
}