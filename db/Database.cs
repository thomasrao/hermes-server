using HermesSocketServer;
using Npgsql;

namespace HermesSocketLibrary.db
{
    public class Database
    {
        private NpgsqlDataSource _source;
        private ServerConfiguration _configuration;

        public NpgsqlDataSource DataSource { get => _source; }


        public Database(ServerConfiguration configuration)
        {
            NpgsqlDataSourceBuilder builder = new NpgsqlDataSourceBuilder(configuration.Database.ConnectionString);
            _source = builder.Build();
        }

        public async Task Execute(string sql, IDictionary<string, object>? values, Action<NpgsqlDataReader> reading)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    if (values != null)
                    {
                        foreach (var entry in values)
                            command.Parameters.AddWithValue(entry.Key, entry.Value);
                    }
                    await command.PrepareAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reading(reader);
                        }
                    }
                }
            }
        }

        public async Task Execute(string sql, Action<NpgsqlCommand> action, Action<NpgsqlDataReader> reading)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    action(command);
                    await command.PrepareAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            reading(reader);
                        }
                    }
                }
            }
        }

        public async Task<int> Execute(string sql, IDictionary<string, object>? values)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    if (values != null)
                    {
                        foreach (var entry in values)
                            command.Parameters.AddWithValue(entry.Key, entry.Value);
                    }
                    await command.PrepareAsync();

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> Execute(string sql, Action<NpgsqlCommand> prepare)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    prepare(command);
                    await command.PrepareAsync();

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<object?> ExecuteScalar(string sql, IDictionary<string, object>? values = null)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    if (values != null)
                    {
                        foreach (var entry in values)
                            command.Parameters.AddWithValue(entry.Key, entry.Value);
                    }

                    await command.PrepareAsync();

                    return await command.ExecuteScalarAsync();
                }
            }
        }

        public async Task<object?> ExecuteScalar(string sql, Action<NpgsqlCommand> action)
        {
            using (var connection = await _source.OpenConnectionAsync())
            {
                using (var command = new NpgsqlCommand(sql, connection))
                {
                    action(command);
                    await command.PrepareAsync();

                    return await command.ExecuteScalarAsync();
                }
            }
        }
    }
}