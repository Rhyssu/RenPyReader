namespace RenPyReader.Services
{
    public interface IAudioPlayerService
    {
        Task<string> PlayFromByteArrayAsync(byte[] audioData, string fileName, bool loop = false, string? trackId = null);

        Task StopAsync(string? trackId = null);

        Task StopSoundsAsync();

        Task StopAllAsync();

        bool IsTrackPlaying(string trackId);

        bool IsPlaying { get; }
    }
}