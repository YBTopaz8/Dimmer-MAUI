using Dimmer.DimmerSearch.Exceptions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dimmer.DimmerSearch.TQL;

public class AstParser
{
    private readonly List<Token> _tokens;
    private int _position = 0;

    public AstParser(List<Token> tokens)
    {
        _tokens = [.. tokens];
        if (_tokens.Count == 0 || _tokens.Last().Type != TokenType.EndOfFile)
        {
            _tokens.Add(new Token(TokenType.EndOfFile, "", -1));
        }
    }

    public AstParser(string filterQuery) : this(Lexer.Tokenize(filterQuery)) { }

    public IQueryNode Parse()
    {
        if (_tokens.All(t => t.Type == TokenType.EndOfFile))
            return new ClauseNode("any", "matchall", "");

        var result = ParseExpression();

        if (!IsAtEnd())
            throw new ParsingException($"Syntax error: Unexpected token '{Peek().Text}' after valid expression.", Peek().Position);

        return result;
    }

    // Lowest precedence: OR
    private IQueryNode ParseExpression()
    {
        var left = ParseTerm();
        while (Match(TokenType.Or, TokenType.Pipe))
        {
            left = new LogicalNode(left, LogicalOperator.Or, ParseTerm());
        }
        return left;
    }

    // Higher precedence: AND (both explicit and implicit)
    private IQueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (!IsAtEnd() && IsImplicitAnd())
        {
            Match(TokenType.And); // Consume optional "and" keyword
            left = new LogicalNode(left, LogicalOperator.And, ParseFactor());
        }
        return left;
    }

    // Highest precedence: NOT, parentheses, and individual clauses
    private IQueryNode ParseFactor()
    {
        if (Match(TokenType.Not, TokenType.Bang))
            return new NotNode(ParseFactor());

        if (Match(TokenType.LeftParen))
        {
            var expression = ParseExpression();
            Consume(TokenType.RightParen, "Expected ')' after expression.");
            return expression;
        }

        return ParseClause();
    }

    // A single unit like `field:value` or just `value`
    private IQueryNode ParseClause()
    {
        string field = "any";

        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            field = Consume(TokenType.Identifier).Text;
            Consume(TokenType.Colon);
        }

        string op = "contains";
        if (IsOperator(Peek().Type))
        {
            op = Consume(Peek().Type).Text;
        }

        bool isNegated = Match(TokenType.Not, TokenType.Bang);

        if (!IsValueToken(Peek().Type))
            throw new ParsingException($"Expected a value for field '{field}' but found '{Peek().Text}'.", Peek().Position);

        var valueToken = Consume(Peek().Type);

        if (Match(TokenType.Minus))
        {
            if (IsValueToken(Peek().Type))
            {
                var upperValueToken = Consume(Peek().Type);
                return new ClauseNode(field, "-", valueToken.Text, upperValueToken.Text, isNegated);
            }
        }

        return new ClauseNode(field, op, valueToken.Text, isNegated);
    }

    // --- Helper Functions ---
    private bool IsImplicitAnd()
    {
        if (IsAtEnd())
            return false;
        var type = Peek().Type;
        // It's an implicit AND if the next token is a value, NOT, or parenthesis.
        // It is NOT an implicit AND if it's a boundary like OR, ), etc.
        return type switch
        {
            TokenType.Or or TokenType.Pipe or TokenType.RightParen => false,
            _ => true
        };
    }

    private Token Peek(int offset = 0) => _position + offset >= _tokens.Count ? _tokens.Last() : _tokens[_position + offset];
    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;
    private Token Consume(TokenType type, string message) => Peek().Type == type ? _tokens[_position++] : throw new ParsingException(message, Peek().Position);
    private Token Consume(TokenType type) => Consume(type, $"Expected {type} but got {Peek().Type}.");
    private bool Match(params TokenType[] types)
    {
        if (IsAtEnd() || !types.Contains(Peek().Type))
            return false;
        _position++;
        return true;
    }

    private static bool IsOperator(TokenType type) => type is TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterThanOrEqual or TokenType.LessThanOrEqual or TokenType.Equals or TokenType.Tilde or TokenType.Caret or TokenType.Dollar;
    private static bool IsValueToken(TokenType type) => type is TokenType.Identifier or TokenType.Number or TokenType.StringLiteral;
}