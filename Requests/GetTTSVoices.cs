using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetTTSVoices : IRequest
    {
        public string Name => "get_tts_voices";
        private Database _database;
        private ILogger _logger;

        public GetTTSVoices(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            IList<VoiceDetails> voices = new List<VoiceDetails>();
            string sql = "SELECT id, name FROM \"TtsVoice\"";
            await _database.Execute(sql, data, (r) => voices.Add(new VoiceDetails()
            {
                Id = r.GetString(0),
                Name = r.GetString(1)
            }));
            _logger.Information("Fetched all TTS voices.");
            return new RequestResult(true, voices, notifyClientsOnAccount: false);
        }
    }
}