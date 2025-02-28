using Microsoft.AspNetCore.Components;
using RenPyReader.DataModels;
using RenPyReader.Utilities;
using SQLitePCL;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RenPyReader.Components.Shared
{
    public partial class DatabaseHandler : ComponentBase
    {
        [Parameter]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] 
        public required LogBuffer LogBuffer { get; init; }

        private string databaseName = "RenPyReader";

        private DBManager? dbManager;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                dbManager = new DBManager(databaseName);
            }
        }
        
        public async Task InsertImageAsync(RenPyImage renPyImage)
        {
            try
            {
                await dbManager!.InsertImageAsync(renPyImage);
            }
            catch (Exception ex)
            {
                LogBuffer.Add($"Exception caught: {ex.Message}");
            }
        }
    }
}
