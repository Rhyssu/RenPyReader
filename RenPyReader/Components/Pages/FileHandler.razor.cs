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
        private FileSizeHandler? _fileSizeHandler;

        private DatabaseHandler? _databaseHandler;

        private FilePropertyHandler? _filePropertyHandler;

        private FileMemoryUsageHandler? _fileMemoryUsageHandler;

        private FileResult? _selectedFile;

        private bool _isWorking;

        private List<string>? _zipEntriesNames;

        private LogBuffer _logBuffer = new(10000);

        private Dictionary<string, Func<ZipArchiveEntry, Task>>? _fileHandlers;

        private PickOptions? _options;

        private int _entriesProcessedCount = 0;

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
                    { ".png",   ProcessImageFile },
                    { ".jpg",   ProcessImageFile },
                    { ".jpeg",  ProcessImageFile },
                    { ".gif",   ProcessImageFile },
                    { ".bmp",   ProcessImageFile },
                    { ".tiff",  ProcessImageFile },
                    { ".tif",   ProcessImageFile },
                    { ".ico",   ProcessImageFile },
                    { ".webp",  ProcessImageFile },
                    { ".mp3",   ProcessAudioFile },
                    { ".wav",   ProcessAudioFile },
                    { ".rpy",   ProcessRenPyFile }
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
                        foreach (var entry in archive.Entries)
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

        private async Task ProcessImageFile(ZipArchiveEntry entry)
        {
            try
            {
                using (var entryStream = entry.Open())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await entryStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        IImageFormat format = Image.DetectFormat(memoryStream);
                        if (format != null)
                        {
                            memoryStream.Position = 0;
                            using var image = Image.Load(memoryStream);
                            image.Mutate(x => x.Resize(960, 540));

                            using var outputStream = new MemoryStream();
                            image.Save(outputStream, format);
                            byte[] imageData = outputStream.ToArray();

                            RenPyImage newImage = new(entry.Name, imageData);
                            await _databaseHandler!.InsertImageAsync(newImage);
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
                _entriesProcessedCount += 1;
            }
        }

        private async Task ProcessAudioFile(ZipArchiveEntry entry)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
            }
        }

        private async Task ProcessRenPyFile(ZipArchiveEntry entry)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logBuffer.Add($"Exception caught: {ex.Message}");
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