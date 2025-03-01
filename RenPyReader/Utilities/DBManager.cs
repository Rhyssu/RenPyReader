using Microsoft.Data.Sqlite;
using RenPyReader.DataModels;
using SQLitePCL;

namespace RenPyReader.Utilities
{
    internal class DBManager
    {
        private readonly string _connectionString;

        private SqliteConnection _connection;

        public DBManager(string databaseName)
        {
            Batteries.Init();
            var databasePath = Path.Combine(
                FileSystem.AppDataDirectory, $"{databaseName}.db");
            if (!File.Exists(databasePath))
            {
                File.Create(databasePath).Dispose();
            }

            _connectionString = $"Data Source={databasePath}";
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();

            CreateTablesIfNotExist();
        }

        public async Task InsertImageAsync(RenPyImage renPyImage)
        {
            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = DBCommand.InsertRenPyBinaryBase.ToSQLite("images");
                command.Parameters.AddWithValue("@Name", renPyImage.Name);
                command.Parameters.AddWithValue("@Content", renPyImage.Content);

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        public async Task InsertAudioAsync(RenPyAudio renPyAudio)
        {
            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = DBCommand.InsertRenPyBinaryBase.ToSQLite("audios");
                command.Parameters.AddWithValue("@Name", renPyAudio.Name);
                command.Parameters.AddWithValue("@Content", renPyAudio.Content);

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }

        public async Task<HashSet<string>> GetBinaryEntriesNamesAsync(string baseName)
        {
            var names = new HashSet<string>();

            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = DBCommand.GetRenPyBinaryBaseNames.ToSQLite(baseName);
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        names.Add(reader.GetString(0));
                    }
                }
            }

            return names;
        }

        private void CreateTablesIfNotExist()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = DBCommand.CreateRenPyBaseTable.ToSQLite("events");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBaseTable.ToSQLite("scenes");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBaseTable.ToSQLite("sounds");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBaseTable.ToSQLite("musics");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBinaryBaseTable.ToSQLite("images");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBinaryBaseTable.ToSQLite("audios");
                command.ExecuteNonQuery();
            }
        }
    }
}