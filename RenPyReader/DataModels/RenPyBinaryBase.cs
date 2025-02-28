namespace RenPyReader.DataModels
{
    public class RenPyBinaryBase(string name, byte[] content)
    {
        internal string Name { get; set; } = name;

        internal byte[]? Content { get; set; } = content;

        internal string GetBase64()
        {
            if (Content == null)
            {
                return string.Empty;
            }

            var base64 = Convert.ToBase64String(Content);
            return $"data:image/webp;base64,{base64}";
        }
    }
}
