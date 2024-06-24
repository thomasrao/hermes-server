// using System.Text.Json;
// using HermesSocketLibrary.db;
// using HermesSocketLibrary.Requests;

// namespace HermesSocketServer.Requests
// {
//     public class BanTTSUser : IRequest
//     {
//         public string Name => "ban_tts_user";
//         private Database _database;
//         private ILogger _logger;

//         public BanTTSUser(Database database, ILogger logger)
//         {
//             _database = database;
//             _logger = logger;
//         }

//         public async Task<RequestResult> Grant(IDictionary<string, object> data)
//         {
//             // if (long.TryParse(data["user"].ToString(), out long user))
//             //     data["user"] = user;
//             // if (data["broadcaster"] is JsonElement b)
//             //     data["broadcaster"] = b.ToString();
//             // if (data["voice"] is JsonElement v)
//             //     data["voice"] = v.ToString();

//             // string sql = "UPDATE \"TtsChatVoice\" (\"broadcasterId\", \"chatterId\", \"ttsVoiceId\") VALUES (@broadcaster, @user, @voice)";
//             // var result = await _database.Execute(sql, data);
//             // _logger.Information($"Selected a tts voice for {data["user"]} in channel {data["broadcaster"]}: {data["voice"]}");
//             return new RequestResult(1 == 1, null);
//         }
//     }
// }