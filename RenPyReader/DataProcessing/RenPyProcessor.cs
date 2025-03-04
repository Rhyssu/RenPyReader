using RenPyReader.DataModels;
using RenPyReader.Entities;
using System.Text.RegularExpressions;

namespace RenPyReader.DataProcessing
{
    internal class RenPyProcessor
    {
        // Dictionary mapping a condition delegate to its processor info.
        private readonly Dictionary<Func<string, bool>, ProcessorInfo> _processors;

        public delegate T ProcessLineDelegate<T>(LineContext context);

        // Precompiled regex for character definitions.
        private static readonly Regex CharacterRegex = new Regex(@"^define\s+(\w+)\s*=\s*Character\(""([^""]+)""(?:[^)]*?color\s*=\s*""([^""]+)"")?", RegexOptions.Compiled);

        public RenPyProcessor()
        {
            _processors = new Dictionary<Func<string, bool>, ProcessorInfo>();

            AddProcessor(
                line => line.StartsWith("label", StringComparison.OrdinalIgnoreCase),
                ProcessEvent,
                typeof(RenPyEvent)
            );

            AddProcessor(
                line => line.StartsWith("scene", StringComparison.OrdinalIgnoreCase),
                ProcessScene,
                typeof(RenPyScene)
            );

            AddProcessor(
                line => line.StartsWith("play sound", StringComparison.OrdinalIgnoreCase),
                ProcessSound,
                typeof(RenPySound)
            );

            AddProcessor(
                line => line.StartsWith("play music", StringComparison.OrdinalIgnoreCase),
                ProcessMusic,
                typeof(RenPyMusic)
            );

            AddProcessor(
                line => line.StartsWith("define", StringComparison.OrdinalIgnoreCase),
                ProcessCharacterDefinition,
                typeof(RenPyCharacter)
            );
        }

        // Adds a processor for a specific type T.
        private void AddProcessor<T>(Func<string, bool> condition, Func<LineContext, Task<T>> processor, Type resultType)
        {
            _processors.Add(condition, new ProcessorInfo
            {
                Condition = condition,
                Processor = async context =>
                    (object?)await processor(context) ?? null,
                ResultType = resultType
            });
        }

        // Processes a line asynchronously using the first matching processor.
        public async Task<object?> ProcessLineAsync(LineContext context)
        {
            foreach (var processorInfo in _processors)
            {
                if (processorInfo.Key(context.Content))
                {
                    return await processorInfo.Value.Processor(context);
                }
            }

            return null;
        }

        // Processes a "label" line into a RenPyEvent.
        private Task<RenPyEvent> ProcessEvent(LineContext context)
        {
            if (string.IsNullOrEmpty(context.Content))
            {
                return Task.FromResult(new RenPyEvent("", "", 0));
            }

            string remainder = context.Content.Substring("label".Length);
            int colonIndex = remainder.IndexOf(':');
            if (colonIndex == -1)
            {
                return Task.FromResult(new RenPyEvent("", "", 0));
            }

            string eventName = remainder.Substring(0, colonIndex).Trim();
            return Task.FromResult(new RenPyEvent(eventName, context.Parent, context.Index));
        }

        // Processes a "scene" line into a RenPyScene.
        private Task<RenPyScene> ProcessScene(LineContext context)
        {
            if (string.IsNullOrEmpty(context.Content))
            {
                return Task.FromResult(new RenPyScene("", "", 0));
            }

            int firstSpaceIndex = context.Content.IndexOf(' ');
            if (firstSpaceIndex == -1)
            {
                return Task.FromResult(new RenPyScene("", "", 0));
            }

            string sceneName;
            int secondSpaceIndex = context.Content.IndexOf(' ', firstSpaceIndex + 1);
            sceneName = secondSpaceIndex == -1
                ? context.Content.Substring(firstSpaceIndex + 1).Trim()
                : context.Content.Substring(firstSpaceIndex + 1, secondSpaceIndex - firstSpaceIndex - 1).Trim();

            return Task.FromResult(new RenPyScene(sceneName, context.Parent, context.Index));
        }

        // Processes a "play sound" line into a RenPySound.
        private Task<RenPySound> ProcessSound(LineContext context)
        {
            if (string.IsNullOrEmpty(context.Content))
            {
                return Task.FromResult(new RenPySound("", "", 0));
            }

            int startQuoteIndex = context.Content.IndexOf('"');
            if (startQuoteIndex < 0)
            {
                return Task.FromResult(new RenPySound("", "", 0));
            }

            int endQuoteIndex = context.Content.IndexOf('"', startQuoteIndex + 1);
            if (endQuoteIndex < 0)
            {
                return Task.FromResult(new RenPySound("", "", 0));
            }

            string soundName = context.Content.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
            return Task.FromResult(new RenPySound(soundName, context.Parent, context.Index));
        }

        // Processes a "play music" line into a RenPyMusic.
        private Task<RenPyMusic> ProcessMusic(LineContext context)
        {
            if (string.IsNullOrEmpty(context.Content))
            {
                return Task.FromResult(new RenPyMusic("", "", 0));
            }

            int startQuoteIndex = context.Content.IndexOf('"');
            if (startQuoteIndex < 0)
            {
                return Task.FromResult(new RenPyMusic("", "", 0));
            }

            int endQuoteIndex = context.Content.IndexOf('"', startQuoteIndex + 1);
            if (endQuoteIndex < 0)
            {
                return Task.FromResult(new RenPyMusic("", "", 0));
            }

            string musicName = context.Content.Substring(startQuoteIndex + 1, endQuoteIndex - startQuoteIndex - 1);
            return Task.FromResult(new RenPyMusic(musicName, context.Parent, context.Index));
        }

        // Processes a character definition line into a RenPyCharacter.
        private Task<RenPyCharacter> ProcessCharacterDefinition(LineContext context)
        {
            if (string.IsNullOrEmpty(context.Content))
            {
                return Task.FromResult(new RenPyCharacter("", "", ""));
            }

            var match = CharacterRegex.Match(context.Content);
            if (match.Success)
            {
                string codeName = match.Groups[1].Value;
                string charName = match.Groups[2].Value;
                string charColor = match.Groups[3].Success ? match.Groups[3].Value : "Default";
                if (!string.IsNullOrEmpty(codeName) && !string.IsNullOrEmpty(charName))
                    return Task.FromResult(new RenPyCharacter(codeName, charName, charColor));
            }

            return Task.FromResult(new RenPyCharacter("", "", ""));
        }

        // Tries to parse a RenPy variable line into its components.
        internal static bool TryParseRenpyVariableLine(string line, out string variableName, out string operation, out string value)
        {
            variableName = operation = value = string.Empty;
            string trimmed = line.TrimStart().Substring(1).Trim();
            string pattern = @"^(?<var>[A-Za-z_]\w*)\s*(?<op>\+=|-=|\*=|/=|%=|=)\s*(?<value>.+)$";

            var match = Regex.Match(trimmed, pattern);
            if (!match.Success)
            {
                return false;
            }

            variableName = match.Groups["var"].Value;
            operation = match.Groups["op"].Value;
            value = match.Groups["value"].Value.Trim();

            return true;
        }
    }

    internal class ProcessorInfo
    {
        // Condition to determine if a processor should be used.
        public required Func<string, bool> Condition { get; set; }

        // Asynchronous processor function.
        public required Func<LineContext, Task<object?>> Processor { get; set; }

        // Expected result type.
        public required Type ResultType { get; set; }
    }

}