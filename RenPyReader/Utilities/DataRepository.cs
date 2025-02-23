using Microsoft.Data.Sqlite;
using RenPyReader.DataModels;

namespace RenPyReader.Utilities
{
    internal class DataRepository
    {
        internal List<RenPyEvent> Events { get; } = [];

        internal List<RenPyScene> Scenes { get; } = [];

        internal List<RenPySound> Sounds { get; } = [];

        internal List<RenPyMusic> Musics { get; } = [];
    }
}