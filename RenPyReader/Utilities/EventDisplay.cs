using RenPyReader.DataModels;
using RenPyReader.Services;
using System;

namespace RenPyReader.Utilities
{
    internal class EventDisplay(ISQLiteService SQLiteService, IAudioPlayerService AudioPlayerService)
    {
        public RenPyEvent? CurrentEvent { get; set; }

        public (int Index, string Content) CurrentDialogue { get; set; }

        public Action StateHasChangedCallback { get; set; } = delegate { };

        public RenPyScene? CurrentScene { get; set; }

        public string? ImageSource { get; set; }

        public RenPyMusic? CurrentMusic { get; set; }

        public RenPySound? CurrentSound { get; set; }

        public RenPyCharacter? CurrentCharacter { get; set; }

        public List<EventDisplayQueueItem> Queue { get; set; } = [];

        public bool IsBeginning { get; set; } = true;

        public bool IsEnd { get; set; } = false;

        public bool IsError { get; set; } = false;

        public string ErrorMessage { get; set; } = string.Empty;

        private List<(int index, string content)> _content = new();

        private List<(int index, string content)> _dialogue = new();

        private List<RenPyScene> _scenes = new();

        private List<RenPyMusic> _musics = new();

        private List<RenPySound> _sounds = new();
        
        private int _eventStopIndex = -1;

        private int _eventStartIndex = -1;

        public async Task SetEventAndInitialize(RenPyEvent renPyEvent)
        {
            CurrentEvent = renPyEvent;

            await GetEventDetails();
            await SetAdditionalContext();
            await PrepareStartingView();
            await ProcessQueue(false);
        }

        private async Task GetEventDetails()
        {
            if (CurrentEvent == null)
            {
                return;
            }

            try
            {
                string content = await SQLiteService.GetDocumentContentAsync(CurrentEvent.Parent);
                if (string.IsNullOrEmpty(content))
                {
                    return;
                }

                ResetState();
                using StringReader reader = new(content);
                {
                    string? line;
                    int index = 1;
                    bool isOnEvent = false;
                    bool isJumpReached = false;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (index == CurrentEvent.Index)
                        {
                            _eventStartIndex = index;
                            isOnEvent = true;
                        }

                        if (isOnEvent && RegexProcessor.IsDialogue(line))
                        {
                            _dialogue.Add((index, line.Trim()));
                        }

                        if (isOnEvent && line.TrimStart().StartsWith("jump"))
                        {
                            _eventStopIndex = index;
                            isJumpReached = true;
                        }

                        if (isOnEvent && isJumpReached && string.IsNullOrEmpty(line))
                        {
                            break;
                        }

                        index += 1;
                    }
                }

                int contentIndex = _eventStartIndex;
                string[] contentLines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                var eventContent = contentLines.Skip(_eventStartIndex - 1).Take(_eventStopIndex - _eventStartIndex + 1).ToList();
                foreach (var eventLine in eventContent)
                {
                    _content.Add((contentIndex, eventLine));
                    contentIndex += 1;
                }
            }
            catch (Exception ex)
            {
                IsError = true;
                ErrorMessage = ex.Message;
            }
        }

        private async Task SetAdditionalContext()
        {
            var scenes = await SQLiteService.GetRenPyBaseTableAsync("scenes", CurrentEvent!.Parent, _eventStartIndex, _eventStopIndex);
            _scenes = [.. scenes.Cast<RenPyScene>()];

            var sounds = await SQLiteService.GetRenPyBaseTableAsync("sounds", CurrentEvent!.Parent, _eventStartIndex, _eventStopIndex);
            _sounds = [.. sounds.Cast<RenPySound>()];

            var musics = await SQLiteService.GetRenPyBaseTableAsync("musics", CurrentEvent!.Parent, _eventStartIndex, _eventStopIndex);
            _musics = [.. musics.Cast<RenPyMusic>()];
        }

        private async Task PrepareStartingView()
        {
            CurrentDialogue = _dialogue.FirstOrDefault();

            await SetCurrentCharacter(CurrentDialogue.Content);
            await AddContextToQueue(0, CurrentDialogue.Index);
        }

        public async Task GoToNextDialogue()
        {
            await AudioPlayerService.StopSoundsAsync();
            var dialogue = _dialogue.FirstOrDefault(d => d.index == CurrentDialogue.Index);

            var index = _dialogue.IndexOf(dialogue);
            var newDialogue = _dialogue.ElementAtOrDefault(index + 1);
            if (index == (_dialogue.Count - 2))
            {
                IsEnd = true;
            }
            else
            {
                IsEnd = false;
            }

            int currentLineIndex = dialogue.index;
            int nextLineIndex = newDialogue.index;
            await AddContextToQueue(currentLineIndex, nextLineIndex);

            CurrentDialogue = newDialogue;
            await SetCurrentCharacter(CurrentDialogue.Content);
            await ProcessQueue();
        }

        public async Task GoToPreviousDialogue()
        {
            await AudioPlayerService.StopSoundsAsync();
            var dialogue = _dialogue.FirstOrDefault(d => d.index == CurrentDialogue.Index);

            var index = _dialogue.IndexOf(dialogue);
            var newDialogue = _dialogue.ElementAtOrDefault(index - 1);
            if (index == 1)
            {
                IsBeginning = true;
            }
            else
            {
                IsBeginning = false;
            }

            int currentLineIndex = dialogue.index;
            int nextLineIndex = newDialogue.index;
            await AddContextToQueue(nextLineIndex, currentLineIndex);

            CurrentDialogue = newDialogue;
            await SetCurrentCharacter(CurrentDialogue.Content);
            await ProcessQueue();
        }

