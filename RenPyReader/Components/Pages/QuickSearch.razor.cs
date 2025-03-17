using Microsoft.AspNetCore.Components.Web;
using RenPyReader.DataModels;

namespace RenPyReader.Components.Pages
{
    public partial class QuickSearch
    {
        private List<RenPyEvent> _events = new();

        private List<RenPySearchResult> _searchResults = new();

        Dictionary<RenPyEvent, List<RenPySearchResult>> _searchEventDict = new();

        private string _inputText = string.Empty;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _events = await SQLiteService.GetRenPyEventsAsync();
            }
        }

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrEmpty(_inputText))
            {
                StateHasChanged();

                _searchResults = await SQLiteService.QuickSearchAsync(_inputText);
                _searchEventDict = CreateDictionary(_searchResults);
            }
        }

        private Dictionary<RenPyEvent, List<RenPySearchResult>> CreateDictionary(List<RenPySearchResult> searchResults)
        {
            var parentFiles = searchResults.Select(sr => sr.Title).ToList();
            var possibleEvents = _events.Where(e => parentFiles.Contains(e.Parent)).ToList();
            var searchResultsMatches = new List<(RenPySearchResult sr, RenPyEvent e)>();

            foreach (var searchResult in searchResults)
            {
                var matchingEvent = possibleEvents
                    .Where(e => e.Parent == searchResult.Title && e.Index <= searchResult.Line)
                    .OrderByDescending(e => e.Index)
                    .FirstOrDefault();
                if (matchingEvent != null)
                {
                    searchResultsMatches.Add((searchResult, matchingEvent));
                }
            }

            var searchEventDict = searchResultsMatches
                .GroupBy(match => match.e)
                .ToDictionary(group => group.Key, group => group.Select(match => match.sr).ToList());
            return searchEventDict;
        }
    }
}
