using RenPyReader.Database;
using System.IO.Compression;

namespace RenPyReader.DataProcessing
{
    internal class RenPyExtractor(RenPyDBManager renPyDBManager)
    {
        private readonly RenPyDBManager _renPyDBManager = renPyDBManager;

        internal async Task<(Int64, string)> ExtractDataAndSave(ZipArchiveEntry file)
        {
            var fileContent = await GetFileContentAsync(file);
            var parentRowID 
                = await _renPyDBManager.SaveDocumentAsync(file.Name, fileContent);
            return (parentRowID, fileContent);
        }

        private async Task<string> GetFileContentAsync(ZipArchiveEntry entry)
        {
            await using (var entryStream = entry.Open())
            {
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    return !string.IsNullOrEmpty(content) ? content : string.Empty;
                }
            }
        }
    }
}