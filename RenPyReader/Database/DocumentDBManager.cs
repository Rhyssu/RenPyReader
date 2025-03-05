using Microsoft.Data.Sqlite;

namespace RenPyReader.Database
{
    internal abstract class DocumentDBManager : RenPyDBBinaryManager
    {
        private SqliteConnection _connection;

        public DocumentDBManager(SqliteConnection connection) : base(connection)
        {
            _connection = connection;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "CREATE VIRTUAL TABLE IF NOT EXISTS documents USING fts5(title, content);";
                command.ExecuteNonQuery();
            }
        }

        public async Task<long> SaveDocumentAsync(string title, string content)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                long rowId;
                await using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT OR REPLACE INTO documents (title, content) VALUES (@title, @content);";
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@content", content);

                    command.ExecuteNonQuery();

                    command.CommandText = "SELECT last_insert_rowid();";

                    var result = command.ExecuteScalar();
                    rowId = result != null ? (long)result : -1;
                    transaction.Commit();
                }

                return rowId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> DoesDocumentExistAsync(string title)
        {
            var doesExist = false;
            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT title FROM documents WHERE title = @title;";
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

        public async Task<string> GetDocumentContentAsync(string title)
        {
            var result = string.Empty;
            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT content FROM documents WHERE title = @title;";
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

        public async Task<List<(long rowID, string title)>> GetAllDocumentTitlesAsync()
        {
            var documents = new List<(long RowId, string Title)>();
            await using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT rowid, title FROM documents;";
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
    }
}