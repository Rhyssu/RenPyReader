using Microsoft.AspNetCore.Components.Web;
using RenPyReader.DataModels;
using RenPyReader.Entities;
using RenPyReader.Utilities;

namespace RenPyReader.Components.Pages
{
    public partial class EventReader
    {
        private OrderedSet<string> _eventNames = new();

        List<(long rowID, string title)> _documentTitles = new();

        private OrderedSet<MapEntry> _eventMap = new();

        private List<IGrouping<(string, int), string>> _eventSearchResultMap = new();

        private string _inputText = string.Empty;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _eventNames     = await SQLiteService.GetOrderedSet("events");
                _eventMap       = await SQLiteService.GetOrderedMap("events");
                _documentTitles = await SQLiteService.GetAllDocumentTitlesAsync();
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrEmpty(_inputText))
            {
                StateHasChanged();

                var _searchResults = await SQLiteService.QuickSearchAsync(_inputText);
                FilterEvents(_searchResults);
            }
        }

        private void FilterEvents(List<RenPySearchResult> searchResults)
        {
            var searchResultParentIDs = searchResults
                .Select(s => s.ParentId)
                .ToList();
            var parentFilteredEventMap = _eventMap
                .Where(e => searchResultParentIDs.Contains(e.ParentRowID))
                .ToList();

            var eventSearchResultMap = searchResults.Select(s =>
            {
                var eventMatch = parentFilteredEventMap
                .Where(e => e.ParentRowID == s.ParentId && e.LineIndex <= s.Line)
                .OrderByDescending(e => e.LineIndex)
                .FirstOrDefault();

                return new
                {
                    s.Title,
                    EventID = eventMatch.ElementRowID,
                    SearchResultContent = s.Content
                };
            }).GroupBy(g => g.EventID)
            .Select(g => new
            {
                g.Key,
                g.First().Title,
                SearchResultContents = g.Select(x => x.SearchResultContent).ToList()
            })
            .GroupBy(g => (g.Title, g.Key), g => g.SearchResultContents)
            .OrderBy(g => g.Key)
            .ToList();
        }

        private string GetFilteredEventHeader(string title, int eventID)
        {
            var eventName = _eventNames[eventID];
            if (string.IsNullOrEmpty(eventName))
            {
                eventName = "Not found (?)";
            }

            return $"File: {title} | Event: {eventName}";
        }
    }
}