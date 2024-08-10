using HermesSocketLibrary.db;
using HermesSocketLibrary.Requests;
using HermesSocketLibrary.Requests.Messages;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Requests
{
    public class GetPermissions : IRequest
    {
        public string Name => "get_permissions";

        private readonly Database _database;
        private readonly ILogger _logger;

        public GetPermissions(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RequestResult> Grant(string sender, IDictionary<string, object>? data)
        {
            var temp = new Dictionary<string, object>() { { "user", sender } };

            var groups = new List<Group>();
            string sql = $"SELECT id, name, priority FROM \"Group\" WHERE \"userId\" = @user";
            await _database.Execute(sql, temp, (r) => groups.Add(new Group()
            {
                Id = r.GetGuid(0).ToString("D"),
                Name = r.GetString(1),
                Priority = r.GetInt32(2)
            }));

            var groupChatters = new List<GroupChatter>();
            sql = $"SELECT \"groupId\", \"chatterId\", \"chatterId\" FROM \"ChatterGroup\" WHERE \"userId\" = @user";
            await _database.Execute(sql, temp, (r) => groupChatters.Add(new GroupChatter()
            {
                GroupId = r.GetGuid(0).ToString("D"),
                ChatterId = r.GetInt32(1)
            }));

            var groupPermissions = new List<GroupPermission>();
            sql = $"SELECT id, \"groupId\", \"path\", \"allow\" FROM \"GroupPermission\" WHERE \"userId\" = @user";
            await _database.Execute(sql, temp, (r) => groupPermissions.Add(new GroupPermission()
            {
                Id = r.GetGuid(0).ToString("D"),
                GroupId = r.GetGuid(1).ToString("D"),
                Path = r.GetString(2),
                Allow = r.GetBoolean(3)
            }));
            _logger.Information($"Fetched all redemptions for channel [channel: {sender}]");

            var info = new GroupInfo()
            {
                Groups = groups,
                GroupChatters = groupChatters,
                GroupPermissions = groupPermissions
            };
            return new RequestResult(true, info, notifyClientsOnAccount: false);
        }
    }
}