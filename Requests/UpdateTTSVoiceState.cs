using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSVoiceState : IRequest
    {
        public string Name => "update_tts_voice_state";
        private Database _database;
        private ILogger _logger;

        public UpdateTTSVoiceState(Database database, ILogger logger)
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

            if (data["voice"] is JsonElement voice)
                data["voice"] = voice.ToString();
            if (data["state"] is JsonElement state)
                data["state"] = state.ToString() == "True";
            data["user"] = sender;

            string sql = "INSERT INTO \"TtsVoiceState\" (\"userId\", \"ttsVoiceId\", state) VALUES (@user, @voice, @state) ON CONFLICT (\"userId\", \"ttsVoiceId\") DO UPDATE SET state = @state";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Updated voice's [voice id: {data["voice"]}] state [new state: {data["state"]}][channel: {data["user"]}]");
            return new RequestResult(result == 1, null);
        }
    }
}