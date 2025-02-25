namespace RenPyReader.Components.Shared
{
    public partial class FileSizeHandler
    {
        // Field to store the maximum size in bytes
        private long? maximumSizeBytes;

        // Field to store the maximum size in gigabytes, initialized to 0.5 GB
        private decimal? maximumSizeGigaBytes = 0.5M;

        // Constant representing the number of bytes in one gigabyte
        private const decimal bytesInGigaBytes = 1073741824.0M;

        // Minimum value for the file size in gigabytes
        private const decimal min = 0.1M;

        // Maximum value for the file size in gigabytes
        private const decimal max = 16M;

        // Step value for incrementing/decrementing the file size
        private const double step = 0.1;

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
                // Update the maximum size in gigabytes and convert to bytes
                maximumSizeGigaBytes = value;
                maximumSizeBytes = (long)(maximumSizeGigaBytes.Value * bytesInGigaBytes);
            }
            else
            {
                // Reset the values if the input is null
                maximumSizeGigaBytes = null;
                maximumSizeBytes = null;
            }
        }
    }
}