using HermesSocketLibrary.db;
using HermesSocketLibrary.Socket.Data;
using ILogger = Serilog.ILogger;

namespace HermesSocketServer.Socket.Handlers
{
    public class ChatterHandler : ISocketHandler
    {
        public int OpCode { get; } = 6;
        private readonly Database _database;
        private readonly HashSet<long> _chatters;
        private readonly ChatterMessage[] _array;
        private readonly ILogger _logger;

        private readonly object _lock;
        private int _index;

        public ChatterHandler(Database database, ILogger logger)
        {
            _database = database;
            _logger = logger;
            _chatters = new HashSet<long>(1001);
            _array = new ChatterMessage[1000];
            _index = -1;
            _lock = new object();
        }

        public async Task Execute<T>(WebSocketUser sender, T message, HermesSocketManager sockets)
        {
            if (message is not ChatterMessage data)
                return;

            lock (_lock)
            {
                if (_chatters.Contains(data.Id))
                    return;

                _chatters.Add(data.Id);

                if (_index == _array.Length - 1)
                    _index = -1;

                _array[++_index] = data;
            }

            try
            {
                string sql = "INSERT INTO \"Chatter\" (id, name) VALUES (@idd, @name)";
                await _database.Execute(sql, new Dictionary<string, object>() { { "idd", data.Id }, { "name", data.Name } });
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to add chatter.");
            }
        }
    }
}