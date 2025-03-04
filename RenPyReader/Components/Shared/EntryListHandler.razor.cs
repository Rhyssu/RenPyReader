using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RenPyReader.Components.Shared
{
    public partial class EntryListHandler
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObservableCollection<EntryItem> EntryItems { get; set; } = new();

        public void AddItem(string entryName, Color newBackgroundColor)
        {
            EntryItems.Add(new EntryItem 
            { 
                EntryName = entryName, 
                BackgroundColor = newBackgroundColor
            });
        }

        public void Update(int index, Color newBackgroundColor)
        {
            if (index >= 0 && index < EntryItems.Count)
            {
                EntryItems[index].BackgroundColor = newBackgroundColor;
            }
        }

        public static string GetTooltipFromColor(Color color)
        {
            return color switch
            {
                var c when c == Colors.LightYellow      => "Entry is a directory.",
                var c when c == Colors.LightCoral       => "Extension not present.",
                var c when c == Colors.LightSeaGreen    => "Successfully processed file.",
                var c when c == Colors.LightSalmon      => "Extension not supported.",
                _                                       => "Unrecognizable color.",
            };
        }

        public void UpdateState()
        {
            StateHasChanged();
        }
    }

    public class EntryItem
    {
        public string EntryName { get; set; } = string.Empty;

        public Color BackgroundColor { get; set; } = Colors.White;
    }
}