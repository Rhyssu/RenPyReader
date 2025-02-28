namespace RenPyReader.Components.Shared
{
    public partial class FilePropertyHandler
    {
        private FileResult? fileResult;

        public void SetFile(FileResult file)
        {
            fileResult = file;
            StateHasChanged();
        }
    }
}