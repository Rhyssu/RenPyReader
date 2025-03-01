namespace RenPyReader.DataModels
{
    public class RenPyAudio(string name, byte[] content)
        : RenPyBinaryBase(name, content)
    {
    }
}
