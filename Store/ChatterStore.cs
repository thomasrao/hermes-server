using System.Collections.Immutable;
using System.Text;
using HermesSocketLibrary.db;

namespace HermesSocketServer.Store
{
    public class ChatterStore : IStore<string, long, string>
    {
        private readonly Database _database;
        private readonly Serilog.ILogger _logger;
        private readonly IDictionary<string, IDictionary<long, string>> _chatters;
        private readonly IDictionary<string, IList<long>> _added;
        private readonly IDictionary<string, IList<long>> _modified;
        private readonly IDictionary<string, IList<long>> _deleted;
        private readonly object _lock;


        public ChatterStore(Database database, Serilog.ILogger logger)
        {
            _database = database;
            _logger = logger;
            _chatters = new Dictionary<string, IDictionary<long, string>>();
            _added = new Dictionary<string, IList<long>>();
            _modified = new Dictionary<string, IList<long>>();
            _deleted = new Dictionary<string, IList<long>>();
            _lock = new object();
        }

        public string? Get(string user, long key)
        {
            if (!_chatters.TryGetValue(user, out var broadcaster))
                return null;
            if (broadcaster.TryGetValue(key, out var chatter))
                return chatter;
            return null;
        }

        public IEnumerable<string> Get()
        {
            return _chatters.Select(c => c.Value).SelectMany(c => c.Values).ToImmutableList();
        }

        public IDictionary<long, string> Get(string user)
        {
            if (_chatters.TryGetValue(user, out var chatters))
                return chatters.ToImmutableDictionary();
            return new Dictionary<long, string>();
        }

        public async Task Load()
        {
            string sql = "SELECT \"chatterId\", \"ttsVoiceId\", \"userId\" FROM \"TtsChatVoice\";";
            await _database.Execute(sql, new Dictionary<string, object>(), (reader) =>
            {
                var chatterId = reader.GetInt64(0);
                var ttsVoiceId = reader.GetString(1);
                var userId = reader.GetString(2);
                if (!_chatters.TryGetValue(userId, out var chatters))
                {
                    chatters = new Dictionary<long, string>();
                    _chatters.Add(userId, chatters);
                }
                chatters.Add(chatterId, ttsVoiceId);
            });
            _logger.Information($"Loaded {_chatters.Count} TTS voices from database.");
        }

        public void Remove(string user, long? key)
        {
            if (key == null)
                return;

            lock (_lock)
            {
                if (_chatters.TryGetValue(user, out var chatters) && chatters.Remove(key.Value))
                {
                    if (!_added.TryGetValue(user, out var added) || !added.Remove(key.Value))
                    {
                        if (_modified.TryGetValue(user, out var modified))
                            modified.Remove(key.Value);
                        if (!_deleted.TryGetValue(user, out var deleted))
                        {
                            deleted = new List<long>();
                            _deleted.Add(user, deleted);
                            deleted.Add(key.Value);
                        }
                        else if (!deleted.Contains(key.Value))
                            deleted.Add(key.Value);
                    }
                }
            }
        }

