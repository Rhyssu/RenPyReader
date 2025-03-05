using System.Collections;

namespace RenPyReader.Utilities
{
    internal class OrderedSet<T> : IEnumerable<T>
    {
        private HashSet<T> _set = new();

        private List<T> _list = new();

        public int Add(T item)
        {
            if (_set.Add(item))
            {
                _list.Add(item);
            }

            return _list.IndexOf(item);
        }

        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                _list.Remove(item);
                return true;
            }

            return false;
        }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public void Clear()
        {
            _set.Clear();
            _list.Clear();
        }

        public int Count => _set.Count;

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}