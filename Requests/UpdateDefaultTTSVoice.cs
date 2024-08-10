using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateDefaultTTSVoice : IRequest
    {
        public string Name => "update_default_tts_voice";
        private Database _database;
        private ILogger _logger;

        public UpdateDefaultTTSVoice(Database database, ILogger logger)
        {
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

            data["user"] = data["user"].ToString();
            data["voice"] = data["voice"].ToString();

            string sql = $"UPDATE \"User\" SET ttsDefaultVoice = @voice WHERE id = @user";
            await _database.Execute(sql, data);
            _logger.Information($"Updated default TTS voice for channel [channel: {sender}][voice: {data["voice"]}]");
            return new RequestResult(true, null);
        }
    }
}