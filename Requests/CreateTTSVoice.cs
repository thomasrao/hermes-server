using System.Text.Json;
using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class CreateTTSVoice : IRequest
    {
        public string Name => "create_tts_voice";
        private Database _database;
        private ILogger _logger;
        private Random _random;

        public CreateTTSVoice(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
            _random = new Random();
        }


        public async Task<RequestResult> Grant(string sender, IDictionary<string, object> data)
        {
            string id = RandomString(25);
            data.Add("idd", id);

            if (data["voice"] is JsonElement v)
                data["voice"] = v.ToString();

            string sql = "INSERT INTO \"TtsVoice\" (id, name) VALUES (@idd, @voice)";
            var result = await _database.Execute(sql, data);
            _logger.Information($"Added a new voice: {data["voice"]} (id: {data["idd"]})");

            data.Remove("idd");
            return new RequestResult(result == 1, id);
        }

        private string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}