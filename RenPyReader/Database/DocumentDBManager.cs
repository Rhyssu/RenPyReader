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

        public async Task SaveDocumentAsync(string title, string content)
        {
            using var transaction = _connection.BeginTransaction();
            try
            {
                await using (var command = _connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "INSERT OR REPLACE INTO documents (title, content) VALUES (@title, @content);";
                    command.Parameters.AddWithValue("@title", title);
                    command.Parameters.AddWithValue("@content", content);

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
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
    }
}