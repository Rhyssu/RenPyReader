using RenPyReader.DataModels;
using System.Text.RegularExpressions;

namespace RenPyReader.Utilities
{
    internal static partial class RegexProcessor
    {
        [GeneratedRegex("label\\s+(\\w+)", RegexOptions.Compiled)]
        private static partial Regex LabelRegex();

        internal static string ExtractLabel(string input)
        { 
            var match = LabelRegex().Match(input);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        [GeneratedRegex("scene\\s+(\\w+)", RegexOptions.Compiled)]
        private static partial Regex SceneRegex();

        internal static string ExtractScene(string input)
        {
            var match = SceneRegex().Match(input);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        [GeneratedRegex("play sound\\s+\"([^\"]+)\"", RegexOptions.Compiled)]
        private static partial Regex SoundRegex();

        internal static string ExtractSound(string input)
        {
            var match = SoundRegex().Match(input);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        [GeneratedRegex("play music\\s+\"([^\"]+)\"", RegexOptions.Compiled)]
        private static partial Regex MusicRegex();

        internal static string ExtractMusic(string input)
        {
            var match = MusicRegex().Match(input);
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        [GeneratedRegex(@"^define\s+(\w+)\s*=\s*Character\(""([^""]+)""(?:[^)]*?color\s*=\s*""([^""]+)"")?", RegexOptions.Compiled)]
        private static partial Regex CharacterRegex();

        internal static RenPyCharacter ExtractCharacter(string input)
        {
            var match = CharacterRegex().Match(input);
            if (match.Success)
            {
                string codeName     = match.Groups[1].Value;
                string charName     = match.Groups[2].Value;
                string charColor    = match.Groups[3].Success ? match.Groups[3].Value : "";
                if (!string.IsNullOrEmpty(codeName) && !string.IsNullOrEmpty(charName))
                {
                    return new RenPyCharacter(codeName, charName, charColor);
                }
            }

            return new RenPyCharacter("", "", "");
        }

        [GeneratedRegex(@"^""|(?<!\b\w+\s)\b\w+\b""", RegexOptions.Compiled)]
        private static partial Regex DialogueRegex();

        internal static bool IsDialogue(string input)
        {
            var match = DialogueRegex().Match(input);
            return match.Success;
        }
    }
}
