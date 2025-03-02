using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RenPyReader.Components.Shared
{
    public partial class EntryListHandler
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObservableCollection<EntryItem> EntryItems { get; set; } = new();

        public void AddItem(string entryName)
        {
            EntryItems.Add(new EntryItem 
            { 
                EntryName = entryName, 
                BackgroundColor = Colors.White 
            });
        }

        public void Update(int index, Color newBackgroundColor)
        {
            if (index >= 0 && index < EntryItems.Count)
            {
                EntryItems[index].BackgroundColor = newBackgroundColor;
            }
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