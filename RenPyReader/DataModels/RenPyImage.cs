namespace RenPyReader.DataModels
{
    public class RenPyImage(string name, byte[] content) 
        : RenPyBinaryBase(name, content)
    {
    }
}
