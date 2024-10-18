using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSUser : IRequest
    {
        public string Name => "update_tts_user";

        private readonly ServerConfiguration _configuration;
        private readonly Database _database;
        private ChatterStore _chatters;
        private ILogger _logger;

        public UpdateTTSUser(ChatterStore chatters, Database database, ServerConfiguration configuration, ILogger logger)
        {
            _database = database;
            _chatters = chatters;
            _configuration = configuration;
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

            _chatters.Set(sender, chatterId, data["voice"].ToString());
            _logger.Information($"Updated chatter's [chatter: {data["chatter"]}] selected tts voice [voice: {data["voice"]}] in channel [channel: {sender}]");
            return new RequestResult(true, null);
        }
    }
}