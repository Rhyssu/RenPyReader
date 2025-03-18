using RenPyReader.DataModels;
using RenPyReader.Services;
using RenPyReader.Utilities;

namespace RenPyReader.Components.Pages
{
    public partial class EventReader
    {
        EventContext EventContext { get; set; } = null!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                EventContext = new EventContext(SQLiteService);

                var renPyEvent = await SQLiteService.GetRenPyEventAsync("iodorm35");
                await EventContext.SetAndInitialize(renPyEvent!);
                StateHasChanged();
            }
        }
    }

    internal class EventContext
    {
        private RenPyEvent? _event;

        private int _eventStopIndex = -1;
        
        private int _eventStartIndex = -1;

        private List<string> _content = new();

        private List<RenPyScene> _scenes = new();

        private RenPyScene? _currentScene;

        private List<RenPyMusic> _musics = new();

        private RenPyMusic? _currentMusic;

        private List<RenPySound> _sounds = new();

        private RenPySound? _currentSound;

        private List<(int index, string content)> _dialogue = new();

        public string? currentDialogue;

        public readonly ISQLiteService sqliteService;

        public string? imageSource;

        public EventContext(ISQLiteService sqliteService)
        {
            this.sqliteService = sqliteService;
        }

        public async Task SetAndInitialize(RenPyEvent renPyEvent)
        {
            _event = renPyEvent;
            await GetEventDetails();
            await SetAdditionalContext();
            await PrepareStartingView();
        }

        public string GetTitle()
        {
            if (_event == null)
            {
                return string.Empty;
            }

            return _event.Name;
        }

        public async Task SetCurrentSceneImageSource()
        {
            if (_currentScene == null)
            {
                return;
            }

            var image = await sqliteService.GetImageAsync(_currentScene.Name);
            if (image == null || image.Content == null)
            {
                return;
            }

            imageSource = GetImageSource(image.Content);
        }

        private async Task GetEventDetails()
        {
            if (_event == null)
            {
                // No event selected, aborting
                return;
            }

            string content = await sqliteService.GetDocumentContentAsync(_event.Parent);
            if (string.IsNullOrEmpty(content))
            {
                // No parent content, aborting
                return;
            }

            _content.Clear();
            _dialogue.Clear();
            using StringReader reader = new(content);
            {
                string? line;
                int index = 1;
                bool isOnEvent = false;
                bool isJumpReached = false;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (index == _event.Index)
                    {
                        // Start of the event reading
                        _eventStartIndex = index;
                        isOnEvent = true;
                    }

                    if (isOnEvent && RegexProcessor.IsDialogue(line))
                    {
                        _dialogue.Add((index, line.Trim()));
                    }

                    if (isOnEvent && line.TrimStart().StartsWith("jump"))
                    {
                        // End of the event reading
                        _eventStopIndex = index;
                        isJumpReached = true;
                    }

                    if (isOnEvent && isJumpReached && string.IsNullOrEmpty(line))
                    {
                        // When the jump is reached and then empty line appears
                        // it means that the event has finished
                        break;
                    }

                    index += 1;
                }
            }

            string[] contentLines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            _content = [.. contentLines.Skip(_eventStartIndex - 1).Take(_eventStopIndex - _eventStartIndex + 1)];
        }

        private async Task SetAdditionalContext()
        {
            _scenes.Clear();
            var scenes = await sqliteService.GetRenPyBaseTableAsync("scenes", _event!.Parent, _eventStartIndex, _eventStopIndex);
            _scenes = [.. scenes.Cast<RenPyScene>()];

            _sounds.Clear();
            var sounds = await sqliteService.GetRenPyBaseTableAsync("sounds", _event!.Parent, _eventStartIndex, _eventStopIndex);
            _sounds = [.. sounds.Cast<RenPySound>()];

            _musics.Clear();
            var musics = await sqliteService.GetRenPyBaseTableAsync("musics", _event!.Parent, _eventStartIndex, _eventStopIndex);
            _musics = [.. musics.Cast<RenPyMusic>()];
        }

        private async Task PrepareStartingView()
        {
            _currentScene = _scenes.FirstOrDefault();
            await SetCurrentSceneImageSource();

            var firstMusic = _musics.FirstOrDefault();
            var (index, content) = _dialogue.FirstOrDefault();
            currentDialogue = content;

            if (firstMusic?.Index < index)
            {
                _currentMusic = firstMusic;
            }
        }

        private string GetImageSource(byte[] content)
        {
            var base64str = Convert.ToBase64String(content);
            return string.Format("data:image/webp;base64,{0}", base64str);
        }
    }
}