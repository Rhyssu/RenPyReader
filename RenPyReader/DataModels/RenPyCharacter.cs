namespace RenPyReader.DataModels
{
    internal class RenPyCharacter(string code, string name, string colorHTML)
    {
        internal string Code { get; set; } = code;

        internal string Name { get; set; } = name;

        internal string ColorHTML { get; set; } = colorHTML;

        public override string ToString() => $"{Code} | {Name} | {ColorHTML}";
    }
}