        private void ResetState()
        {
            CurrentScene = null;
            CurrentMusic = null;
            CurrentSound = null;

            _content.Clear();
            _scenes.Clear();
            _sounds.Clear();
            _musics.Clear();
            _dialogue.Clear();

            IsError = false;
            ErrorMessage = string.Empty;
        }

        private async Task AddContextToQueue(int lowerLimit = 0, int upperLimit = 0)
        {
            await AddScenesToQueue(lowerLimit, upperLimit);
            await AddMusicsToQueue(lowerLimit, upperLimit);
            await AddSoundsToQueue(lowerLimit, upperLimit);
        }

        private async Task AddScenesToQueue(int lowerLimit, int upperLimit)
        {
            var scenes = _scenes.Where(s => s.Index > lowerLimit && s.Index < upperLimit).ToList();
            foreach (var scene in scenes)
            {
                var renPyImage = await SQLiteService.GetImageAsync(scene.Name);
                if (renPyImage != null && renPyImage.Content != null)
                {
                    var imageSource = GetImageSource(renPyImage.Content);
                    var queueItem = new EventDisplayQueueItem
                    {
                        QueueItemType = QueueItemType.Image,
                        RenPyItem = scene,
                        Value = imageSource,
                    };

                    Queue.Add(queueItem);
                }
            }
        }

        private async Task AddMusicsToQueue(int lowerLimit, int upperLimit)
        {
            var musics = _musics.Where(s => s.Index > lowerLimit && s.Index < upperLimit).ToList();
            foreach (var music in musics)
            {
                var renPyAudio = await SQLiteService.GetAudioAsync(music.Name);
                if (renPyAudio != null && renPyAudio.Content != null)
                {
                    var queueItem = new EventDisplayQueueItem
                    {
                        QueueItemType = QueueItemType.Music,
                        RenPyItem = music,
                        Content = renPyAudio.Content,
                    };

                    Queue.Add(queueItem);
                }
            }
        }

        private async Task AddSoundsToQueue(int lowerLimit, int upperLimit)
        {
            var sounds = _sounds.Where(s => s.Index > lowerLimit && s.Index < upperLimit).ToList();
            foreach (var sound in sounds)
            {
                var renPySound = await SQLiteService.GetAudioAsync(sound.Name);
                if (renPySound != null && renPySound.Content != null)
                {
                    var queueItem = new EventDisplayQueueItem
                    {
                        QueueItemType = QueueItemType.Sound,
                        RenPyItem = sound,
                        Content = renPySound.Content,
                    };

                    Queue.Add(queueItem);
                }
            }
        }

        private async Task ProcessQueue(bool isDelay = true)
        {
            while (Queue.Count > 0)
            {
                bool skipDelay = false; 
                var queueItem = Queue[0];
                switch (queueItem.QueueItemType)
                {
                    case QueueItemType.Image:
                        ImageSource = queueItem.Value;
                        skipDelay = true;
                        break;
                    case QueueItemType.Music:
                        var renPyMusic = await SQLiteService.GetAudioAsync(queueItem.RenPyItem!.Name);
                        if (renPyMusic == null)
                        {
                            skipDelay = true;
                        }
                        else if (renPyMusic.Content != null)
                        {
                            await TryPlayAudio(renPyMusic.Content, true);
                        }
                        break;
                    case QueueItemType.Sound:
                        var renPySound = await SQLiteService.GetAudioAsync(queueItem.RenPyItem!.Name);
                        if (renPySound == null)
                        {
                            skipDelay = true;
                        }
                        else if (renPySound.Content != null)
                        {
                            await TryPlayAudio(renPySound.Content);
                        }
                        break;
                }

                StateHasChangedCallback.Invoke();
                if (isDelay && !skipDelay)
                {
                    await Task.Delay(500);
                }

                Queue.RemoveAt(0);
            }
        }

        private async Task TryPlayAudio(byte[] content, bool isLoop = false)
        {
            var fileName = $"{Guid.NewGuid()}.mp3";
            if (content != null)
            {
                await AudioPlayerService.PlayFromByteArrayAsync(content, fileName, isLoop);
                StateHasChangedCallback.Invoke();
            }
        }

        private static string GetImageSource(byte[] content)
        {
            var base64str = Convert.ToBase64String(content);
            return string.Format("data:image/webp;base64,{0}", base64str);
        }

        private async Task SetCurrentCharacter(string content)
        {
            (string characterCode, string dialogueContent) = ExtractWordBeforeFirstQuote(content);
            if (!string.IsNullOrEmpty(characterCode))
            {
                CurrentCharacter = await SQLiteService.GetCharacterAsync(characterCode);
                CurrentDialogue = (CurrentDialogue.Index, dialogueContent);
            }
        }

        private static (string word, string remainingContent) ExtractWordBeforeFirstQuote(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return (string.Empty, string.Empty);
            }

            int quotePosition = content.IndexOf('\"');
            if (quotePosition <= 0)
            {
                return (string.Empty, content);
            }

            string beforeQuote = content[..quotePosition].Trim();
            string[] words = beforeQuote.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            string extractedWord = words.Length > 0 ? words[^1] : string.Empty;

            if (string.IsNullOrEmpty(extractedWord))
            {
                return (string.Empty, content);
            }

            int wordPosition = content.LastIndexOf(extractedWord, quotePosition);
            if (wordPosition < 0)
            {
                return (extractedWord, content);
            }

            string remaining = content.Remove(wordPosition, extractedWord.Length);
            return (extractedWord, remaining);
        }
    }

    public record EventDisplayQueueItem
    {
        public QueueItemType QueueItemType { get; set; }

        public string Value { get; set; } = string.Empty;

        public RenPyBase? RenPyItem { get; set; }

        public byte[] Content { get; set; } = [];
    }

    public enum QueueItemType
    {
        Image,

        Sound,

        Music
    }
}