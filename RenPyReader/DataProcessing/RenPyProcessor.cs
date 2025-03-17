using RenPyReader.DataModels;
using RenPyReader.Entities;
using RenPyReader.Services;
using RenPyReader.Utilities;

namespace RenPyReader.DataProcessing
{
    internal partial class RenPyProcessor
    {
        public RenPyDataRepository RenPyDataRepository { get; set; }

        public ISQLiteService SqliteService { get; }

        private delegate bool StringCondition(string conditionStr);

        private delegate void StringAction(string entryName, string content, int index);

        private readonly Dictionary<StringCondition, StringAction> _actions = new();

        public RenPyProcessor(ISQLiteService sqliteService)
        {
            _actions = InitializeActions();
            RenPyDataRepository = new();
            SqliteService = sqliteService;
        }

        private Dictionary<StringCondition, StringAction> InitializeActions() => new() { { IsLabel, AddLabel }, { IsScene, AddScene }, { IsPlaySound, AddSound }, { IsPlayMusic, AddMusic }, { IsDefineCharacter, AddCharacter } };

        internal async Task ProcessFileContentAsync(string entryName, string entryContent)
        {
            using StringReader reader = new(entryContent);
            {
                string? line;
                int index = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    index += 1;
                    line = line.TrimStart();
                    CheckKeywords(entryName, line, index);
                }
            }
        }

        internal async Task BatchSaveAll()
        {
            await SqliteService.BatchInsertOrReplaceBaseTableAsync("events", [.. RenPyDataRepository.Events.Cast<RenPyBase>()]);
            RenPyDataRepository.Events.Clear();

            await SqliteService.BatchInsertOrReplaceBaseTableAsync("scenes", [.. RenPyDataRepository.Scenes.Cast<RenPyBase>()]);
            RenPyDataRepository.Scenes.Clear();

            await SqliteService.BatchInsertOrReplaceBaseTableAsync("sounds", [.. RenPyDataRepository.Sounds.Cast<RenPyBase>()]);
            RenPyDataRepository.Sounds.Clear();

            await SqliteService.BatchInsertOrReplaceBaseTableAsync("musics", [.. RenPyDataRepository.Musics.Cast<RenPyBase>()]);
            RenPyDataRepository.Musics.Clear();

            await SqliteService.BatchInsertOrReplaceCharactersAsync(RenPyDataRepository.Characters);
            RenPyDataRepository.Characters.Clear();
        }

        private void CheckKeywords(string entryName, string content, int index)
        {
            foreach (var action in _actions)
            {
                if (action.Key(content))
                {
                    action.Value(entryName, content, index);
                    break;
                }
            }
        }

        private bool IsLabel(string content) => content.StartsWith("label", StringComparison.OrdinalIgnoreCase);

        private void AddLabel(string entryName, string content, int index)
        {
            var eventName = RegexProcessor.ExtractLabel(content);
            if (!string.IsNullOrEmpty(eventName))
            {
                var newRenPyEvent = new RenPyEvent(eventName, entryName, (uint)index);
                RenPyDataRepository.Events.Add(newRenPyEvent);
            }
        }

        private bool IsScene(string content) => content.StartsWith("scene", StringComparison.OrdinalIgnoreCase);

        private void AddScene(string entryName, string content, int index)
        {
            var sceneName = RegexProcessor.ExtractScene(content);
            if (!string.IsNullOrEmpty(sceneName))
            {
                var newRenPyScene = new RenPyScene(sceneName, entryName, (uint)index);
                RenPyDataRepository.Scenes.Add(newRenPyScene);
            }
        }

        private bool IsPlaySound(string content) => content.StartsWith("play sound", StringComparison.OrdinalIgnoreCase);

        private void AddSound(string entryName, string content, int index)
        {
            var soundName = RegexProcessor.ExtractSound(content);
            if (!string.IsNullOrEmpty(soundName))
            {
                var newRenPySound = new RenPySound(soundName, entryName, (uint)index);
                RenPyDataRepository.Sounds.Add(newRenPySound);
            }
        }

        private bool IsPlayMusic(string content) => content.StartsWith("play music", StringComparison.OrdinalIgnoreCase);

        private void AddMusic(string entryName, string content, int index)
        {
            var musicName = RegexProcessor.ExtractMusic(content);
            if (!string.IsNullOrEmpty(musicName))
            {
                var newRenPyMusic = new RenPyMusic(musicName, entryName, (uint)index);
                RenPyDataRepository.Musics.Add(newRenPyMusic);
            }
        }

        private bool IsDefineCharacter(string content) => content.StartsWith("define", StringComparison.OrdinalIgnoreCase);

        private void AddCharacter(string entryName, string content, int index)
        {
            var character = RegexProcessor.ExtractCharacter(content);
            if (!string.IsNullOrEmpty(character.Name))
            {
                RenPyDataRepository.Characters.Add(character);
            }
        }
    }
}