namespace RenPyReader.Services
{
    public interface IStringTransformerService
    {
        public string ApplyTransformations(string input, Dictionary<string, string>? variables = null);
    }
}
