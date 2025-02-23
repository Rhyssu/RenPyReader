using RenPyReader.Entities;

namespace RenPyReader.DataModels
{
    internal class RenPyStatement(string name, 
        string operation, string value, string parent)
    {
        internal StatementType Type { get; set; }

        internal string Name { get; set; } = name;

        internal string Operation { get; set; } = operation;

        internal string Value { get; set; } = value;

        internal string Parent { get; set; } = parent;

        internal uint Index { get; set; }

        internal uint Indent { get; set; }
    }
}
