using RenPyReader.DataModels;

namespace RenPyReader.Services
{
    internal interface ISQLiteService
    {
        Task<List<string>> GetTableNamesAsync();

        Task<List<Dictionary<string, string>>> GetTableDataAsync(string tableName);

        Task<List<RenPyEvent>> GetRenPyEventsAsync();

        Task<RenPyEvent?> GetRenPyEventAsync(string eventName);

        Task<List<RenPyBase>> GetRenPyBaseTableAsync(string tableName, string parentName, int start, int end);

        Task SaveDocumentAsync(string title, string content);

        Task<bool> DoesDocumentExistAsync(string title);

        Task<string> GetDocumentContentAsync(string title);

        Task<List<(long rowID, string title)>> GetAllDocumentTitlesAsync();

        Task BatchInsertOrReplaceBaseTableAsync(string tableName, List<RenPyBase> renPyBaseEntries);

        Task BatchInsertOrReplaceCharactersAsync(List<RenPyCharacter> characters);

        Task<List<RenPySearchResult>> QuickSearchAsync(string searchPhrase, bool useFullWord = false);

        Task InsertAudioAsync(RenPyAudio renPyAudio);

        Task InsertImageAsync(RenPyImage renPyImage);

        Task<RenPyImage?> GetImageAsync(string sceneName);
    }
}
