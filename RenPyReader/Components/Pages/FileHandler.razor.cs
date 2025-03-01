using Microsoft.AspNetCore.Components;
using RenPyReader.Components.Shared;
using RenPyReader.DataModels;
using RenPyReader.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.IO.Compression;
using Image = SixLabors.ImageSharp.Image;

namespace RenPyReader.Components.Pages
{
    public partial class FileHandler : ComponentBase
    {
        private DatabaseHandler? _databaseHandler;

        private FilePropertyHandler? _nameHandler;

        private FilePropertyHandler? _progressHandler;

        private Dictionary<string, Func<ZipArchiveEntry, Task>>? _fileHandlers;

        private FileResult? _selectedFile;
        
        private LogBuffer _logBuffer = new(10000);

        private PickOptions? _options;

        private List<string>? _zipEntriesNames;

        private bool _isWorking;

        private bool _parallelProcessing;

        private bool _newEntriesOnly;

        private HashSet<string>? _audioEntries;

        private HashSet<string>? _imageEntries;

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

                _fileHandlers = new Dictionary<string, 
                    Func<ZipArchiveEntry, Task>>(StringComparer.OrdinalIgnoreCase)
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
                    _nameHandler!.Value = _selectedFile.FileName;
                    _nameHandler.Update();
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
                await using (var stream = await _selectedFile.OpenReadAsync())
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
                await using (var stream = await _selectedFile.OpenReadAsync())
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        foreach (var (entry, index) in archive.Entries.Select((entry, index) => (entry, index)))
                        {
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
                                await fileHandler(entry);
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
                StopWorking();
            }
        }

        private async Task ProcessImageFileAsync(ZipArchiveEntry entry)
        {
            try
            {
                await using (var entryStream = entry.Open())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await entryStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        IImageFormat format = Image.DetectFormat(memoryStream);
                        if (format != null)
                        {
                            memoryStream.Position = 0;
                            using (var image = Image.Load(memoryStream))
                            {
                                image.Mutate(x => x.Resize(960, 540));
                                await using (var outputStream = new MemoryStream())
                                {
                                    image.Save(outputStream, format);
                                    byte[] imageData = outputStream.ToArray();
                                    var newImage = new RenPyImage(
                                        entry.Name, imageData);

                                    await _databaseHandler!.InsertImageAsync(newImage);
                                }
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

            }
        }

        private async Task ProcessAudioFileAsync(ZipArchiveEntry entry)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
            }
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
                _logBuffer.Add("New entries only has been ENABLED. " +
                    "Only files that are not in the DB will be processed.");

                _audioEntries = await _databaseHandler!.GetBinaryEntriesNamesAsync("audios");
                _logBuffer.Add($"Retrieved {_audioEntries.Count} audio file entries.");

                _imageEntries = await _databaseHandler!.GetBinaryEntriesNamesAsync("images");
                _logBuffer.Add($"Retrieved {_imageEntries.Count} image file entries.");
            }
            else
            {
                _logBuffer.Add("New entries only has been DISABLED. " +
                    "All files will be processed.");

                _audioEntries?.Clear();
                _imageEntries?.Clear();
                _logBuffer.Add("Cleared both audio and image entries names.");
            }
            StateHasChanged();
        }

        private void ParallelProcessingChanged(bool value)
        {
            _parallelProcessing = value;

            if (value)
                _logBuffer.Add("Parallel processing has been ENABLED.");
            else
                _logBuffer.Add("Parallel processing has been DISABLED.");

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