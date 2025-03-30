using System.Globalization;
using System.Text.RegularExpressions;

namespace RenPyReader.Services
{
    public class StringTransformerService : IStringTransformerService
    {
        private readonly Dictionary<string, Func<string, Dictionary<string, string>, string>> _transformationHandlers;

        private static readonly Regex _variableRegex = new(@"\${([^}]*)}", RegexOptions.Compiled);
        
        private static readonly Regex _tagRegex = new(@"\{(\w+)\}(.*?)\{/\1\}", RegexOptions.Compiled | RegexOptions.Singleline);

        public StringTransformerService()
        {
            _transformationHandlers = new Dictionary<string, Func<string, Dictionary<string, string>, string>>
            {
                { "r", (value, _) => value },
                { "s", (value, _) => value },
                { "i", InterpolateRecursively },
                { "q", (value, _) => $"\"{value}\"" },
                { "u", (value, _) => value.ToUpper() },
                { "l", (value, _) => value.ToLower() },
                { "c", (value, _) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value) }
            };
        }

        public string ApplyTransformations(string input, Dictionary<string, string>? variables = null)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            variables ??= new Dictionary<string, string>();
            string result = _variableRegex.Replace(input, match =>
            {
                string transformContent = match.Groups[1].Value;
                return ProcessVariableTransformation(transformContent, variables);
            });

            result = ProcessTagTransformations(result, variables);
            return result;
        }
        
        private string ProcessTagTransformations(string input, Dictionary<string, string> variables)
        {
            string result = input;
            bool madeChange;

            do
            {
                madeChange = false;
                result = _tagRegex.Replace(result, match =>
                {
                    string transform = match.Groups[1].Value.ToLowerInvariant();
                    string content = match.Groups[2].Value;

                    madeChange = true;
                    if (_transformationHandlers.TryGetValue(transform, out var handler))
                    {
                        return handler(content, variables);
                    }

                    return content;
                });
            } while (madeChange);

            return result;
        }

        private string ProcessVariableTransformation(string transformContent, Dictionary<string, string> variables)
        {
            string[] parts = transformContent.Split('|');
            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string content = parts[0].Trim();
            string value = variables.ContainsKey(content) ? variables[content] : content;

            for (int i = 1; i < parts.Length; i++)
            {
                string transform = parts[i].Trim();

                if (transform.Length > 0)
                {
                    char transformType = transform[0];
                    string transformKey = transformType.ToString();

                    if (_transformationHandlers.TryGetValue(transformKey, out var handler))
                    {
                        value = handler(value, variables);
                    }
                }
            }

            return value;
        }

        private string InterpolateRecursively(string value, Dictionary<string, string> variables)
        {
            return ApplyTransformations(value, variables);
        }
    }
}
