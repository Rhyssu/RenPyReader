using Microsoft.Data.Sqlite;
using RenPyReader.DataModels;
using RenPyReader.Utilities;

namespace RenPyReader.Database
{
    /// <summary>
    /// Abstract class that manages binary data entries in the SQLite database.
    /// Inherits from RenPyDBManagerBase.
    /// </summary>
    internal abstract class RenPyDBBinaryManager : RenPyDBManagerBase
    {
        private readonly SqliteConnection _connection;

        /// <summary>
        /// Initializes a new instance of the RenPyDBBinaryManager class.
        /// </summary>
        /// <param name="connection">The SQLite connection to be used by the manager.</param>
        protected RenPyDBBinaryManager(SqliteConnection connection) : base(connection)
        {
            _connection = connection;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = DBCommand.CreateRenPyBinaryBaseTable.ToSQLite("images");
                command.ExecuteNonQuery();

                command.CommandText = DBCommand.CreateRenPyBinaryBaseTable.ToSQLite("audios");
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Asynchronously retrieves the names of binary entries from the database.
        /// </summary>
        /// <param name="baseName">The base name to filter the binary entries.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a set of binary entry names.</returns>
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

        /// <summary>
        /// Asynchronously inserts an image entry into the database.
        /// </summary>
        /// <param name="renPyImage">The RenPyImage object containing the image data to be inserted.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        /// <summary>
        /// Asynchronously inserts an audio entry into the database.
        /// </summary>
        /// <param name="renPyAudio">The RenPyAudio object containing the audio data to be inserted.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
    }
}