        public void Remove(string? leftKey, long rightKey)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Save()
        {
            var changes = false;
            var sb = new StringBuilder();
            var sql = "";

            if (_added.Any())
            {
                int count = _added.Count;
                sb.Append("INSERT INTO \"TtsChatVoice\" (\"chatterId\", \"ttsVoiceId\", \"userId\") VALUES ");
                lock (_lock)
                {
                    foreach (var broadcaster in _added)
                    {
                        var userId = broadcaster.Key;
                        var user = _chatters[userId];
                        foreach (var chatterId in broadcaster.Value)
                        {
                            var voiceId = user[chatterId];
                            sb.Append("(")
                                .Append(chatterId)
                                .Append(",'")
                                .Append(voiceId)
                                .Append("','")
                                .Append(userId)
                                .Append("'),");
                        }
                    }
                    sb.Remove(sb.Length - 1, 1)
                        .Append(';');

                    sql = sb.ToString();
                    sb.Clear();
                    _added.Clear();
                }

                try
                {
                    _logger.Debug($"About to save {count} voices to database.");
                    await _database.ExecuteScalar(sql);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to save TTS voices on database: " + sql);
                }
                changes = true;
            }

            if (_modified.Any())
            {
                int count = _modified.Count;
                sb.Append("UPDATE \"TtsChatVoice\" as t SET \"ttsVoiceId\" = c.\"ttsVoiceId\" FROM (VALUES ");
                lock (_lock)
                {
                    foreach (var broadcaster in _modified)
                    {
                        var userId = broadcaster.Key;
                        var user = _chatters[userId];
                        foreach (var chatterId in broadcaster.Value)
                        {
                            var voiceId = user[chatterId];
                            sb.Append("(")
                                .Append(chatterId)
                                .Append(",'")
                                .Append(voiceId)
                                .Append("','")
                                .Append(userId)
                                .Append("'),");
                        }
                    }
                    sb.Remove(sb.Length - 1, 1)
                        .Append(") AS c(\"chatterId\", \"ttsVoiceId\", \"userId\") WHERE \"userId\" = c.\"userId\" AND \"chatterId\" = c.\"chatterId\";");

                    sql = sb.ToString();
                    sb.Clear();
                    _modified.Clear();
                }

                try
                {
                    _logger.Debug($"About to update {count} voices on the database.");
                    await _database.ExecuteScalar(sql);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to modify TTS voices on database: " + sql);
                }
                changes = true;
            }

            if (_deleted.Any())
            {
                int count = _deleted.Count;
                sb.Append("DELETE FROM \"TtsChatVoice\" WHERE (\"chatterId\", \"userId\") IN (");
                lock (_lock)
                {
                    foreach (var broadcaster in _deleted)
                    {
                        var userId = broadcaster.Key;
                        var user = _chatters[userId];
                        foreach (var chatterId in broadcaster.Value)
                        {
                            sb.Append("(")
                                .Append(chatterId)
                                .Append(",'")
                                .Append(userId)
                                .Append("'),");
                        }
                    }
                    sb.Remove(sb.Length - 1, 1)
                        .Append(");");

                    sql = sb.ToString();
                    sb.Clear();
                    _deleted.Clear();
                }

                try
                {
                    _logger.Debug($"About to delete {count} voices from the database.");
                    await _database.ExecuteScalar(sql);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to modify TTS voices on database: " + sql);
                }
                changes = true;
            }
            return changes;
        }

        public bool Set(string? user, long key, string? value)
        {
            if (user == null || value == null)
                return false;

            lock (_lock)
            {
                if (!_chatters.TryGetValue(user, out var broadcaster))
                {
                    broadcaster = new Dictionary<long, string>();
                    _chatters.Add(user, broadcaster);
                }

                if (broadcaster.TryGetValue(key, out var chatter))
                {
                    if (chatter != value)
                    {
                        broadcaster[key] = value;
                        if (!_added.TryGetValue(user, out var added) || !added.Contains(key))
                        {
                            if (!_modified.TryGetValue(user, out var modified))
                            {
                                modified = new List<long>();
                                _modified.Add(user, modified);
                                modified.Add(key);
                            }
                            else if (!modified.Contains(key))
                                modified.Add(key);
                        }
                    }
                }
                else
                {
                    broadcaster.Add(key, value);
                    _added.TryAdd(user, new List<long>());

                    if (!_deleted.TryGetValue(user, out var deleted) || !deleted.Remove(key))
                    {
                        if (!_added.TryGetValue(user, out var added))
                        {
                            added = new List<long>();
                            _added.Add(user, added);
                            added.Add(key);
                        }
                        else if (!added.Contains(key))
                            added.Add(key);
                    }
                }
            }

            return true;
        }
    }
}