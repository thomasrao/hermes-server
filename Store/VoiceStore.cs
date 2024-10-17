using System.Collections.Immutable;
using System.Text;
using HermesSocketLibrary.db;
using HermesSocketServer.Validators;

namespace HermesSocketServer.Store
{
    public class VoiceStore : IStore<string, string>
    {
        private readonly Database _database;
        private readonly IValidator _voiceIdValidator;
        private readonly IValidator _voiceNameValidator;
        private readonly Serilog.ILogger _logger;
        private readonly IDictionary<string, string> _voices;
        private readonly IList<string> _added;
        private readonly IList<string> _modified;
        private readonly IList<string> _deleted;
        private readonly object _lock;

        public DateTime PreviousSave;


        public VoiceStore(Database database, VoiceIdValidator voiceIdValidator, VoiceNameValidator voiceNameValidator, Serilog.ILogger logger)
        {
            _database = database;
            _voiceIdValidator = voiceIdValidator;
            _voiceNameValidator = voiceNameValidator;
            _logger = logger;
            _voices = new Dictionary<string, string>();
            _added = new List<string>();
            _modified = new List<string>();
            _deleted = new List<string>();
            _lock = new object();

            PreviousSave = DateTime.UtcNow;
        }

        public string? Get(string key)
        {
            if (_voices.TryGetValue(key, out var voice))
                return voice;
            return null;
        }

        public IEnumerable<string> Get()
        {
            return _voices.Values.ToImmutableList();
        }

        public async Task Load()
        {
            string sql = "SELECT id, name FROM \"TtsVoice\";";
            await _database.Execute(sql, new Dictionary<string, object>(), (reader) =>
            {
                var id = reader.GetString(0);
                var name = reader.GetString(1);
                _voices.Add(id, name);
            });
            _logger.Information($"Loaded {_voices.Count} TTS voices from database.");
        }

        public void Remove(string? key)
        {
            if (key == null)
                return;

            lock (_lock)
            {
                if (_voices.ContainsKey(key))
                {
                    _voices.Remove(key);
                    if (!_added.Remove(key))
                    {
                        _modified.Remove(key);
                        if (!_deleted.Contains(key))
                            _deleted.Add(key);
                    }
                }
            }
        }

        public async Task<bool> Save()
        {
            var changes = false;
            var sb = new StringBuilder();
            var sql = "";

            if (_added.Any())
            {
                int count = _added.Count;
                sb.Append("INSERT INTO \"TtsVoice\" (id, name) VALUES ");
                lock (_lock)
                {
                    foreach (var voiceId in _added)
                    {
                        string voice = _voices[voiceId];
                        sb.Append("('")
                            .Append(voiceId)
                            .Append("','")
                            .Append(voice)
                            .Append("'),");
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
                sb.Append("UPDATE \"TtsVoice\" as t SET name = c.name FROM (VALUES ");
                lock (_lock)
                {
                    foreach (var voiceId in _modified)
                    {
                        string voice = _voices[voiceId];
                        sb.Append("('")
                            .Append(voiceId)
                            .Append("','")
                            .Append(voice)
                            .Append("'),");
                    }
                    sb.Remove(sb.Length - 1, 1)
                        .Append(") AS c(id, name) WHERE id = c.id;");


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
                sb.Append("DELETE FROM \"TtsVoice\" WHERE id IN (");
                lock (_lock)
                {
                    foreach (var voiceId in _deleted)
                    {
                        sb.Append("'")
                            .Append(voiceId)
                            .Append("',");
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

        public bool Set(string? key, string? value)
        {
            if (key == null || value == null)
                return false;
            _voiceNameValidator.Check(value);

            lock (_lock)
            {
                if (_voices.TryGetValue(key, out var voice))
                {

                    if (voice != value)
                    {
                        _voices[key] = value;
                        if (!_added.Contains(key) && !_modified.Contains(key))
                            _modified.Add(key);
                    }
                }
                else
                {
                    _voiceIdValidator.Check(key);
                    _voices.Add(key, value);
                    if (!_deleted.Remove(key) && !_added.Contains(key))
                        _added.Add(key);
                }
            }

            return true;
        }
    }
}