using RenPyReader.DataModels;
using RenPyReader.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using System.IO.Compression;
using Image = SixLabors.ImageSharp.Image;

namespace RenPyReader.DataProcessing
{
    // Class responsible for processing and resizing images
    internal partial class ImageProcessor(LogBuffer logBuffer)
    {
        // Log buffer for logging exceptions and messages
        private readonly LogBuffer _logBuffer = logBuffer;

        // Default width for resizing images
        public int ImageResizeWidth = 960;

        // Default height for resizing images
        public int ImageResizeHeight = 540;

        // Asynchronously processes an image from a ZipArchiveEntry
        public async Task ProcessImageAsync(ZipArchiveEntry entry)
        {
            await using (var entryStream = entry.Open())
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Copy the entry stream to a memory stream
                    await entryStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Resize the image and insert it into the database
                    var newRenPyImage = await ResizeImageAsync(memoryStream, entry.Name);
                    if (newRenPyImage != null)
                    {
                        // await _renPyDBManager.InsertImageAsync(newRenPyImage);
                    }
                }
            }
        }

        // Asynchronously resizes an image and returns a RenPyImage object
        private async Task<RenPyImage?> ResizeImageAsync(MemoryStream memoryStream, string imageName)
        {
            // Detect the image format
            IImageFormat format = Image.DetectFormat(memoryStream);
            RenPyImage? resizedRenPyImage = null;

            if (format != null)
            {
                try
                {
                    memoryStream.Position = 0;
                    // Resize the image and create a new RenPyImage object
                    return new RenPyImage(imageName, await ResizeImage(format, memoryStream));
                }
                catch (Exception ex)
                {
                    // Log any exceptions that occur during resizing
                    _logBuffer.Add($"Exception caught: {ex.Message}.");
                    return null;
                }
            }

            return resizedRenPyImage;
        }

        // Asynchronously resizes an image and returns the resized image as a byte array.
        private async Task<byte[]> ResizeImage(IImageFormat format, MemoryStream memoryStream)
        {
            byte[] content = Array.Empty<byte>();

            using (var image = Image.Load(memoryStream))
            {
                // Resize the image to the specified width and height
                image.Mutate(x => x.Resize(ImageResizeWidth, ImageResizeHeight));
                await using (var outputStream = new MemoryStream())
                {
                    // Save the resized image to the output stream
                    image.Save(outputStream, format);
                    content = outputStream.ToArray();
                }
            }

            return content;
        }
    }
}