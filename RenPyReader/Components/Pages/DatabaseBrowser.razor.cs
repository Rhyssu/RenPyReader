namespace RenPyReader.Components.Pages
{
    public partial class DatabaseBrowser
    {
        private List<string> _tableNames = new();

        private string _errorMessage = string.Empty;

        private string _selectedTableName = string.Empty;

        private List<Dictionary<string, string>> _tableData = new();

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
}