using RenPyReader.Components.Shared;
using RenPyReader.DataModels;
using RenPyReader.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.IO.Compression;
using Image = SixLabors.ImageSharp.Image;

namespace RenPyReader.DataProcessing
{
    internal partial class ImageProcessor(DatabaseHandler databaseHandler, LogBuffer logBuffer)
    {
        private readonly DatabaseHandler _databaseHandler = databaseHandler;

        private readonly LogBuffer _logBuffer = logBuffer;

        public int ImageResizeWidth = 960;

        public int ImageResizeHeight = 540;

        public async Task ProcessImageAsync(ZipArchiveEntry entry)
        {
            await using (var entryStream = entry.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    await entryStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    var newRenPyImage = await ResizeImageAsync(memoryStream, entry.Name);
                    if (newRenPyImage != null)
                    {
                        await _databaseHandler!.InsertImageAsync(newRenPyImage);
                    }
                }
            }
        }

        private async Task<RenPyImage?> ResizeImageAsync(MemoryStream memoryStream, string imageName)
        {
            IImageFormat format = Image.DetectFormat(memoryStream);
            RenPyImage? resizedRenPyImage = null;

            if (format != null)
            {
                try
                {
                    memoryStream.Position = 0;
                    return new RenPyImage(imageName, await ResizeImage(format, memoryStream));
                }
                catch (Exception ex)
                {
                    _logBuffer.Add($"Exception caught: {ex.Message}.");
                    return null;
                }
            }

            return resizedRenPyImage;
        }

        private async Task<byte[]> ResizeImage(IImageFormat format, MemoryStream memoryStream)
        {
            byte[] content = Array.Empty<byte>();

            using (var image = Image.Load(memoryStream))
            {
                image.Mutate(x => x.Resize(ImageResizeWidth, ImageResizeHeight));
                await using (var outputStream = new MemoryStream())
                {
                    image.Save(outputStream, format);
                    content = outputStream.ToArray();
                }
            }

            return content;
        }
    }
}