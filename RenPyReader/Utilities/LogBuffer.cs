namespace RenPyReader.Utilities
{
    internal class LogBuffer(int size)
    {
        private readonly List<string> _buffer = [.. new string[size]];

        private int _currentIndex = 0;

        public void Add(string message)
        {
            _buffer[_currentIndex] = $"{DateTime.Now:HH:mm:ss.fff} : {message}";
            _currentIndex = (_currentIndex + 1) % size;
        }

        public IEnumerable<string> GetMessages()
        {
            for (int i = 0; i < size; i++)
            {
                int index = (_currentIndex + i) % size;

                if (_buffer[index] != null)
                {
                    yield return _buffer[index];
                }
            }
        }
    }
}