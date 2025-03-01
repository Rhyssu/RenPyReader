using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace RenPyReader.Components.Shared
{
    public partial class FilePropertyHandler
    {
        [Parameter]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public required string Label { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value { get; set; } = string.Empty;

        [Parameter]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? Hint { get; set; } = string.Empty;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Disabled { get; set; } = true;

        public void Update() => StateHasChanged();
    }
}