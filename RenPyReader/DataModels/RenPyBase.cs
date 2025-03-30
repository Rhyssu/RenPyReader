namespace RenPyReader.DataModels
{
    public abstract class RenPyBase(string name, string parent, uint index)
    {
        public string Name { get; set; } = name;

        public string Parent { get; set; } = parent;

        public uint Index { get; set; } = index;

        public override string ToString() => $"{Parent} | {Name} | {Index}";
    }
}