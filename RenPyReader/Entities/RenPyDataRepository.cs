using RenPyReader.Database;
using RenPyReader.Utilities;
using System.Reflection;

namespace RenPyReader.Entities
{
    internal class RenPyDataRepository
    {
        public OrderedSet<string> Events { get; set; } = new();

        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> EventsMap { get; set; } = new();

        public OrderedSet<string> Scenes { get; set; } = new();

        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> ScenesMap { get; set; } = new();

        public OrderedSet<string> Sounds { get; set; } = new();

        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> SoundsMap { get; set; } = new();

        public OrderedSet<string> Musics { get; set; } = new();

        public OrderedSet<(Int64 ParentRowID, int ElementRowID, int LineIndex)> MusicsMap { get; set; } = new();

        public OrderedSet<string> DocumentNames { get; set; } = new();

        private RenPyDBManager _renPyDBManager;

        public RenPyDataRepository(RenPyDBManager renPyDBManager)
        {
            _renPyDBManager = renPyDBManager;
            InitializeEventsAsync();
        }

        private async void InitializeEventsAsync()
        {
            Events = await _renPyDBManager.GetTableOrderedSet(nameof(Events).ToLowerInvariant());
            EventsMap = await _renPyDBManager.GetTableOrderedMap(nameof(Events).ToLowerInvariant());
            Scenes = await _renPyDBManager.GetTableOrderedSet(nameof(Scenes).ToLowerInvariant());
            ScenesMap = await _renPyDBManager.GetTableOrderedMap(nameof(Scenes).ToLowerInvariant());
            Sounds = await _renPyDBManager.GetTableOrderedSet(nameof(Sounds).ToLowerInvariant());
            SoundsMap = await _renPyDBManager.GetTableOrderedMap(nameof(Sounds).ToLowerInvariant());
            Musics = await _renPyDBManager.GetTableOrderedSet(nameof(Musics).ToLowerInvariant());
            MusicsMap = await _renPyDBManager.GetTableOrderedMap(nameof(Musics).ToLowerInvariant());
        }

        public void BatchSaveAll()
        {
            _renPyDBManager.BatchInsertOrIgnore(nameof(Events).ToLowerInvariant(), Events);
            _renPyDBManager.BatchInsertOrIgnoreMap(nameof(Events).ToLowerInvariant(), EventsMap);
            _renPyDBManager.BatchInsertOrIgnore(nameof(Scenes).ToLowerInvariant(), Scenes);
            _renPyDBManager.BatchInsertOrIgnoreMap(nameof(Scenes).ToLowerInvariant(), ScenesMap);
            _renPyDBManager.BatchInsertOrIgnore(nameof(Sounds).ToLowerInvariant(), Sounds);
            _renPyDBManager.BatchInsertOrIgnoreMap(nameof(Sounds).ToLowerInvariant(), SoundsMap);
            _renPyDBManager.BatchInsertOrIgnore(nameof(Musics).ToLowerInvariant(), Musics);
            _renPyDBManager.BatchInsertOrIgnoreMap(nameof(Musics).ToLowerInvariant(), MusicsMap);
        }
    }
}