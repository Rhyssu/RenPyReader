using Microsoft.Data.Sqlite;
using RenPyReader.Database;
using RenPyReader.Entities;
using RenPyReader.Utilities;
using System.IO.Compression;

namespace RenPyReader.DataProcessing
{
    internal class RenPyExtractor(RenPyDBManager renPyDBManager, LogBuffer logBuffer)
    {
        // Log buffer for messages.
        private readonly LogBuffer _logBuffer = logBuffer;

        private readonly RenPyDBManager _renPyDBManager = renPyDBManager;

        // Processor for individual RenPy lines.
        private readonly RenPyProcessor _renPyLineProcessor = new();

        // Extracts data from a ZipArchiveEntry file and saves it to DB.
        internal async Task ExtractDataAndSave(ZipArchiveEntry file)
        {
            // Read the content of the file.
            var fileContent = await GetFileContentAsync(file);
            await _renPyDBManager.SaveDocumentAsync(file.Name, fileContent);
            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                _logBuffer.Add($"Processing file '{file.Name}' with content size: {fileContent.Length} characters.");
                await ProcessContentAsync(file.Name, fileContent);
                var transaction = _renPyDBManager.BeginAndGetTransaction();
                await _renPyDBManager.BatchSaveAsync(_logBuffer);
            }
            else
            {
                _logBuffer.Add($"Skipping file '{file.Name}': file content is empty.");
            }
        }

        // Processes the file content line by line.
        internal async Task ProcessContentAsync(string fileName, string fileContent)
        {
            uint currentLineNumber = 1;
            using var streamReader = new StringReader(fileContent);

            while (await streamReader.ReadLineAsync() is string line)
            {
                await ProcessLine(fileName, line, currentLineNumber);
                currentLineNumber++;
            }
        }

        // Processes a single line and adds the resulting RenPy item to the repository.
        private async Task ProcessLine(string fileName, string line, uint index)
        {
            var renpyLineContext = new LineContext(fileName, line.TrimStart(), index);
            var renPyItem = await _renPyLineProcessor.ProcessLineAsync(renpyLineContext);
            if (renPyItem != null)
            {
                try
                {
                    _renPyDBManager.AddItem(renPyItem);
                }
                catch (ArgumentException ex)
                {
                    _logBuffer.Add($"Unsupported RenPy item type: {ex}");
                }
            }
        }

        // Reads the content of a ZipArchiveEntry file asynchronously.
        private async Task<string> GetFileContentAsync(ZipArchiveEntry entry)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            try
            {
                var content = await reader.ReadToEndAsync();
                return !string.IsNullOrEmpty(content) ? content : string.Empty;
            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught while reading {entry.Name}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}