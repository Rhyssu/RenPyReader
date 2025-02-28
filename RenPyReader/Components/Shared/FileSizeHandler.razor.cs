namespace RenPyReader.Components.Shared
{
    public partial class FileSizeHandler
    {
        // Field to store the maximum size in bytes
        private long? maximumSizeBytes;

        // Field to store the maximum size in megabytes, initialized to 20MB
        private decimal? maximumSizeMegaBytes = 20M;

        // Constant representing the number of bytes in one megabyte
        private const decimal bytesInMegaByte = 1048576.0M;

        // Minimum value for the file size in megabytes
        private const decimal min = 1M;

        // Maximum value for the file size in megabytes
        private const decimal max = 200M;

        // Step value for incrementing / decrementing the file size
        private const double step = 1;

        // Method to get the maximum size in bytes
        public long GetMaximumSizeBytes()
        {
            if (maximumSizeBytes == null)
            {
                return default;
            }

            return (long)maximumSizeBytes;
        }

        // Method to handle changes in the file size
        private void OnSizeChanged(decimal? value)
        {
            if (value.HasValue)
            {
                // Update the maximum size in megabytes and convert to bytes
                maximumSizeMegaBytes = value;
                maximumSizeBytes = (long)(maximumSizeMegaBytes.Value * bytesInMegaByte);
            }
            else
            {
                // Reset the values if the input is null
                maximumSizeMegaBytes = null;
                maximumSizeBytes = null;
            }
        }
    }
}