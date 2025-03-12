using Microsoft.AspNetCore.Components.Web;
using RenPyReader.DataModels;

namespace RenPyReader.Components.Pages
{
    public partial class QuickSearch
    {
        private string? InputText { get; set; }

        private List<RenPySearchResult> SearchResults { get; set; } = new();

        private async Task HandleKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrEmpty(InputText))
            {
                StateHasChanged();
                SearchResults = await SQLiteService.QuickSearchAsync(InputText);
            }
        }
    }
}
