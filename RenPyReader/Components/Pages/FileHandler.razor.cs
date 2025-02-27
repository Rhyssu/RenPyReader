using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using RenPyReader.Components.Shared;
using RenPyReader.Utilities;
using System.IO.Compression;
using ProgressBar = BlazorBootstrap.ProgressBar;

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

        private string? _tempFilePath;

        private LogBuffer _logBuffer = new(10000);

        private ProgressBar? _progressBar;

        private void HandleInputFile(InputFileChangeEventArgs e)
        {
            _selectedFile = null;
            IBrowserFile file = e.File;

            if (file == null)
            {
                _logBuffer.Add("No file was uploaded.");
                StateHasChanged();
                return;
            }

            if (!Path.GetExtension(file.Name).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logBuffer.Add("File must be zip.");
                StateHasChanged();
                return;
            }

            _selectedFile = file;
            _filePropertyHandler?.SetFile(file);
            StateHasChanged();
        }

        private async Task Process()
        {
            StartWorking();

            try
            {
                _tempFilePath = Path.GetTempFileName();

                await CreateTempFileAsync();
                await ProcessTempFileAsync();
            }
            catch (IOException iex)
            {
                _logBuffer.Add($"IOException caught: {iex.Message}");
            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
            }
            finally
            {
                StopWorking();
            }
        }

        private async Task CreateTempFileAsync()
        {
            _logBuffer.Add("Creating new temporary file...");

            using var tempFileStream = new FileStream(
                _tempFilePath!, FileMode.Create, FileAccess.Write, FileShare.Read);
            await _selectedFile!.OpenReadStream(maxAllowedSize: long.MaxValue)
                .CopyToAsync(tempFileStream);

            FileInfo? fileInfo = new(_tempFilePath!);
            long fileSize = fileInfo.Length;

            _logBuffer.Add($"Successfully created new temporary file with size: {fileSize} bytes.");
        }

        private async Task ProcessTempFileAsync()
        {
            _logBuffer.Add("Processing uploaded zip file.");

            using var tempFileStream = new FileStream(
                _tempFilePath!, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var archive = new ZipArchive(tempFileStream, ZipArchiveMode.Read);

            int archiveCount = archive.Entries.Count;
            _logBuffer.Add($"Found {archiveCount} inside uploaded zip file.");
            StateHasChanged();

            int currentEntryIndex = 0;
            foreach (var entry in archive.Entries)
            {
                
            }
        }

        private void StartWorking()
        {
            _isWorking = true;
            _fileMemoryUsageHandler!.Start();

            StateHasChanged();
        }

        private void StopWorking()
        {
            _isWorking = false;
            _fileMemoryUsageHandler!.Stop();

            StateHasChanged();
        }

        private bool IsFileSelected => _selectedFile != null && !_isWorking;

        private async Task TriggerFileInputAsync()
        {
            await JSRuntime.InvokeVoidAsync("triggerInputFile", "inputFileID");
        }
    }
}