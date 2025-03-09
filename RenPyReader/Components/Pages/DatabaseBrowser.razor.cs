using Microsoft.AspNetCore.Components;

namespace RenPyReader.Components.Pages
{
    public partial class DatabaseBrowser : ComponentBase
    {
        private List<string> _tableNames = new();

        private string _errorMessage = string.Empty;

        private string _selectedTableName = string.Empty;

        private List<Dictionary<string, string>> _tableData = new();

        private List<Dictionary<string, string>> _filteredTableData = new();

        private bool _isWorking;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _isWorking = true;
                StateHasChanged();

                try
                {
                    _tableNames = await SQLiteService.GetTableNamesAsync();
                    _tableNames = _tableNames.Where(tn => !tn.Contains("documents")).ToList();
                }
                catch (Exception ex)
                {
                    _errorMessage = ex.Message;
                }
                finally
                {
                    _isWorking = false;
                    StateHasChanged();
                }
            }
        }

        private async Task OnSelectedTableName(string tableName)
        {
            _isWorking = true;
            StateHasChanged();

            try
            {
                _tableData = await SQLiteService.GetTableDataAsync(tableName);
                _filteredTableData = _tableData;
            }
            catch (Exception ex)
            {
                _errorMessage = ex.Message;
            }
            finally
            {
                _isWorking = false;
                StateHasChanged();
            }
        }

        private void OnFilterTextChanged(ChangeEventArgs e, string columnName)
        {
            var filterText = e.Value?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filterText))
            {
                _filteredTableData = _tableData;
            }
            else
            {
                bool isNumericColumn = _tableData.FirstOrDefault()?.ContainsKey(columnName) == true 
                    && double.TryParse(_tableData.First()[columnName], out _);
                if (isNumericColumn)
                {
                    _filteredTableData = _tableData.Where(row => row[columnName]?
                    .Equals(filterText, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
                }
                else
                {
                    _filteredTableData = _tableData.Where(row => row[columnName]?
                    .Contains(filterText, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
                }
            }
        }
    }
}