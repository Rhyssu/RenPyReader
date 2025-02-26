using Microsoft.AspNetCore.Components.Forms;
using RenPyReader.Components.Shared;
using Microsoft.JSInterop;
using System.IO.Compression;

namespace RenPyReader.Components.Pages
{
    public partial class FileHandler
    {
        private FileSizeHandler? _fileSizeHandler;

        private FilePropertyHandler? _filePropertyHandler;

        private FileMemoryUsageHandler? _fileMemoryUsageHandler;

        private InputFile? _inputFileReference;

        private IBrowserFile? _selectedFile;

        private string? _errorMessage;

        private bool _isWorking;

        private List<ZipArchiveEntry>? _zipEntries;

        private List<string>? _zipEntriesNames;

        private void HandleInputFile(InputFileChangeEventArgs e)
        {
            _selectedFile = null;
            IBrowserFile file = e.File;

            if (file == null)
            {
                _errorMessage = "No file was uploaded.";
                StateHasChanged();
                return;
            }

            if (!Path.GetExtension(file.Name).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _errorMessage = "File must be zip.";
                StateHasChanged();
                return;
            }

            _selectedFile = file;
            _filePropertyHandler?.SetFile(file);
            StateHasChanged();
        }

        private async Task Scan()
        {
            _isWorking = true;
            _fileMemoryUsageHandler!.Start();
            StateHasChanged();

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    var maxFileSize = _fileSizeHandler!
                        .GetMaximumSizeBytes();
                    await _selectedFile!
                        .OpenReadStream(maxFileSize)
                        .CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (var archive = new ZipArchive(
                        memoryStream, ZipArchiveMode.Read))
                    {
                        _zipEntriesNames = 
                            [.. archive.Entries.Select(x => x.Name)];
                    }
                }
            }
            finally
            {
                _isWorking = false;
                _fileMemoryUsageHandler!.Stop();
                StateHasChanged();
            }
        }

        private async Task Process()
        {

        }

        private bool IsFileSelected => _selectedFile != null && !_isWorking;

        private async Task TriggerFileInputAsync()
        {
            await JSRuntime.InvokeVoidAsync("triggerInputFile", "inputFileID");
        }
    }
}