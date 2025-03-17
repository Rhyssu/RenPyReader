using RenPyReader.DataModels;
using RenPyReader.Utilities;

namespace RenPyReader.Components.Pages
{
    public partial class EventReader
    {
        private RenPyEvent? _event;

        private List<(int index, string content)> _eventDialogue = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _event = new RenPyEvent("iodorm35", "IoEvents.rpy", 4116);
                _eventDialogue = await GetEventDialogue();
            }
        }

        private async Task<List<(int index, string content)>> GetEventDialogue()
        {
            List<(int index, string content)> result = new();
            if (_event == null)
            {
                // No event selected, aborting
                return result;
            }

            string content = await SQLiteService.GetDocumentContentAsync(_event.Parent);
            if (string.IsNullOrEmpty(content))
            {
                // No parent content, aborting
                return result;
            }

            using StringReader reader = new(content);
            {
                string? line;
                int index = 0;
                bool isOnEvent = false;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (index == _event.Index)
                    {
                        // Start of the event reading
                        isOnEvent = true;
                    }

                    if (isOnEvent && RegexProcessor.IsDialogue(line))
                    {
                        var trimLine = line.Trim();
                        result.Add((index, trimLine));
                    }

                    if (isOnEvent && line.TrimStart().Contains("jump"))
                    {
                        // End of the event reading
                        break;
                    }

                    index += 1;
                }
            }

            return result;
        }
    }
}