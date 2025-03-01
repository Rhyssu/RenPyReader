using Microsoft.AspNetCore.Components;
using System.ComponentModel;

namespace RenPyReader.Components.Shared
{
    public partial class ProgressBarHandler : ComponentBase
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Total { get; set; } = 100;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Part { get; set; }

        private string ProgressBarStyle => $"width: {_progress}%";

        private int _progress;

        private string? _label;

        public void SetTotal(int total)
        {
            Total = total;
            StateHasChanged();
        }

        public int GetTotal()
        {
            return Total;
        }

        public void SetAndUpdatePart(int part)
        {
            Part = part;
            UpdateProgress();
        }

        public void UpdateProgress()
        {
            if (Total > 0)
            {
                _progress = (int)((double)Part / Total * 100);
            }
            else
            {
                _progress = 0;
            }

            _label = $"{_progress}%";
            StateHasChanged();
        }
    }
}
