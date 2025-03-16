using RenPyReader.DataModels;

namespace RenPyReader.Services
{
    internal interface ISQLiteService
    {
        Task<List<string>> GetTableNamesAsync();

        Task<List<Dictionary<string, string>>> GetTableDataAsync(string tableName);

        Task SaveDocumentAsync(string title, string content);

        Task<bool> DoesDocumentExistAsync(string title);

        Task<string> GetDocumentContentAsync(string title);

        Task<List<(long rowID, string title)>> GetAllDocumentTitlesAsync();

        Task BatchInsertOrIgnoreBaseTable(string tableName, List<RenPyBase> renPyBaseEntries);

        Task BatchInsertOrIgnoreCharacters(List<RenPyCharacter> characters);

        Task<List<RenPySearchResult>> QuickSearchAsync(string searchPhrase, bool useFullWord = false);
    }
}
