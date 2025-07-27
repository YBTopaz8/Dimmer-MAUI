namespace Dimmer.DimmerSearch.TQL;

public enum TokenType
{
    // Single-character tokens
    Colon, Bang, Pipe, LeftParen, RightParen, GreaterThan, LessThan,
    Equals, Tilde, Caret, Dollar, Minus,

    // Two-character tokens
    GreaterThanOrEqual, LessThanOrEqual,

    // Literals
    Identifier, StringLiteral, Number,

    // Keywords
    And, Or, Not,
    Include, Add, Exclude, Remove,
    Asc, Desc, Random, Shuffle,
    First, Last,

    // Control Tokens
    EndOfFile, Error
}

public record Token(TokenType Type, string Text, int Position);

public static class Lexer
{
    private static readonly Dictionary<string, TokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "and", TokenType.And }, { "or", TokenType.Or }, { "not", TokenType.Not },
        { "include", TokenType.Include }, { "add", TokenType.Add }, { "plus", TokenType.Add },
        { "exclude", TokenType.Exclude }, { "remove", TokenType.Remove }, { "minus", TokenType.Remove },
        { "asc", TokenType.Asc }, { "desc", TokenType.Desc },
        { "random", TokenType.Random }, { "shuffle", TokenType.Shuffle },
        { "first", TokenType.First }, { "last", TokenType.Last }
    };

    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int position = 0;

        while (position < text.Length)
        {
            char current = text[position];

            if (char.IsWhiteSpace(current))
            { position++; continue; }

            if (current == '"')
            {
                int start = position;
                position++; // Skip opening quote
                var sb = new StringBuilder();
                while (position < text.Length && text[position] != '"')
                {
                    if (text[position] == '\\' && position + 1 < text.Length && text[position + 1] == '"')
                    {
                        sb.Append('"');
                        position += 2;
                    }
                    else
                    { sb.Append(text[position++]); }
                }
                if (position < text.Length)
                    position++; // Skip closing quote
                tokens.Add(new Token(TokenType.StringLiteral, sb.ToString(), start));
                continue;
            }

            if (TryMatchOperator(text, ref position, tokens))
                continue;

            if (char.IsLetter(current))
            {
                int start = position;
                while (position < text.Length && char.IsLetterOrDigit(text[position]))
                { position++; }
                string word = text.Substring(start, position - start);
                if (_keywords.TryGetValue(word, out var keywordType))
                    tokens.Add(new Token(keywordType, word, start));
                else
                    tokens.Add(new Token(TokenType.Identifier, word, start));
                continue;
            }

            if (char.IsDigit(current))
            {
                int start = position;
                while (position < text.Length && (char.IsDigit(text[position]) || text[position] == '.' || text[position] == ':'))
                { position++; }
                string number = text.Substring(start, position - start);
                tokens.Add(new Token(TokenType.Number, number, start));
                continue;
            }

            tokens.Add(new Token(TokenType.Error, current.ToString(), position++));
        }
        tokens.Add(new Token(TokenType.EndOfFile, string.Empty, position));
        return tokens;
    }

    private static bool TryMatchOperator(string text, ref int position, List<Token> tokens)
    {
        if (position + 1 < text.Length)
        {
            string op = text.Substring(position, 2);
            if (op == ">=" || op == "<=")
            {
                tokens.Add(new Token(op == ">=" ? TokenType.GreaterThanOrEqual : TokenType.LessThanOrEqual, op, position));
                position += 2;
                return true;
            }
        }

        char c = text[position];
        var type = c switch
        {
            ':' => TokenType.Colon,
            '!' => TokenType.Bang,
            '|' => TokenType.Or,
            '(' => TokenType.LeftParen,
            ')' => TokenType.RightParen,
            '>' => TokenType.GreaterThan,
            '<' => TokenType.LessThan,
            '=' => TokenType.Equals,
            '~' => TokenType.Tilde,
            '^' => TokenType.Caret,
            '$' => TokenType.Dollar,
            '-' => TokenType.Minus,
            _ => (TokenType?)null
        };

        if (type.HasValue)
        {
            tokens.Add(new Token(type.Value, c.ToString(), position));
            position++;
            return true;
        }
        return false;
    }
}