using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSVoice : IRequest
    {
        public string Name => "update_tts_voice";
        private Database _database;
        private ILogger _logger;

        public UpdateTTSVoice(Database database, ILogger logger)
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

            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();
            if (data["voiceid"] is JsonElement id)
                data["voiceid"] = id.ToString();

            string sql = "UPDATE \"TtsVoice\" SET name = @voice WHERE id = @voiceid";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Updated voice's [voice id: {data["voiceid"]}] name [new name: {data["voice"]}]");
            return new RequestResult(result == 1, null);
        }
    }
}