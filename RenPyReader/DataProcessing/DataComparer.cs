using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using RenPyReader.Database;
using RenPyReader.Utilities;

namespace RenPyReader.DataProcessing
{
    internal class DataComparer
    {
        private readonly LogBuffer _logBuffer;

        private readonly DocumentDBManager _documentDBManager;

        private readonly string _connectionString = string.Empty;

        internal DataComparer(
            LogBuffer logBuffer,
            string connectionString,
            DocumentDBManager documentDBManager)
        {
            _logBuffer = logBuffer;
            _connectionString = connectionString;
            _documentDBManager = documentDBManager;
        }

        public async Task<DiffPaneModel?> GetDiffPaneModel(string title, string content)
        {
            var doesExist = await _documentDBManager.DoesDocumentExistAsync(title);
            if (!doesExist)
            {
                _logBuffer.Add($"File does not exist in the DB: {title}. " +
                    $"Skipping comparison.");
                return null;
            }

            var dbContent = await _documentDBManager.GetDocumentContentAsync(title);
            if (string.IsNullOrEmpty(dbContent))
            {
                _logBuffer.Add($"File does exist in the DB but the content is empty?" +
                    $"Skipping comparison.");
                return null;
            }

            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diffPaneModel = diffBuilder.BuildDiffModel(dbContent, content);
            if (!diffPaneModel.HasDifferences)
            {
                return null;
            }

            return diffPaneModel;
        }
    }
}
