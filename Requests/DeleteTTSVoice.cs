using System.Text.Json;
using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class DeleteTTSVoice : IRequest
    {
        public string Name => "delete_tts_voice";
        private IStore<string, string> _voices;
        private ILogger _logger;

        public DeleteTTSVoice(VoiceStore voices, ILogger logger)
        {
            _voices = voices;
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

            _voices.Remove(data["voice"].ToString());
            _logger.Information($"Deleted a voice by id [voice id: {data["voice"]}]");
            return new RequestResult(true, null);
        }
    }
}