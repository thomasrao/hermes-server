using System.Text.Json;
using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class CreateTTSVoice : IRequest
    {
        public string Name => "create_tts_voice";
        private IStore<string, string> _voices;
        private ILogger _logger;
        private Random _random;

        public CreateTTSVoice(VoiceStore voices, ILogger logger)
        {
            _voices = voices;
            _logger = logger;
            _random = new Random();
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
            else
                return new RequestResult(false, "Invalid voice name.");

            string id = RandomString(25);

            _voices.Set(id, data["voice"].ToString());
            _logger.Information($"Added a new voice [voice: {data["voice"]}][voice id: {id}]");

            return new RequestResult(true, id);
        }

        private string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}