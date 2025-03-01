namespace RenPyReader.Utilities
{
    internal static class DBCommandExtension
    {
        internal static string ToSQLite(this DBCommand command, string baseName)
        {
            return command switch
            {
                DBCommand.CreateRenPyBaseTable          => $"CREATE TABLE IF NOT EXISTS {baseName} (Name TEXT, Parent TEXT, Line INTEGER);",
                DBCommand.InsertRenPyBase               => $"INSERT OR REPLACE INTO {baseName} (Name, Parent, Line) VALUES (@Name, @Parent, @Line);",
                DBCommand.GetRenPyBase                  => $"SELECT * FROM {baseName} WHERE Parent = @Parent AND Line <= @Line ORDER BY Line DESC LIMIT 1;",
                DBCommand.CreateRenPyBinaryBaseTable    => $"CREATE TABLE IF NOT EXISTS {baseName} (Name TEXT PRIMARY KEY, Content BLOB NOT NULL);",
                DBCommand.InsertRenPyBinaryBase         => $"INSERT OR REPLACE INTO {baseName} (Name, Content) VALUES (@Name, @Content);",
                DBCommand.GetRenPyBinaryBase            => $"SELECT * FROM {baseName} WHERE Name LIKE @Name;",
                DBCommand.GetRenPyBinaryBaseNames       => $"SELECT name from {baseName};",
                DBCommand.CreateRenPyCharacterTable     => "CREATE TABLE IF NOT EXISTS characters (Code TEXT PRIMARY KEY, Name TEXT NOT NULL, Color TEXT NOT NULL);",
                DBCommand.InsertRenPyCharacter          => "INSERT OR REPLACE INTO characters (Code, Name, Color) VALUES (@Code, @Name, @Color);",
                DBCommand.GetRenPyCharacter             => "SELECT * FROM characters WHERE Code = @Code;",
                _                                       => throw new ArgumentException("Invalid DBCommand", nameof(command))
            };
        }
    }
}
