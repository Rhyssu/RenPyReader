using AudioTrack = RenPyReader.DataModels.AudioTrack;

namespace RenPyReader.Services
{
    public class AudioPlayerService(IDispatcher dispatcher) : IAudioPlayerService
    {
        private readonly List<string> tempFilePaths = new();
        
        private readonly Dictionary<string, AudioTrack> audioTracks = new();

        public bool IsPlaying => audioTracks.Values.Any(track => track.IsPlaying);

        public bool IsTrackPlaying(string trackId)
        {
            return audioTracks.TryGetValue(trackId, out var track) && track.IsPlaying;
        }

        public async Task<string> PlayFromByteArrayAsync(byte[] audioData, string fileName, bool loop = false, string? trackId = null)
        {
            string actualTrackId = trackId ?? Guid.NewGuid().ToString();

            if (audioTracks.ContainsKey(actualTrackId))
            {
                await StopAsync(actualTrackId);
            }

            string cacheDir = FileSystem.CacheDirectory;
            string tempFilePath = Path.Combine(cacheDir, $"{actualTrackId}_{fileName}");

            try
            {
                await File.WriteAllBytesAsync(tempFilePath, audioData);

                if (!tempFilePaths.Contains(tempFilePath))
                {
                    tempFilePaths.Add(tempFilePath);
                }

                var cancellationTokenSource = new CancellationTokenSource();
                var track = new AudioTrack
                {
                    TrackId = actualTrackId,
                    FilePath = tempFilePath,
                    IsPlaying = true,
                    IsLooping = loop,
                    CancellationTokenSource = cancellationTokenSource
                };

                audioTracks[actualTrackId] = track;
                _ = PlayWindowsTrackAsync(track);

                return actualTrackId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio: {ex.Message}");
                if (audioTracks.ContainsKey(actualTrackId))
                {
                    await StopAsync(actualTrackId);
                }

                return actualTrackId;
            }
        }

        public async Task StopAsync(string? trackId = null)
        {
            if (string.IsNullOrEmpty(trackId))
            {
                var firstTrack = audioTracks.Values.FirstOrDefault();
                if (firstTrack != null)
                {
                    await StopTrackAsync(firstTrack.TrackId);
                }
            }
            else
            {
                await StopTrackAsync(trackId);
            }
        }

        public async Task StopSoundsAsync()
        {
            var sounds = audioTracks.Select(a => a.Value).Where(a => a.IsLooping == false);
            foreach (var sound in sounds)
            {
                await StopTrackAsync(sound.TrackId);
            }
        }

        public async Task StopAllAsync()
        {
            foreach (var trackId in audioTracks.Keys.ToList())
            {
                await StopTrackAsync(trackId);
            }
        }

        private async Task StopTrackAsync(string trackId)
        {
            if (audioTracks.TryGetValue(trackId, out var track))
            {
                track.CancellationTokenSource?.Cancel();
                track.CancellationTokenSource?.Dispose();
                track.MediaPlayer?.Dispose();
                track.IsPlaying = false;

                audioTracks.Remove(trackId);
            }

            await Task.CompletedTask;
        }

        private async Task PlayWindowsTrackAsync(AudioTrack track)
        {
            track.MediaPlayer = new Windows.Media.Playback.MediaPlayer();

            try
            {
                var tcs = new TaskCompletionSource<bool>();

                track.MediaPlayer.MediaEnded += async (sender, e) =>
                {
                    if (track.IsLooping && !track.CancellationTokenSource.IsCancellationRequested 
                        && track.MediaPlayer != null)
                    {
                        var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(track.FilePath);
                        var mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(storageFile);
                        
                        track.MediaPlayer.Source = mediaSource;
                        track.MediaPlayer.Play();
                    }
                    else
                    {
                        await dispatcher.DispatchAsync(() =>
                        {
                            track.IsPlaying = false;
                            audioTracks.Remove(track.TrackId);
                        });
                        tcs.TrySetResult(true);
                    }
                };

                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(track.FilePath);
                var mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(storageFile);
                track.MediaPlayer.Source = mediaSource;
                track.MediaPlayer.Play();

                await Task.WhenAny(
                    tcs.Task,
                    Task.Delay(Timeout.Infinite, track.CancellationTokenSource.Token)
                );

                if (track.CancellationTokenSource.IsCancellationRequested && track.MediaPlayer != null)
                {
                    track.MediaPlayer.Pause();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PlayWindowsTrackAsync: {ex.Message}");
            }
            finally
            {
                if (track.MediaPlayer != null)
                {
                    track.MediaPlayer.Dispose();
                    track.MediaPlayer = null;
                }
            }
        }

        // Clean up temp files
        private void CleanupTempFiles(bool cleanAll = false)
        {
            var activeFilePaths = cleanAll ? new List<string>() :
                audioTracks.Values.Select(t => t.FilePath).ToList();

            foreach (var filePath in tempFilePaths.ToList())
            {
                if (!cleanAll && activeFilePaths.Contains(filePath))
                {
                    continue;
                }    

                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    tempFilePaths.Remove(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting temp file {filePath}: {ex.Message}");
                }
            }
        }

        public void CleanupAllTempFiles()
        {
            CleanupTempFiles(true);
        }
    }
}