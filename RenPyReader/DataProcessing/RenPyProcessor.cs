using RenPyReader.Database;
using RenPyReader.Entities;
using RenPyReader.Utilities;

namespace RenPyReader.DataProcessing
{
    internal partial class RenPyProcessor
    {
        public RenPyDataRepository RenPyDataRepository { get; set; }

        private delegate bool StringCondition(string input);

        private delegate void StringAction(Int64 rowID, int index, string input);

        private readonly RenPyDBManager _renPyDBManager;

        private readonly Dictionary<StringCondition, StringAction> _actions = new();

        public RenPyProcessor(RenPyDBManager renPyDBManager)
        {
            _actions = InitializeActions();
            _renPyDBManager = renPyDBManager;
            RenPyDataRepository = new(_renPyDBManager);
        }

        private Dictionary<StringCondition, StringAction> InitializeActions() => new() { { IsLabel, AddLabel }, { IsScene, AddScene }, { IsPlaySound, AddSound }, { IsPlayMusic, AddMusic } };

        internal async Task ProcessFileContentAsync(Int64 rowID, string content)
        {
            using StringReader reader = new(content);
            {
                string? line;
                int index = 0;

                while ((line = await reader.ReadLineAsync()) != null)
                {
                    index += 1;
                    line = line.TrimStart();
                    CheckKeywords(rowID, index, line);
                }
            }
        }

        private void CheckKeywords(Int64 rowID, int index, string input)
        {
            foreach (var action in _actions)
            {
                if (action.Key(input))
                {
                    action.Value(rowID, index, input);
                    break;
                }
            }
        }

        private bool IsLabel(string input) => input.StartsWith("label", StringComparison.OrdinalIgnoreCase);

        private void AddLabel(Int64 rowID, int index, string input)
        {
            var elementID = RenPyDataRepository.Events.Add(RegexProcessor.ExtractLabel(input));
            RenPyDataRepository.EventsMap.Add((rowID, elementID, index));
        }

        private bool IsScene(string input) => input.StartsWith("scene", StringComparison.OrdinalIgnoreCase);

        private void AddScene(Int64 rowID, int index, string input)
        {
            var elementID = RenPyDataRepository.Scenes.Add(RegexProcessor.ExtractScene(input));
            RenPyDataRepository.ScenesMap.Add((rowID, elementID, index));
        }

        private bool IsPlaySound(string input) => input.StartsWith("play sound", StringComparison.OrdinalIgnoreCase);

        private void AddSound(Int64 rowID, int index, string input)
        {
            var elementID = RenPyDataRepository.Sounds.Add(RegexProcessor.ExtractSound(input));
            RenPyDataRepository.SoundsMap.Add((rowID, elementID, index));
        }

        private bool IsPlayMusic(string input) => input.StartsWith("play music", StringComparison.OrdinalIgnoreCase);

        private void AddMusic(Int64 rowID, int index, string input)
        {
            var elementID = RenPyDataRepository.Musics.Add(RegexProcessor.ExtractMusic(input));
            RenPyDataRepository.MusicsMap.Add((rowID, elementID, index));
        }
    }
}