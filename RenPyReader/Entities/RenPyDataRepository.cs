using RenPyReader.Database;
using RenPyReader.Utilities;

namespace RenPyReader.Entities
{
    internal class RenPyDataRepository
    {
        #region Events
        
        public OrderedSet<string> Events { get; set; } = new();

        public OrderedSet<MapEntry> EventsMap { get; set; } = new();

        #endregion Events

        #region Scenes

        public OrderedSet<string> Scenes { get; set; } = new();
        
        public OrderedSet<MapEntry> ScenesMap { get; set; } = new();

        #endregion Scenes

        #region Sounds

        public OrderedSet<string> Sounds { get; set; } = new();
        
        public OrderedSet<MapEntry> SoundsMap { get; set; } = new();

        #endregion Sounds

        #region Musics

        public OrderedSet<string> Musics { get; set; } = new();
        
        public OrderedSet<MapEntry> MusicsMap { get; set; } = new();

        #endregion Musics

        #region Documents

        public OrderedSet<string> DocumentNames { get; set; } = new();

        #endregion Documents

        private readonly RenPyDBManager _renPyDBManager;

        public RenPyDataRepository(RenPyDBManager renPyDBManager)
        {
            _renPyDBManager = renPyDBManager;
            InitializeDataAsync();
        }

        private async void InitializeDataAsync()
        {
            await InitializeSetAndMapAsync(nameof(Events).ToLowerInvariant(), Events, EventsMap);
            await InitializeSetAndMapAsync(nameof(Scenes).ToLowerInvariant(), Scenes, ScenesMap);
            await InitializeSetAndMapAsync(nameof(Sounds).ToLowerInvariant(), Sounds, SoundsMap);
            await InitializeSetAndMapAsync(nameof(Musics).ToLowerInvariant(), Musics, MusicsMap);
        }

        private async Task InitializeSetAndMapAsync(string tableName, OrderedSet<string> set, OrderedSet<MapEntry> map)
        {
            ArgumentNullException.ThrowIfNull(set);
            ArgumentNullException.ThrowIfNull(map);

            #pragma warning disable IDE0059 // Unnecessary assignment of a value
            set = await _renPyDBManager.GetOrderedSet(tableName);
            map = await _renPyDBManager.GetOrderedMap(tableName);
            #pragma warning restore IDE0059 // Unnecessary assignment of a value
        }

        public void BatchSaveAll()
        {
            BatchSave(nameof(Events).ToLowerInvariant(), Events, EventsMap);
            BatchSave(nameof(Scenes).ToLowerInvariant(), Scenes, ScenesMap);
            BatchSave(nameof(Sounds).ToLowerInvariant(), Sounds, SoundsMap);
            BatchSave(nameof(Musics).ToLowerInvariant(), Musics, MusicsMap);
        }

        private void BatchSave(string tableName, OrderedSet<string> set, OrderedSet<MapEntry> map)
        {
            _renPyDBManager.BatchInsertOrIgnore(tableName, set);
            _renPyDBManager.BatchInsertOrIgnoreMap(tableName, map);
        }
    }

    internal record struct MapEntry(long ParentRowID, int ElementRowID, int LineIndex)
    {
        public static implicit operator (long ParentRowID, int ElementRowID, int LineIndex)(MapEntry value)
        {
            return (value.ParentRowID, value.ElementRowID, value.LineIndex);
        }

        public static implicit operator MapEntry((long ParentRowID, int ElementRowID, int LineIndex) value)
        {
            return new MapEntry(value.ParentRowID, value.ElementRowID, value.LineIndex);
        }
    }
}