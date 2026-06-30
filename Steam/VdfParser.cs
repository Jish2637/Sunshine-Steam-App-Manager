using System.Text;

namespace SunshineSteamAppManager.Steam;

public sealed class VdfParseException : Exception
{
    public VdfParseException(string message)
        : base(message)
    {
    }
}

public static class VdfParser
{
    public static async Task<VdfNode> ParseFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return Parse(text);
    }

    public static VdfNode Parse(string text)
    {
        var tokens = Tokenize(text);
        var parser = new Parser(tokens);
        return parser.ParseDocument();
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var index = 0;

        while (index < text.Length)
        {
            SkipWhitespaceAndComments(text, ref index);
            if (index >= text.Length)
            {
                break;
            }

            var ch = text[index];
            if (ch == '{' || ch == '}')
            {
                tokens.Add(ch.ToString());
                index++;
                continue;
            }

            if (ch == '"')
            {
                tokens.Add(ReadQuoted(text, ref index));
                continue;
            }

            tokens.Add(ReadBare(text, ref index));
        }

        return tokens;
    }

    private static void SkipWhitespaceAndComments(string text, ref int index)
    {
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '/')
            {
                index += 2;
                while (index < text.Length && text[index] != '\r' && text[index] != '\n')
                {
                    index++;
                }
                continue;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                index += 2;
                while (index + 1 < text.Length && !(text[index] == '*' && text[index + 1] == '/'))
                {
                    index++;
                }

                if (index + 1 < text.Length)
                {
                    index += 2;
                }
                continue;
            }

            break;
        }
    }

    private static string ReadQuoted(string text, ref int index)
    {
        var builder = new StringBuilder();
        index++;

        while (index < text.Length)
        {
            var ch = text[index++];
            if (ch == '"')
            {
                return builder.ToString();
            }

            if (ch == '\\' && index < text.Length)
            {
                var escaped = text[index++];
                switch (escaped)
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case '"':
                        builder.Append('"');
                        break;
                    case '\\':
                        builder.Append('\\');
                        break;
                    default:
                        builder.Append('\\');
                        builder.Append(escaped);
                        break;
                }

                continue;
            }

            builder.Append(ch);
        }

        throw new VdfParseException("Unterminated quoted string in VDF file.");
    }

    private static string ReadBare(string text, ref int index)
    {
        var start = index;
        while (index < text.Length && !char.IsWhiteSpace(text[index]) && text[index] != '{' && text[index] != '}')
        {
            index++;
        }

        return text[start..index];
    }

    private sealed class Parser
    {
        private readonly List<string> _tokens;
        private int _index;

        public Parser(List<string> tokens)
        {
            _tokens = tokens;
        }

        public VdfNode ParseDocument()
        {
            var node = new VdfNode();
            while (!End)
            {
                var key = NextValueToken("Expected key.");
                if (End)
                {
                    throw new VdfParseException($"Missing value for key '{key}'.");
                }

                node.Set(key, ParseValue());
            }

            return node;
        }

        private VdfNode ParseObject()
        {
            var node = new VdfNode();
            Expect("{");

            while (!End && Peek() != "}")
            {
                var key = NextValueToken("Expected object key.");
                if (End)
                {
                    throw new VdfParseException($"Missing value for key '{key}'.");
                }

                node.Set(key, ParseValue());
            }

            Expect("}");
            return node;
        }

        private VdfValue ParseValue()
        {
            if (Peek() == "{")
            {
                return VdfValue.FromObject(ParseObject());
            }

            return VdfValue.FromString(NextValueToken("Expected value."));
        }

        private string NextValueToken(string errorMessage)
        {
            if (End)
            {
                throw new VdfParseException(errorMessage);
            }

            var token = _tokens[_index++];
            if (token == "{" || token == "}")
            {
                throw new VdfParseException(errorMessage);
            }

            return token;
        }

        private void Expect(string expected)
        {
            if (End || _tokens[_index] != expected)
            {
                throw new VdfParseException($"Expected '{expected}'.");
            }

            _index++;
        }

        private string? Peek() => End ? null : _tokens[_index];
        private bool End => _index >= _tokens.Count;
    }
}
