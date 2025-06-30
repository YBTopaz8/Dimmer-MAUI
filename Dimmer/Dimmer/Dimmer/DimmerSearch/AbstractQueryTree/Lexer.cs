using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;

public enum TokenType
{
    // Single-character tokens
    Colon, Bang, Pipe, LeftParen, RightParen, GreaterThan, LessThan,
    Equals, Tilde, Caret, Dollar, Minus,

    // Two-character tokens
    GreaterThanOrEqual, LessThanOrEqual,

    // Literals
    Identifier, StringLiteral, Number,

    // Keywords for logic and directives.
    // NOTE: For the AST, we will primarily focus on logical keywords.
    // Directives (like include, first, etc.) are handled by a meta-parser.
    And, Or, Not,

    // Control Token
    EndOfFile,
    Error
}

public record Token(TokenType Type, string Text, int Position);

public static class Lexer
{
    private static readonly Dictionary<string, TokenType> _keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "and", TokenType.And },
        { "or", TokenType.Or },
        { "not", TokenType.Not }
        // We no longer need to define directives like 'include' or 'first' here,
        // as they are handled at a higher level (meta-parsing).
    };

    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int position = 0;

        while (position < text.Length)
        {
            char current = text[position];

            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            if (char.IsLetter(current))
            {
                int start = position;
                while (position < text.Length && (char.IsLetterOrDigit(text[position]) || text[position] == '_'))
                {
                    position++;
                }
                string word = text.Substring(start, position - start);
                if (_keywords.TryGetValue(word, out TokenType keywordType))
                {
                    tokens.Add(new Token(keywordType, word, start));
                }
                else
                {
                    tokens.Add(new Token(TokenType.Identifier, word, start));
                }
                continue;
            }

            if (char.IsDigit(current) || (current == '.' && position + 1 < text.Length && char.IsDigit(text[position + 1])))
            {
                int start = position;
                // A more robust number parser that handles decimals and time formats
                while (position < text.Length && (char.IsDigit(text[position]) || text[position] == '.' || text[position] == ':'))
                {
                    position++;
                }
                string number = text.Substring(start, position - start);
                tokens.Add(new Token(TokenType.Number, number, start));
                continue;
            }

            if (current == '"')
            {
                int start = position;
                position++; // Skip opening quote
                var sb = new StringBuilder();
                while (position < text.Length && text[position] != '"')
                {
                    sb.Append(text[position]);
                    position++;
                }

                if (position < text.Length)
                {
                    position++; // Skip closing quote
                }
                tokens.Add(new Token(TokenType.StringLiteral, sb.ToString(), start));
                continue;
            }

            if (TryMatchTwoCharOperator(text, ref position, tokens))
                continue;
            if (TryMatchOneCharOperator(text, ref position, tokens))
                continue;

            // If we get here, it's an unrecognized character.
            tokens.Add(new Token(TokenType.Error, text[position].ToString(), position));
            position++;
        }

        tokens.Add(new Token(TokenType.EndOfFile, string.Empty, position));
        return tokens;
    }

    private static bool TryMatchTwoCharOperator(string text, ref int position, List<Token> tokens)
    {
        if (position + 1 < text.Length)
        {
            string op = text.Substring(position, 2);
            TokenType? type = op switch
            {
                ">=" => TokenType.GreaterThanOrEqual,
                "<=" => TokenType.LessThanOrEqual,
                _ => null
            };

            if (type.HasValue)
            {
                tokens.Add(new Token(type.Value, op, position));
                position += 2;
                return true;
            }
        }
        return false;
    }

    private static bool TryMatchOneCharOperator(string text, ref int position, List<Token> tokens)
    {
        char op = text[position];
        TokenType? type = op switch
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
            _ => null
        };

        if (type.HasValue)
        {
            tokens.Add(new Token(type.Value, op.ToString(), position));
            position++;
            return true;
        }
        return false;
    }
}