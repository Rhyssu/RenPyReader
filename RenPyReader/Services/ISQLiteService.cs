using RenPyReader.Utilities;

namespace RenPyReader.Services
{
    internal interface ISQLiteService
    {
        Task<List<string>> GetTableNamesAsync();

        Task<List<Dictionary<string, string>>> GetTableDataAsync(string tableName);

        void BatchInsertOrIgnoreSet(string tableName, OrderedSet<string> entries);

        void BatchInsertOrIgnoreMap(string tableName, OrderedSet<(Int64 ParentRowID, int ElementID, int LineIndex)> maps);

        Task<OrderedSet<string>> GetOrderedSet(string tableName);

        Task<OrderedSet<Entities.MapEntry>> GetOrderedMap(string tableName);

        Task<long> SaveDocumentAsync(string title, string content);

        Task<bool> DoesDocumentExistAsync(string title);

        Task<string> GetDocumentContentAsync(string title);

        Task<List<(long rowID, string title)>> GetAllDocumentTitlesAsync();
    }
}
