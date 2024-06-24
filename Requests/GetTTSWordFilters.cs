using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetTTSWordFilters : IRequest
    {
        public string Name => "get_tts_word_filters";
        private readonly Database _database;
        private readonly ILogger _logger;

        public GetTTSWordFilters(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            data["user"] = sender;

            IList<TTSWordFilter> filters = new List<TTSWordFilter>();
            string sql = $"SELECT id, search, replace FROM \"TtsWordFilter\" WHERE \"userId\" = @user";
            await _database.Execute(sql, data, (r) => filters.Add(new TTSWordFilter()
            {
                Id = r.GetString(0),
                Search = r.GetString(1),
                Replace = r.GetString(2)
            }));
            return new RequestResult(true, filters, notifyClientsOnAccount: false);
        }
    }
}