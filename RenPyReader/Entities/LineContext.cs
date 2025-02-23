namespace RenPyReader.Entities
{
    internal class LineContext(string parent, string content, uint line)
    {
        internal string Content { get; set; } = content;
        
        internal string Parent { get; set; } = parent;

        internal uint Index { get; set; } = line;

        public static LineContext Default => new("", "", 0);

        public override string ToString() => $"{Parent} | {Content.Length} | {Index}";
    }
}