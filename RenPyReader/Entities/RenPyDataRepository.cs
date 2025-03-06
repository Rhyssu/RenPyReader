using RenPyReader.Database;
using RenPyReader.Utilities;

namespace RenPyReader.Entities
{
    internal class RenPyDataRepository
    {
        // Ordered sets to store different types of data
        public OrderedSet<string> Events { get; set; } = new();
        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> EventsMap { get; set; } = new();

        public OrderedSet<string> Scenes { get; set; } = new();
        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> ScenesMap { get; set; } = new();

        public OrderedSet<string> Sounds { get; set; } = new();
        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> SoundsMap { get; set; } = new();

        public OrderedSet<string> Musics { get; set; } = new();
        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> MusicsMap { get; set; } = new();

        public OrderedSet<string> DocumentNames { get; set; } = new();

        // Reference to the database manager
        private readonly RenPyDBManager _renPyDBManager;

        // Constructor to initialize the repository with the database manager
        public RenPyDataRepository(RenPyDBManager renPyDBManager)
        {
            _renPyDBManager = renPyDBManager;
            InitializeDataAsync();
        }

        // Asynchronous method to initialize data sets and maps
        private async void InitializeDataAsync()
        {
            await InitializeSetAndMapAsync(nameof(Events).ToLowerInvariant(), Events, EventsMap);
            await InitializeSetAndMapAsync(nameof(Scenes).ToLowerInvariant(), Scenes, ScenesMap);
            await InitializeSetAndMapAsync(nameof(Sounds).ToLowerInvariant(), Sounds, SoundsMap);
            await InitializeSetAndMapAsync(nameof(Musics).ToLowerInvariant(), Musics, MusicsMap);
        }

        // Generic method to initialize a set and its corresponding map from the database
        private async Task InitializeSetAndMapAsync(string tableName, OrderedSet<string> set, OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> map)
        {
            set = await _renPyDBManager.GetOrderedSet(tableName);
            map = await _renPyDBManager.GetOrderedMap(tableName);
        }

        // Method to batch save all data sets and maps to the database
        public void BatchSaveAll()
        {
            BatchSave(nameof(Events).ToLowerInvariant(), Events, EventsMap);
            BatchSave(nameof(Scenes).ToLowerInvariant(), Scenes, ScenesMap);
            BatchSave(nameof(Sounds).ToLowerInvariant(), Sounds, SoundsMap);
            BatchSave(nameof(Musics).ToLowerInvariant(), Musics, MusicsMap);
        }

        // Generic method to batch save a set and its corresponding map to the database
        private void BatchSave(string tableName, OrderedSet<string> set, OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> map)
        {
            _renPyDBManager.BatchInsertOrIgnore(tableName, set);
            _renPyDBManager.BatchInsertOrIgnoreMap(tableName, map);
        }
    }
}