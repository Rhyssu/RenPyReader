using Microsoft.Data.Sqlite;
using RenPyReader.Entities;
using RenPyReader.Utilities;

namespace RenPyReader.Database
{
    internal abstract class RenPyDBManagerBase
    {
        private readonly SqliteConnection _connection;

        private readonly string[] tableNames = ["events", "scenes", "sounds", "musics"];

        internal RenPyDBManagerBase(SqliteConnection connection)
        {
            _connection = connection;
            EnsureTablesExist();
        }

        internal void BatchInsertOrIgnore(string tableName, OrderedSet<string> entries)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO {tableName} (Name) VALUES (@Name)";
                        var nameParameter = command.Parameters.Add("@Name", SqliteType.Text);

                        foreach (var entry in entries)
                        {
                            command.Parameters["@Name"].Value = entry;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
        }

        internal void BatchInsertOrIgnoreMap(string tableName, OrderedSet<MapEntry> mapEntries)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO {tableName + "Map"} (ParentRow, ElementRow, LineIndex) VALUES (@ParentRow, @ElementRow, @LineIndex)";
                        var parentRowParameter = command.Parameters.Add("@ParentRow", SqliteType.Integer);
                        var elementIDParameter = command.Parameters.Add("@ElementRow", SqliteType.Integer);
                        var LineIndexParameter = command.Parameters.Add("@LineIndex", SqliteType.Integer);

                        foreach (var (ParentID, ElementID, LineIndex) in mapEntries)
                        {
                            command.Parameters["@ParentRow"].Value = ParentID;
                            command.Parameters["@ElementRow"].Value = ElementID;
                            command.Parameters["@LineIndex"].Value = LineIndex;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
        }

        internal async Task<OrderedSet<string>> GetOrderedSet(string tableName)
        {
            var result = new OrderedSet<string>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT Name from {tableName};";
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }
            return result;
        }

        internal async Task<OrderedSet<Entities.MapEntry>> GetOrderedMap(string tableName)
        {
            var result = new OrderedSet<Entities.MapEntry>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $"SELECT ID, ParentRow, ElementRow from {tableName + "Map"};";
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
                    }
                }
            }
            return result;
        }

        private void CreateTableIfNotExist(string tableName)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"CREATE TABLE IF NOT EXISTS {tableName} (ID INTEGER PRIMARY KEY, Name TEXT NOT NULL UNIQUE);";
                command.ExecuteNonQuery();

                command.CommandText = $@"CREATE TABLE IF NOT EXISTS {tableName + "Map"} (ID INTEGER PRIMARY KEY, ParentRow INTEGER, ElementRow INTEGER, LineIndex INTEGER);";
                command.ExecuteNonQuery();
            }
        }

        private void EnsureTablesExist()
        {
            foreach (var tableName in tableNames)
            {
                CreateTableIfNotExist(tableName);
            }
        }
    }
}