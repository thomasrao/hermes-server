using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class CreateTTSUser : IRequest
    {
        public string Name => "create_tts_user";
        private Database _database;
        private ChatterStore _chatters;
        private ILogger _logger;

        public CreateTTSUser(ChatterStore chatters, Database database, ILogger logger)
        {
            _database = database;
            _chatters = chatters;
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
            else
                return new RequestResult(false, "Invalid Twitch user id");
            
            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();
            else
                return new RequestResult(false, "Invalid voice id");
            
            data["user"] = sender;

            var check = await _database.ExecuteScalar("SELECT state FROM \"TtsVoiceState\" WHERE \"userId\" = @user AND \"ttsVoiceId\" = @voice", data) ?? false;
            if (check is not bool state || !state)
                return new RequestResult(false, "Voice is disabled on this channel.");

            _chatters.Set(sender, chatterId, data["voice"].ToString());
            _logger.Information($"Selected a tts voice [voice: {data["voice"]}] for user [chatter: {data["chatter"]}] in channel [channel: {data["user"]}]");
            return new RequestResult(true, null);
        }
    }
}