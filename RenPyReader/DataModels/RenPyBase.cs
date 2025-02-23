namespace RenPyReader.DataModels
{
    internal abstract class RenPyBase(string name, string parent, uint index)
    {
        internal string Name { get; set; } = name;

        internal string Parent { get; set; } = parent;

        internal uint Index { get; set; } = index;

        internal byte[]? Content { get; set; }

        internal string GetBase64()
        {
            if (Content == null)
            {
                return string.Empty;
            }

            var base64 = Convert.ToBase64String(Content);
            return $"data:image/webp;base64,{base64}";
        }

        public override string ToString() => $"{Parent} | {Name} | {Index}";
    }
}