using Microsoft.AspNetCore.Components;
using RenPyReader.Components.Shared;
using RenPyReader.DataProcessing;
using RenPyReader.Utilities;
using SixLabors.ImageSharp;
using System.Diagnostics;
using System.IO.Compression;

namespace RenPyReader.Components.Pages
{
    public partial class FileHandler : ComponentBase
    {
        private DatabaseHandler? _databaseHandler;

        private FilePropertyHandler? _nameHandler;

        private FilePropertyHandler? _progressHandler;

        private ProgressBarHandler? _progressBarHandler;

        private Dictionary<string, Func<ZipArchiveEntry, Task>>? _fileHandlers;

        private ImageProcessor? _imageProcessor;

        private AudioProcessor? _audioProcessor;

        private FileResult? _selectedFile;
        
        private LogBuffer _logBuffer = new(10000);

        private PickOptions? _options;

        private List<string>? _zipEntriesNames;

        private bool _isWorking;

        private bool _newEntriesOnly;

        private HashSet<string>? _audioEntries;

        private HashSet<string>? _imageEntries;

        private int _part = 0;

        private int _total = 0;

        private int _processedCount = 0;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                _options = new PickOptions
                {
                    PickerTitle = "Select a zip file",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, [".zip"] }
                    })
                };

                _fileHandlers = new Dictionary<string, Func<ZipArchiveEntry, Task>>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    { ".png",   ProcessImageFileAsync },
                    { ".jpg",   ProcessImageFileAsync },
                    { ".jpeg",  ProcessImageFileAsync },
                    { ".gif",   ProcessImageFileAsync },
                    { ".bmp",   ProcessImageFileAsync },
                    { ".tiff",  ProcessImageFileAsync },
                    { ".tif",   ProcessImageFileAsync },
                    { ".ico",   ProcessImageFileAsync },
                    { ".webp",  ProcessImageFileAsync },
                    { ".mp3",   ProcessAudioFileAsync },
                    { ".wav",   ProcessAudioFileAsync },
                    { ".rpy",   ProcessRenPyFileAsync }
                };

                _imageProcessor = new ImageProcessor(_databaseHandler!, _logBuffer);
            }
        }

        private async Task HandleFilePickerAsync()
        {
            if (_options == null)
            {
                throw new ArgumentNullException("fileResult picker options are not set.");
            }

            try
            {
                _selectedFile = await FilePicker.PickAsync(_options);
            }
            catch (TaskCanceledException)
            {
                _logBuffer.Add("fileResult picker task was canceled.");
            }
            finally
            {
                if (_selectedFile == null)
                {
                    _logBuffer.Add("No file selected.");
                }
                else
                {
                    _logBuffer.Add("File successfully selected.");
                    _nameHandler!.Value = _selectedFile.FileName;
                    _nameHandler.Update();
                }

                StateHasChanged();
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

            _logBuffer.Add($"Processing of {_selectedFile.FileName} started.");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            StartWorking();

            try
            {
                await using (var stream = await _selectedFile.OpenReadAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        var entryProcessTasks = new List<Task>();
                        var entriesCount = archive.Entries.Count;
                        _logBuffer.Add($"Found {entriesCount} entries to process.");
                        _progressBarHandler!.SetTotal(entriesCount);
                        _processedCount = 0;
                        StateHasChanged();

                        foreach (var (entry, index) in archive.Entries.Select((entry, index) => (entry, index)))
                        {
                            _progressBarHandler!.SetAndUpdatePart(index);
                            if (entry.FullName.EndsWith('/'))
                            {
                                continue;
                            }

                            var extension = Path.GetExtension(entry.Name);
                            if (string.IsNullOrEmpty(extension))
                            {
                                return;
                            }

                            if (_fileHandlers?.TryGetValue(extension, out var fileHandler) == true)
                            {
                                entryProcessTasks.Add(fileHandler(entry));
                                _processedCount += 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                _logBuffer.Add($"Finished processing. Processed {_processedCount} out of {_progressBarHandler!.GetTotal()} entries.");
                _logBuffer.Add($"Processing took {stopwatch.ElapsedMilliseconds} ms in total.");
                StopWorking();
            }
        }

        private async Task ProcessImageFileAsync(ZipArchiveEntry entry)
        {
            var skip = SkipImageProcessing(entry.Name);
            if (!skip)
            {
                await _imageProcessor!.ProcessImageAsync(entry);
            }
        }

        private bool SkipImageProcessing(string imageName)
        {
            var doesExist = false;
            if (_newEntriesOnly && _imageEntries!.Count != 0)
            {
                doesExist = _imageEntries.Contains(imageName);
            }

            return doesExist;
        }

        private async Task ProcessAudioFileAsync(ZipArchiveEntry entry)
        {
            var skip = SkipAudioProcessing(entry.Name);
            if (!skip)
            {
                await _audioProcessor!.ProcessAudioAsync(entry);
            }
        }

        private bool SkipAudioProcessing(string audioName)
        {
            var doesExist = false;
            if (_newEntriesOnly && _audioEntries!.Count != 0)
            {
                doesExist = _audioEntries.Contains(audioName);
            }

            return doesExist;
        }

        private async Task ProcessRenPyFileAsync(ZipArchiveEntry entry)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
            }
        }

        private async Task NewEntriesOnlyChangedAsync(bool value)
        {
            _newEntriesOnly = value;

            if (value)
            {
                _logBuffer.Add("New entries only has been ENABLED. Only files that are not in the DB will be processed.");

                _audioEntries = await _databaseHandler!.GetBinaryEntriesNamesAsync("audios");
                _logBuffer.Add($"Retrieved {_audioEntries.Count} audio file entries.");

                _imageEntries = await _databaseHandler!.GetBinaryEntriesNamesAsync("images");
                _logBuffer.Add($"Retrieved {_imageEntries.Count} image file entries.");
            }
            else
            {
                _logBuffer.Add("New entries only has been DISABLED. All files will be processed.");

                _audioEntries?.Clear();
                _imageEntries?.Clear();

                _logBuffer.Add("Cleared both audio and image entries names.");
            }

            StateHasChanged();
        }

        private void StartWorking()
        {
            _isWorking = true;
            StateHasChanged();
        }

        private void StopWorking()
        {
            _isWorking = false;
            StateHasChanged();
        }

        private bool IsFileSelected => _selectedFile != null && !_isWorking;
    }
}