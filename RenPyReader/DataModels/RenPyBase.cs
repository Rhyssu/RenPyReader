namespace RenPyReader.DataModels
{
    internal abstract class RenPyBase(string name, string parent, uint index)
    {
        internal string Name { get; set; } = name;

        internal string Parent { get; set; } = parent;

        internal uint Index { get; set; } = index;

        public override string ToString() => $"{Parent} | {Name} | {Index}";
    }
}