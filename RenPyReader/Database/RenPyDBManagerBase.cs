using Microsoft.Data.Sqlite;
using RenPyReader.DataModels;
using RenPyReader.Entities;
using RenPyReader.Utilities;

namespace RenPyReader.Database
{
    internal abstract class RenPyDBManagerBase
    {
        private readonly SqliteConnection _connection;
        
        private readonly RenPyDataRepository? _renPyDataRepository;

        // Dictionary mapping from the type to an action that adds the item to its collection.
        private readonly Dictionary<Type, Action<object>>? _addActions;

        #pragma warning disable IDE0028 // Simplify collection initialization
        // Result list storing info about each record inserted or replaced.
        public List<(object item, string operation)> SaveResults { get; } = new();
        #pragma warning restore IDE0028 // Simplify collection initialization

        // Dictionary mapping from the type to a save action that calls the generic SaveBatchAsync.
        private readonly Dictionary<Type, Func<LogBuffer, Task>> _saveActions;

        public RenPyDBManagerBase(SqliteConnection connection)
        {
            _connection = connection;
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

                command.CommandText = DBCommand.CreateRenPyCharacterTable.ToSQLite("");
                command.ExecuteNonQuery();
            }

            _renPyDataRepository = new();
            _addActions = new Dictionary<Type, Action<object>>
            {
                { typeof(RenPyEvent), item      => _renPyDataRepository.Events.Add((RenPyEvent)item) },
                { typeof(RenPyScene), item      => _renPyDataRepository.Scenes.Add((RenPyScene)item) },
                { typeof(RenPySound), item      => _renPyDataRepository.Sounds.Add((RenPySound)item) },
                { typeof(RenPyMusic), item      => _renPyDataRepository.Musics.Add((RenPyMusic)item) },
                { typeof(RenPyCharacter), item  => _renPyDataRepository.Characters.Add((RenPyCharacter)item) }
            };

            // Build the save actions using the generic batch saver.
            _saveActions = new Dictionary<Type, Func<LogBuffer, Task>>
            {
                {
                    typeof(RenPyEvent), CreateSaveAction(
                        _renPyDataRepository!.Events,
                        "events",
                        new Dictionary<string, Func<RenPyEvent, object>>
                    {
                        { "Name",       s => s.Name },
                        { "Parent",     s => s.Parent },
                        { "Line",       s => s.Index }
                    })
                },
                {
                    typeof(RenPyScene), CreateSaveAction(
                        _renPyDataRepository!.Scenes,
                        "scenes",
                        new Dictionary<string, Func<RenPyScene, object>>
                    {
                        { "Name",       s => s.Name },
                        { "Parent",     s => s.Parent },
                        { "Line",       s => s.Index }
                    })
                },
                {
                    typeof(RenPySound), CreateSaveAction(
                        _renPyDataRepository!.Sounds,
                        "sounds",
                        new Dictionary<string, Func<RenPySound, object>>
                    {
                        { "Name",       s => s.Name },
                        { "Parent",     s => s.Parent },
                        { "Line",       s => s.Index }
                    })
                },
                {
                    typeof(RenPyMusic), CreateSaveAction(
                        _renPyDataRepository!.Musics,
                        "musics",
                        new Dictionary<string, Func<RenPyMusic, object>>
                    {
                        { "Name",       s => s.Name },
                        { "Parent",     s => s.Parent },
                        { "Line",       s => s.Index }
                    })
                },
                {
                    typeof(RenPyCharacter), CreateSaveAction(
                        _renPyDataRepository!.Characters,
                        "characters",
                        new Dictionary<string, Func<RenPyCharacter, object>>
                    {
                        { "Code",       s => s.Code },
                        { "Name",       s => s.Name },
                        { "Color",      s => s.ColorHTML }
                    })
                }
            };
        }

        public void ClearRepository()
        {
            _renPyDataRepository!.Clear();
        }

        /// <summary>
        /// Adds an item to its corresponding collection.
        /// </summary>
        public void AddItem(object item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (_addActions!.TryGetValue(item.GetType(), out var addAction))
            {
                addAction(item);
            }
            else
            {
                throw new ArgumentException("Unsupported type", nameof(item));
            }
        }

        public SqliteTransaction BeginAndGetTransaction()
        {
            using (var transaction =  _connection.BeginTransaction())
            {
                return transaction;
            }
        }

        /// <summary>
        /// Saves all collections to the SQLite database using batch operations.
        /// </summary>
        public async Task BatchSaveAsync(LogBuffer logBuffer)
        {
            foreach (var saveAction in _saveActions.Values)
            {
                await saveAction(logBuffer);
            }
        }

        /// <summary>
        /// Helper to create a save action for a specific type T.
        /// </summary>
        private Func<LogBuffer, Task> CreateSaveAction<T>(List<T> list, string tableName, Dictionary<string, Func<T, object>> columnMappings)
        {
            return async logBuffer =>
            {
                if (list.Count > 0)
                {
                    var results = await SaveBatchAsync(list, tableName, columnMappings);
                    foreach (var (item, operation) in results)
                    {
                        if (item != null)
                        {
                            SaveResults.Add((item, operation));
                        }
                    }
                    logBuffer.Add($"Saved {list.Count} record(s) in table '{tableName}'.");
                }
            };
        }

        /// <summary>
        /// A generic helper method that performs a batch save of a list of items to the database,
        /// using INSERT OR REPLACE and returning information about each operation.
        /// </summary>
        /// <typeparam name="T">The type of items being saved.</typeparam>
        /// <param name="transaction">The SQLite transaction.</param>
        /// <param name="items">The list of items to save.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnMappings">A mapping of column names to value extractor functions.</param>
        /// <returns>A list of tuples containing the item and a string indicating whether it was inserted or replaced.</returns>
        private async Task<List<(T item, string operation)>> SaveBatchAsync<T>(List<T> items, string tableName, Dictionary<string, Func<T, object>> columnMappings)
        {
            var results = new List<(T, string)>();
            using (var command = _connection.CreateCommand())
            {
                string columns = string.Join(", ", columnMappings.Keys);
                string paramNames = string.Join(", ", columnMappings.Keys.Select(c => "@" + c));
                command.CommandText = $"INSERT OR REPLACE INTO {tableName} ({columns}) VALUES ({paramNames});";

                foreach (var item in items)
                {
                    command.Parameters.Clear();
                    foreach (var kvp in columnMappings)
                    {
                        command.Parameters.AddWithValue("@" + kvp.Key, kvp.Value(item));
                    }

                    int affectedRows = await command.ExecuteNonQueryAsync();
                    string operation = affectedRows == 1 ? "inserted" : affectedRows == 2 ? "replaced" : "unknown";
                    results.Add((item, operation));
                }
            }
            return results;
        }
    }
}