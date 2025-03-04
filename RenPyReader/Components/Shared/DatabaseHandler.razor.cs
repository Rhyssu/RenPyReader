using Microsoft.AspNetCore.Components;

namespace RenPyReader.Components.Shared
{
    public partial class DatabaseHandler : ComponentBase
    {
        private string databaseName = "RenPyReader";

        public string GetDatabaseName()
        {
            return databaseName;
        }
    }
}
