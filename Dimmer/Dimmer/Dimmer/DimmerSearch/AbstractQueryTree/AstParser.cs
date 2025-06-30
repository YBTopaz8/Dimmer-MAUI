using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;

public class AstParser
{
    private readonly List<Token> _tokens;
    private int _position = 0;

    public AstParser(string query)
    {
        _tokens = Lexer.Tokenize(query);
    }

    public IQueryNode Parse()
    {
        var result = ParseExpression();
        if (!IsAtEnd())
        {
            // This indicates there were leftover tokens that couldn't be parsed.
            // You can log this or throw a more specific error.
            throw new Exception($"Syntax error: Unexpected token '{Peek().Text}' at position {Peek().Position}.");
        }
        return result;
    }

    // expression -> term (OR term)*
    private IQueryNode ParseExpression()
    {
        var left = ParseTerm();
        while (Match(TokenType.Or, TokenType.Pipe))
        {
            left = new LogicalNode(left, LogicalOperator.Or, ParseTerm());
        }
        return left;
    }

    // term -> factor (AND? factor)*
    private IQueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (!IsAtEnd() && Peek().Type != TokenType.Or && Peek().Type != TokenType.Pipe && Peek().Type != TokenType.RightParen)
        {
            // Implicit AND
            Match(TokenType.And); // Consume optional AND keyword
            left = new LogicalNode(left, LogicalOperator.And, ParseFactor());
        }
        return left;
    }

    // factor -> (NOT factor) | (LPAREN expression RPAREN) | primary_clause
    private IQueryNode ParseFactor()
    {
        if (Match(TokenType.Not, TokenType.Bang))
        {
            return new NotNode(ParseFactor());
        }

        if (Match(TokenType.LeftParen))
        {
            var expression = ParseExpression();
            Consume(TokenType.RightParen, $"Expected ')' after expression, but found '{Peek().Text}'.");
            return expression;
        }

        return ParseClause();
    }

    // clause -> (IDENTIFIER COLON)? (OPERATOR? (VALUE | STRING | NUMBER) (- VALUE)?)
    private IQueryNode ParseClause()
    {
        string field = "Title"; // Default search field

        // Check for an explicit field (e.g., "artist:")
        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            field = Consume(TokenType.Identifier).Text;
            Consume(TokenType.Colon);
        }

        // Determine the operator and value
        string op = "="; // Default operator
        object value;
        object? upperValue = null;

        var currentToken = Peek();
        // Check for prefix operators
        if (IsOperator(currentToken.Type))
        {
            op = Consume(currentToken.Type).Text;
        }

        var valueToken = Peek();
        if (valueToken.Type == TokenType.Identifier || valueToken.Type == TokenType.StringLiteral || valueToken.Type == TokenType.Number)
        {
            value = Consume(valueToken.Type).Text;
        }
        else
        {
            throw new Exception($"Expected a value but found '{valueToken.Text}' at position {valueToken.Position}.");
        }

        // Check for a range operator
        if (Match(TokenType.Minus))
        {
            op = "-"; // Override operator to be range
            var upperValueToken = Peek();
            if (upperValueToken.Type == TokenType.Identifier || upperValueToken.Type == TokenType.Number)
            {
                upperValue = Consume(upperValueToken.Type).Text;
            }
            else
            {
                throw new Exception($"Expected a value for range upper bound but found '{upperValueToken.Text}'.");
            }
        }

        return new ClauseNode(field, op, value, upperValue);
    }

    // --- Parser Helper Methods ---
    private Token Peek(int offset = 0) => _position + offset >= _tokens.Count ? _tokens.Last() : _tokens[_position + offset];
    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;
    private Token Consume(TokenType type, string message) => Peek().Type == type ? _tokens[_position++] : throw new Exception(message);
    private Token Consume(TokenType type) => Consume(type, $"Expected {type} but got {Peek().Type}.");
    private bool Match(params TokenType[] types)
    {
        if (types.Any(t => Peek().Type == t))
        {
            _position++;
            return true;
        }
        return false;
    }
    private bool IsOperator(TokenType type) => type switch
    {
        TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterThanOrEqual or
        TokenType.LessThanOrEqual or TokenType.Equals or TokenType.Tilde or
        TokenType.Caret or TokenType.Dollar => true,
        _ => false
    };
}