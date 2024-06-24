using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class DeleteTTSVoice : IRequest
    {
        public string Name => "delete_tts_voice";
        private Database _database;
        private ILogger _logger;

        public DeleteTTSVoice(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();

            string sql = "DELETE FROM \"TtsVoice\" WHERE id = @voice";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Deleted a voice by id: {data["voice"]}");
            return new RequestResult(result == 1, null);
        }
    }
}