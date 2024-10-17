using System.Text.Json;
using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class UpdateTTSVoice : IRequest
    {
        public string Name => "update_tts_voice";
        private IStore<string, string> _voices;
        private ILogger _logger;

        public UpdateTTSVoice(VoiceStore voices, ILogger logger)
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
            if (data["voiceid"] is JsonElement id)
                data["voiceid"] = id.ToString();

            _voices.Set(data["voiceid"].ToString(), data["voice"].ToString());
            _logger.Information($"Updated voice's [voice id: {data["voiceid"]}] name [new name: {data["voice"]}]");
            return new RequestResult(true, null);
        }
    }
}