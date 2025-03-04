using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace RenPyReader.Database
{
    /// <summary>
    /// Manages the SQLite database connection
    /// Inherits from RenPyDBBinaryManager.
    /// </summary>
    internal class RenPyDBManager : DocumentDBManager
    {
        /// <summary>
        /// Initializes a new instance of the RenPyDBManager class.
        /// Opens a connection to the specified database and initializes SQLite batteries.
        /// </summary>
        /// <param name="databaseName">The name of the database file.</param>
        public RenPyDBManager(string databaseName) : base(CreateConnection(databaseName))
        {
            // Initialize SQLite batteries
            Batteries.Init();
        }

        /// <summary>
        /// Creates and opens a connection to the specified SQLite database.
        /// If the database file does not exist, it creates a new file.
        /// </summary>
        /// <param name="databaseName">The name of the database file.</param>
        /// <returns>An open SqliteConnection to the specified database.</returns>
        private static SqliteConnection CreateConnection(string databaseName)
        {
            // Construct the path to the database file in the application data directory
            var databasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{databaseName}.db");

            // Create the database file if it does not exist
            if (!File.Exists(databasePath))
            {
                File.Create(databasePath).Dispose();
            }

            // Create and open the SQLite connection
            var connectionString = $"Data Source={databasePath}";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            return connection;
        }
    }
}