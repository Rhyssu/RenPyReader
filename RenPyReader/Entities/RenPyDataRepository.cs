using RenPyReader.DataModels;

namespace RenPyReader.Entities
{
    internal class RenPyDataRepository
    {
        public List<RenPyEvent> Events { get; set; } = new();

        public List<RenPyScene> Scenes { get; set; } = new();

        public List<RenPySound> Sounds { get; set; } = new();

        public List<RenPyMusic> Musics { get; set; } = new();

        public List<RenPyAudio> Audios { get; set; } = new();

        public List<RenPyImage> Images { get; set; } = new();

        public List<RenPyCharacter> Characters { get; set; } = new();
    }
}