using HermesSocketLibrary.Requests;
using HermesSocketServer.Store;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetTTSUsers : IRequest
    {
        public string Name => "get_tts_users";
        private ChatterStore _chatters;
        private ILogger _logger;

        public GetTTSUsers(ChatterStore chatters, ILogger logger)
        {
            _chatters = chatters;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = _chatters.Get(sender);
            _logger.Information($"Fetched all chatters' selected tts voice for channel [channel: {sender}]");
            return new RequestResult(true, temp, notifyClientsOnAccount: false);
        }
    }
}