using Microsoft.Data.Sqlite;
using RenPyReader.DataModels;
using SQLitePCL;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace RenPyReader.Services
{
    internal class SQLiteService : ISQLiteService
    {
        private const string DefaultDatabaseName = "RenPyReader";

        private const string EventsDefaultTableName = "events";

        private const string ScenesDefaultTableName = "scenes";

        private const string SoundsDefaultTableName = "sounds";

        private const string MusicsDefaultTableName = "musics";

        private readonly string[] DefaultBaseTableNames =
        [
            EventsDefaultTableName,
            ScenesDefaultTableName,
            SoundsDefaultTableName,
            MusicsDefaultTableName
        ];

        private const string ImagesDefaultTableName = "images";

        private const string AudiosDefaultTableName = "audios";

        private readonly string[] DefaultBinaryTableNames =
        [
            ImagesDefaultTableName,
            AudiosDefaultTableName
        ];

        private const string DocumentsDefaultTableName = "documents";

        private const string CharactersDefaultTableName = "characters";

        private readonly SqliteConnection? _connection;

        private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

        public SQLiteService()
        {
            Batteries.Init();
            _connection = CreateConnection(DefaultDatabaseName);
            if (_connection != null)
            {
                EnsureTablesExist();
            }
        }

        private static SqliteConnection? CreateConnection(string databaseName)
        {
            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                $"{databaseName}.db");
            if (!File.Exists(databasePath))
            {
                File.Create(databasePath).Dispose();
            }

            try
            {
                var connectionString = $"Data Source={databasePath}";
                var connection = new SqliteConnection(connectionString);
                connection.Open();

                return connection;
            }
            catch
            {
                return null;
            }
        }

        private void EnsureTablesExist()
        {
            foreach (var tableName in DefaultBaseTableNames)
            {
                CreateBaseTableIfNotExist(tableName);
            }

            foreach (var tableName in DefaultBinaryTableNames)
            {
                CreateBinaryTableIfNotExist(tableName);
            }

            CreateFTS5TableIfNotExist();
            CreateCharacterTableIfNotExist();
        }

        private void CreateBaseTableIfNotExist(string tableName)
        {
            ExecuteNonQueryCommand($@"CREATE TABLE IF NOT EXISTS {tableName} (ID INTEGER PRIMARY KEY, Name TEXT NOT NULL, ParentName TEXT NOT NULL, LineIndex INTEGER NOT NULL);");
        }

        private void CreateBinaryTableIfNotExist(string tableName)
        {
            ExecuteNonQueryCommand($@"CREATE TABLE IF NOT EXISTS {tableName} (Name TEXT PRIMARY KEY, Content BLOB NOT NULL);");
        }

        private void CreateFTS5TableIfNotExist()
        {
            ExecuteNonQueryCommand($@"CREATE VIRTUAL TABLE IF NOT EXISTS {DocumentsDefaultTableName} USING fts5(title, content);");
        }

        private void CreateCharacterTableIfNotExist()
        {
            ExecuteNonQueryCommand($@"CREATE TABLE IF NOT EXISTS {CharactersDefaultTableName} (Code TEXT PRIMARY KEY, Name TEXT NOT NULL, Color TEXT NOT NULL);");
        }

        private void ExecuteNonQueryCommand(string commandText)
        {
            using (var command = _connection!.CreateCommand())
            {
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        async Task<List<string>> ISQLiteService.GetTableNamesAsync()
        {
            var result = new List<string>();
            if (_connection == null)
            {
                return result;
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }

            return result;
        }

        async Task<List<Dictionary<string, string>>> ISQLiteService.GetTableDataAsync(string tableName)
        {
            var result = new List<Dictionary<string, string>>();
            if (_connection == null)
            {
                return result;
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @$"SELECT * FROM {tableName};";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Dictionary<string, string> row = new();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            row.Add(reader.GetName(i), reader.GetString(i));
                        }

                        result.Add(row);
                    }
                }
            }

            return result;
        }

        async Task ISQLiteService.SaveDocumentAsync(string title, string content)
        {
            if (_connection == null)
            {
                return;
            }

            using var transaction = _connection.BeginTransaction();
            try
            {
                long rowId;
                await using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = $@"INSERT OR REPLACE INTO {DocumentsDefaultTableName} (title, content) VALUES (@title, @content);";
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@content", content);

                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid();";

                    var result = command.ExecuteScalar();
                    rowId = result != null ? (long)result : -1;
                    transaction.Commit();
                }

                return;
            }
            catch
            {
                transaction.Rollback();
            }
        }

        async Task ISQLiteService.BatchInsertOrIgnoreBaseTable(string tableName, List<RenPyBase> renPyBaseEntries)
        {
            if (_connection == null)
            {
                return;
            }

            await using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    await using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO {tableName} (Name, ParentName, LineIndex) VALUES (@Name, @ParentName, @LineIndex)";
                        var nameParam = command.Parameters.Add("@Name", SqliteType.Text);
                        var parentNameParam = command.Parameters.Add("@ParentName", SqliteType.Text);
                        var lineIndexParam = command.Parameters.Add("@LineIndex", SqliteType.Integer);

                        foreach (var entry in renPyBaseEntries)
                        {
                            command.Parameters["@Name"].Value = entry.Name;
                            command.Parameters["@ParentName"].Value = entry.Parent;
                            command.Parameters["@LineIndex"].Value = entry.Index;
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

        public async Task BatchInsertOrIgnoreCharacters(List<RenPyCharacter> characters)
        {
            if (_connection == null)
            {
                return;
            }

            await using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    await using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = $"INSERT INTO characters (Code, Name, Color) VALUES (@Code, @Name, @Color)";
                        var codeParam = command.Parameters.Add("@Code", SqliteType.Text);
                        var nameParam = command.Parameters.Add("@Name", SqliteType.Text);
                        var colorParam = command.Parameters.Add("@Color", SqliteType.Text);

                        foreach (var character in characters)
                        {
                            command.Parameters["@Code"].Value = character.Code;
                            command.Parameters["@Name"].Value = character.Name;
                            command.Parameters["@Color"].Value = character.ColorHTML;
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

        async Task<bool> ISQLiteService.DoesDocumentExistAsync(string title)
        {
            var doesExist = false;
            if (_connection == null)
            {
                return doesExist;
            }

            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"SELECT title FROM {DocumentsDefaultTableName} WHERE title = @title;";
                command.Parameters.AddWithValue("@title", title);
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var result = reader.GetString(0);
                        doesExist = !string.IsNullOrEmpty(result);
                    }
                }
            }

            return doesExist;
        }

        async Task<string> ISQLiteService.GetDocumentContentAsync(string title)
        {
            var result = string.Empty;
            if (_connection == null)
            {
                return result;
            }

            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"SELECT content FROM {DocumentsDefaultTableName} WHERE title = @title;";
                command.Parameters.AddWithValue("@title", title);
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result = reader.GetString(0);
                    }
                }
            }
            return result;
        }

        async Task<List<(long rowID, string title)>> ISQLiteService.GetAllDocumentTitlesAsync()
        {
            var documents = new List<(long RowId, string Title)>();
            if (_connection == null)
            {
                return documents;
            }

            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = $@"SELECT rowid, title FROM {DocumentsDefaultTableName};";
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var rowID = reader.GetInt64(0);
                        var title = reader.GetString(1);
                        documents.Add((rowID, title));
                    }
                }
            }

            return documents;
        }

        public async Task<List<RenPySearchResult>> QuickSearchAsync(string searchPhrase, bool useFullWord = false)
        {
            var searchResults = new List<RenPySearchResult>();
            if (_connection == null)
            {
                return searchResults;
            }   

            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT title, content, rowid FROM documents WHERE content MATCH @searchPhrase;";
                command.Parameters.AddWithValue("@searchPhrase", searchPhrase);
                using var reader = await command.ExecuteReaderAsync();
                Regex? wordRegex = useFullWord
                ? _regexCache.GetOrAdd(searchPhrase, key => new Regex($@"\b{Regex.Escape(key)}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled))
                : null;

                while (await reader.ReadAsync())
                {
                    var title       = reader.GetString(0);
                    var content     = reader.GetString(1);
                    var parentId    = reader.GetInt32(2);

                    int lineNumber = 0;
                    string? currentLine;
                    using var stringReader = new StringReader(content);
                    while ((currentLine = await stringReader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        bool containsSearchTerm = wordRegex != null
                            ? wordRegex.IsMatch(currentLine)
                            : currentLine.Contains(searchPhrase, StringComparison.OrdinalIgnoreCase);

                        if (containsSearchTerm)
                        {
                            searchResults.Add(new RenPySearchResult(title, currentLine, lineNumber, parentId));
                        }
                    }
                }
            }

            return searchResults;
        }
    }
}