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

    public AstParser(string filterQuery)
    {
        _tokens = Lexer.Tokenize(filterQuery);
    }

    public IQueryNode Parse()
    {
        if (_tokens.All(t => t.Type == TokenType.EndOfFile))
            return new ClauseNode("Title", "contains", "");
        var result = ParseExpression();
        if (!IsAtEnd())
            throw new Exception($"Syntax error: Unexpected token '{Peek().Text}' at position {Peek().Position}.");
        return result;
    }

    private IQueryNode ParseExpression()
    {
        var left = ParseTerm();
        while (Match(TokenType.Or, TokenType.Pipe))
            left = new LogicalNode(left, LogicalOperator.Or, ParseTerm());
        return left;
    }

    private IQueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (!IsAtEnd() && Peek().Type != TokenType.Or && Peek().Type != TokenType.Pipe && Peek().Type != TokenType.RightParen)
        {
            Match(TokenType.And);
            left = new LogicalNode(left, LogicalOperator.And, ParseFactor());
        }
        return left;
    }

    private IQueryNode ParseFactor()
    {
        if (Match(TokenType.Not, TokenType.Bang))
            return new NotNode(ParseFactor());

        if (Match(TokenType.LeftParen))
        {
            var expression = ParseExpression();
            Consume(TokenType.RightParen, $"Expected ')' after expression.");
            return expression;
        }
        return ParseClause();
    }

    private IQueryNode ParseClause()
    {
        string field = "Title";
        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            field = Consume(TokenType.Identifier).Text;
            Consume(TokenType.Colon);
        }

        string op = "contains"; // Default operator for user-friendliness
        if (IsOperator(Peek().Type))
            op = Consume(Peek().Type).Text;

        return ParseValueExpression(field, op);
    }

    private IQueryNode ParseValueExpression(string field, string op)
    {
        var valueToken = Peek();
        if (valueToken.Type == TokenType.StringLiteral)
        {
            return new ClauseNode(field, op, Consume(TokenType.StringLiteral).Text);
        }

        if (IsValueToken(valueToken.Type))
        {
            var firstNode = new ClauseNode(field, op, Consume(valueToken.Type).Text);
            IQueryNode logicalChain = firstNode;

            while (IsValueToken(Peek().Type))
            {
                var nextValue = Consume(Peek().Type).Text;
                logicalChain = new LogicalNode(logicalChain, LogicalOperator.And, new ClauseNode(field, op, nextValue));
            }
            return logicalChain;
        }
        throw new Exception($"Syntax Error: Expected a value for field '{field}' but found '{valueToken.Text}'.");
    }

    private Token Peek(int offset = 0) => _position + offset >= _tokens.Count ? _tokens.Last() : _tokens[_position + offset];
    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;
    private Token Consume(TokenType type, string message) => Peek().Type == type ? _tokens[_position++] : throw new Exception(message);
    private Token Consume(TokenType type) => Consume(type, $"Expected {type} but got {Peek().Type}.");
    private bool Match(params TokenType[] types)
    {
        if (IsAtEnd())
            return false;
        if (types.Any(t => Peek().Type == t))
        { _position++; return true; }
        return false;
    }
    private bool IsOperator(TokenType type) => type switch
    {
        TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterThanOrEqual or
        TokenType.LessThanOrEqual or TokenType.Equals or TokenType.Tilde or
        TokenType.Caret or TokenType.Dollar => true,
        _ => false
    };
    private bool IsValueToken(TokenType type) => type == TokenType.Identifier || type == TokenType.Number;
}
