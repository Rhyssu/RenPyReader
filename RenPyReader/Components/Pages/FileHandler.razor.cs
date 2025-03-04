using Microsoft.AspNetCore.Components;
using RenPyReader.Components.Shared;
using RenPyReader.Database;
using RenPyReader.DataProcessing;
using RenPyReader.Utilities;
using SixLabors.ImageSharp;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Versioning;
using Color = Microsoft.Maui.Graphics.Color;

namespace RenPyReader.Components.Pages
{
    public partial class FileHandler : ComponentBase
    {
        private DatabaseHandler? _databaseHandler;

        private DocumentDBManager? _documentDBManager;

        private FilePropertyHandler? _nameHandler;

        private ProgressBarHandler? _progressBarHandler;

        private FilePropertyHandler? _imageCountHandler;

        private FilePropertyHandler? _audioCountHandler;

        private FilePropertyHandler? _renPyCountHandler;

        private Dictionary<string, Func<ZipArchiveEntry, Task>>? _fileHandlers;

        private ImageProcessor? _imageProcessor;

        private AudioProcessor? _audioProcessor;

        private FileResult? _selectedFile;

        private LogBuffer _logBuffer = new(10000);

        private PickOptions? _options;

        private bool _isWorking;

        private bool _newEntriesOnly;

        private CancellationTokenSource? _cts;

        private HashSet<string>? _audioEntries;

        private HashSet<string>? _imageEntries;

        private RenPyDBManager? _renPyDBManager;

        private EntryListHandler? _entryListHandler;

        private ObservableCollection<EntryItem>? EntryItems;

        private int _imageCount, _audioCount, _renPyCount = 0;

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

                _cts = new CancellationTokenSource();
                string databaseName = _databaseHandler!.GetDatabaseName();
                _renPyDBManager = new RenPyDBManager(databaseName);
                _imageProcessor = new ImageProcessor(_renPyDBManager, _logBuffer);
                _audioProcessor = new AudioProcessor(_renPyDBManager, _logBuffer);
            }
        }

        [SupportedOSPlatform("windows10.0.17763.0")]
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
            var cancellationToken = _cts!.Token;
            stopwatch.Start();
            StartWorking();

            try
            {
                await using (var stream = await _selectedFile.OpenReadAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        var entriesCount = archive.Entries.Count;
                        _logBuffer.Add($"Found {entriesCount} entries to process.");
                        _progressBarHandler!.SetTotal(entriesCount);
                        ClearProcessedCounts();
                        StateHasChanged();

                        foreach (var (entry, index) in archive.Entries.Select((entry, index) => (entry, index)))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logBuffer.Add("Processing was cancelled.");
                                break;
                            }

                            _progressBarHandler.SetAndUpdatePart(index + 1);
                            if (entry.FullName.EndsWith('/'))
                            {
                                AddEntryList(entry.FullName, Colors.LightYellow);
                                continue;
                            }

                            var extension = Path.GetExtension(entry.Name);
                            if (string.IsNullOrEmpty(extension))
                            {
                                AddEntryList(entry.Name, Colors.LightCoral);
                                continue;
                            }

                            if (_fileHandlers?.TryGetValue(extension, out var fileHandler) == true)
                            {
                                AddEntryList(entry.Name, Colors.PaleGreen);
                                await fileHandler(entry);
                                continue;
                            }

                            AddEntryList(entry.Name, Colors.LightSalmon);
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
                _entryListHandler!.UpdateState();
                _logBuffer.Add($"Processing took {stopwatch.ElapsedMilliseconds} ms in total.");
                StopWorking();
            }
        }

        private void AddEntryList(string entryName, Color newBackgroundColor)
        {
            if (!string.IsNullOrEmpty(entryName))
            {
                _entryListHandler!.AddItem(entryName, newBackgroundColor);
            }
        }

        private void ClearProcessedCounts()
        {
            _imageCountHandler!.Value = "";
            _imageCount = 0;

            _audioCountHandler!.Value = "";
            _audioCount = 0;

            _renPyCountHandler!.Value = "";
            _renPyCount = 0;
        }

        private async Task ProcessImageFileAsync(ZipArchiveEntry entry)
        {
            //var skip = SkipImageProcessing(entry.Name);
            //if (!skip)
            //{
            //    await _imageProcessor!.ProcessImageAsync(entry);
            //    _imageCountHandler!.Value = (_imageCount += 1).ToString();
            //    _imageCountHandler!.Update();
            //}
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
            //var skip = SkipAudioProcessing(entry.Name);
            //if (!skip)
            //{
            //    await _audioProcessor!.ProcessAudioAsync(entry);
            //    _audioCountHandler!.Value = (_audioCount += 1).ToString();
            //    _audioCountHandler!.Update();
            //}
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
            _renPyDBManager!.ClearRepository();
            var renPyExtractor = new RenPyExtractor(_renPyDBManager!, _logBuffer);
            await renPyExtractor.ExtractDataAndSave(entry);

            _renPyCountHandler!.Value = (_renPyCount += 1).ToString();
            _renPyCountHandler!.Update();
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