using Windows.Media.Playback;

namespace RenPyReader.DataModels
{
    public class AudioTrack
    {
        public string TrackId { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public bool IsPlaying { get; set; }

        public bool IsLooping { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        
        public MediaPlayer? MediaPlayer { get; set; }
    }
}
