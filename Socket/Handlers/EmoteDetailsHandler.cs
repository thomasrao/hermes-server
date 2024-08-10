using HermesSocketLibrary.db;
using HermesSocketLibrary.Socket.Data;
using Npgsql;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class EmoteDetailsHandler : ISocketHandler
    {
        public int OperationCode { get; } = 7;
        private readonly Database _database;
        private readonly HashSet<string> _emotes;
        private readonly ILogger _logger;
        private readonly object _lock;

        public EmoteDetailsHandler(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
            _emotes = new HashSet<string>(501);
            _lock = new object();
        }

        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not EmoteDetailsMessage data || sender.Id == null)
                return;

            if (data.Emotes == null)
                return;

            if (!data.Emotes.Any())
                return;

            lock (_lock)
            {
                foreach (var entry in data.Emotes)
                {
                    if (_emotes.Contains(entry.Key))
                    {
                        _emotes.Remove(entry.Key);
                        continue;
                    }

                    _emotes.Add(entry.Key);
                }
            }

            int rows = 0;
            string sql = "INSERT INTO \"Emote\" (id, name) VALUES (@idd, @name)";
            using (var connection = await _database.DataSource.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    foreach (var entry in data.Emotes)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("idd", entry.Key);
                        command.Parameters.AddWithValue("name", entry.Value);

                        await command.PrepareAsync();
                        try
                        {
                            rows += await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Failed to add emote detail: " + entry.Key + " -> " + entry.Value);
                        }
                    }
                }
            }


        }
    }
}