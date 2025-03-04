using RenPyReader.DataModels;

namespace RenPyReader.Entities
{
    internal class RenPyDataRepository
    {
        #pragma warning disable IDE0028 // Simplify collection initialization

        public List<RenPyEvent> Events { get; set; } = new();

        public List<RenPyScene> Scenes { get; set; } = new();

        public List<RenPySound> Sounds { get; set; } = new();

        public List<RenPyMusic> Musics { get; set; } = new();

        public List<RenPyCharacter> Characters { get; set; } = new();

        #pragma warning restore IDE0028 // Simplify collection initialization

        public void Clear()
        {
            Events.Clear();
            Scenes.Clear();
            Sounds.Clear();
            Musics.Clear();
            Characters.Clear();
        }
    }
}