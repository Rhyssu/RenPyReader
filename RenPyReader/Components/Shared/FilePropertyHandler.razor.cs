using System.ComponentModel;

namespace RenPyReader.Components.Shared
{
    public partial class FilePropertyHandler
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private FileResult? File { get; set; }

        public void SetFile(FileResult file)
        {
            File = file;
            StateHasChanged();
        }
    }
}