using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel;
using System.Security.Permissions;

namespace RenPyReader.Components.Shared
{
    public partial class FilePropertyHandler
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private IBrowserFile? File { get; set; }

        public void SetFile(IBrowserFile file)
        {
            File = file;
            StateHasChanged();
        }
    }
}
