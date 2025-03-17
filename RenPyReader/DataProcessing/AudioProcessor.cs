using NAudio.Lame;
using NAudio.Wave;
using RenPyReader.DataModels;
using RenPyReader.Services;
using RenPyReader.Utilities;
using System.IO.Compression;

namespace RenPyReader.DataProcessing
{
    // Class responsible for processing and compressing audio files
    internal partial class AudioProcessor(ISQLiteService sqliteService, LogBuffer logBuffer)
    {
        private readonly ISQLiteService _sqliteService = sqliteService;

        // Log buffer for logging exceptions and messages
        private readonly LogBuffer _logBuffer = logBuffer;

        // Asynchronously processes an audio file from a ZipArchiveEntry
        public async Task ProcessAudioAsync(ZipArchiveEntry entry)
        {
            await using (var entryStream = entry.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Copy the entry stream to a memory stream
                    await entryStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Check the file extension and process accordingly
                    if (entry.FullName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var mp3Stream = new Mp3FileReader(memoryStream))
                        {
                            await ProcessAudioStream(mp3Stream, entry);
                        }
                    }
                    else if (entry.FullName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var wavStream = new WaveFileReader(memoryStream))
                        {
                            await ProcessAudioStream(wavStream, entry);
                        }
                    }
                }
            }
        }

        // Asynchronously processes an audio stream and inserts it into the database
        private async Task ProcessAudioStream(WaveStream waveStream, ZipArchiveEntry entry)
        {
            using (var compressedStream = new MemoryStream())
            {
                // Compress the audio stream to MP3 format
                using (var writer = new LameMP3FileWriter(
                    compressedStream, waveStream.WaveFormat, LAMEPreset.VBR_90))
                {
                    byte[] buffer = new byte[8192]; int bytesRead;
                    while ((bytesRead = waveStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                compressedStream.Position = 0;
                byte[] compressedAudio = compressedStream.ToArray();
                RenPyAudio renPyAudio = new RenPyAudio(entry.Name, compressedAudio);
                // Insert the compressed audio into the database
                await _sqliteService.InsertAudioAsync(renPyAudio);
            }
        }
    }
}