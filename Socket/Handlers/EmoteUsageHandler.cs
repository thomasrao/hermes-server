using HermesSocketLibrary.db;
using HermesSocketLibrary.Socket.Data;
using Npgsql;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class EmoteUsageHandler : ISocketHandler
    {
        public int OpCode { get; } = 8;

        private readonly Database _database;
        private readonly HashSet<string> _history;
        private readonly EmoteUsageMessage[] _array;
        private readonly ILogger _logger;

        private int _index;

        public EmoteUsageHandler(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
            _history = new HashSet<string>(101);
            _array = new EmoteUsageMessage[100];
            _index = -1;
        }


        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not EmoteUsageMessage data)
                return;

            lock (_logger)
            {
                if (_history.Contains(data.MessageId))
                {
                    return;
                }
                _history.Add(data.MessageId);

                if (_index >= _array.Length - 1)
                    _index = -1;

                _index = (_index + 1) % _array.Length;
                if (_array[_index] != null)
                    _history.Remove(data.MessageId);

                _array[_index] = data;
            }

            int rows = 0;
            string sql = "INSERT INTO \"EmoteUsageHistory\" (timestamp, \"broadcasterId\", \"emoteId\", \"chatterId\") VALUES (@time, @broadcaster, @emote, @chatter)";
            using (var connection = await _database.DataSource.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    foreach (var entry in data.Emotes)
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("time", data.DateTime);
                        command.Parameters.AddWithValue("broadcaster", data.BroadcasterId);
                        command.Parameters.AddWithValue("emote", entry);
                        command.Parameters.AddWithValue("chatter", data.ChatterId);

                        await command.PrepareAsync();
                        rows += await command.ExecuteNonQueryAsync();
                    }
                }
            }

            _logger.Information($"Tracked {rows} emote(s) to history.");
        }
    }
}