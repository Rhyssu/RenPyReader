using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.UI.Xaml.Automation.Peers;
using RenPyReader.Components.Shared;
using RenPyReader.Utilities;
using System.IO.Compression;
using System.IO.Pipes;
using ProgressBar = BlazorBootstrap.ProgressBar;

namespace RenPyReader.Components.Pages
{
    public partial class FileHandler : ComponentBase
    {
        private FileSizeHandler? _fileSizeHandler;

        private FilePropertyHandler? _filePropertyHandler;

        private FileMemoryUsageHandler? _fileMemoryUsageHandler;

        private InputFile? _inputFileReference;

        private FileResult? _selectedFile;

        private string? _errorMessage;

        private bool _isWorking;

        private List<ZipArchiveEntry>? _zipEntries;

        private List<string>? _zipEntriesNames;

        private string? _tempFilePath;

        private LogBuffer _logBuffer = new(10000);

        private ProgressBar? _progressBar;

        private PickOptions? _options;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _options = new PickOptions
                {
                    PickerTitle = "Select a zip file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, [".zip"] }
                    })
                };
            }
        }

        private async Task HandleFilePickerAsync()
        {
            if (_options == null)
            {
                throw new ArgumentNullException("File picker options are not set.");
            }

            try
            {
                _selectedFile = await FilePicker.PickAsync(_options);
            }
            catch (TaskCanceledException)
            {
                _logBuffer.Add("File picker task was canceled.");
            }
            finally
            {
                if (_selectedFile == null)
                {
                    _logBuffer.Add("No file selected.");
                }
                else
                {
                    _filePropertyHandler?.SetFile(_selectedFile);
                }

                StateHasChanged();
            }
        }

        private async Task HandleListEntriesAsync()
        {
            if (_selectedFile == null)
            {
                _logBuffer.Add("No file selected.");
                StateHasChanged();
                return;
            }

            StartWorking();
            
            try
            {
                using (var stream = await _selectedFile.OpenReadAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        _zipEntriesNames = [.. archive.Entries.Select(x => x.Name)];
                    }
                }
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

        private async Task ProcessEntriesAsync()
        {
            if (_selectedFile == null)
            {
                _logBuffer.Add("No file selected.");
                StateHasChanged();
                return;
            }

            StartWorking();

            try
            {
                using (var stream = await _selectedFile.OpenReadAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        var entryLock = new object();
                        Parallel.ForEach(archive.Entries, entry =>
                        {
                            lock (entryLock)
                            {
                                using (var entryStream = entry.Open())
                                {
                                    _logBuffer.Add("Processing entry: " + entry.FullName);
                                }
                            }
                        });
                    }
                }
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

        private void StartWorking()
        {
            _isWorking = true;
            _fileMemoryUsageHandler?.Start();
            StateHasChanged();
        }

        private void StopWorking()
        {
            _isWorking = false;
            _fileMemoryUsageHandler?.Stop();
            StateHasChanged();
        }

        private bool IsFileSelected => _selectedFile != null && !_isWorking;
    }
}