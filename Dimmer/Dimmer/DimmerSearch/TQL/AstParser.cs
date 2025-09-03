using Dimmer.DimmerSearch.Exceptions;

using System.Text.RegularExpressions;

using static ATL.TagData;

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

        var result = ParseAddRemove(); // StartAsync parsing from the lowest precedence operator

        if (!IsAtEnd())
            throw new ParsingException($"Syntax error: Unexpected token '{Peek().Text}' after valid expression.", Peek().Position);

        return result;
    }
    private IQueryNode ParseAddRemove()
    {
        var left = ParseExpression(); // Parse the next level up

        while (Match(TokenType.Add, TokenType.Include, TokenType.Remove, TokenType.Exclude))
        {
            var opToken = Previous();
            var right = ParseExpression();

            left = opToken.Type switch
            {
                TokenType.Add or TokenType.Include => new LogicalNode(left, LogicalOperator.Or, right),
                TokenType.Remove or TokenType.Exclude => new LogicalNode(left, LogicalOperator.And, new NotNode(right)),
                _ => left
            };
        }
        return left;
    }

    private IQueryNode ParseExpression()
    {
        var left = ParseTerm();
        while (Match(TokenType.Or, TokenType.Pipe))
        {
            left = new LogicalNode(left, LogicalOperator.Or, ParseTerm());
        }
        return left;
    }

    // Level 3: Handles implicit 'and'
    private IQueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (!IsAtEnd() && IsImplicitAnd())
        {
            Match(TokenType.And); // Consume optional 'and' keyword
            left = new LogicalNode(left, LogicalOperator.And, ParseFactor());
        }
        return left;
    }

    // Level 4: Handles 'not', parentheses, and clauses
    private IQueryNode ParseFactor()
    {
        if (Match(TokenType.Not, TokenType.Bang))
            return new NotNode(ParseFactor());

        if (Match(TokenType.LeftParen))
        {
            var expression = ParseAddRemove(); // A parenthesis can contain a full sub-query, so restart from the top level
            Consume(TokenType.RightParen, "Expected ')' after expression.");
            return expression;
        }

        return ParseClause();
    }

    // Level 5 (Highest Precedence): Handles individual clauses like 'artist:name'
    private IQueryNode ParseClause()
    {
        var peekToken = Peek();
        string field = "any";
        string op = "contains";
        bool isNegated = false;

        if (peekToken.Type == TokenType.Identifier && Peek(1).Type != TokenType.Colon)
        {
            if (peekToken.Text.Equals("chance", StringComparison.OrdinalIgnoreCase))
            {
                return ParseChanceClause();
            }
        }

        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            field = Consume(TokenType.Identifier).Text;
            Consume(TokenType.Colon, $"Expected ':' after field '{field}'.");
        }

        if (IsOperator(Peek().Type))
        {
            op = Consume(Peek().Type).Text;
        }
        isNegated = Match(TokenType.Not, TokenType.Bang);

        // Date keyword parsing...
        if (FieldRegistry.FieldsByAlias.TryGetValue(field, out var fieldDef) && fieldDef.Type == FieldType.Date)
        {
            var nextTokenForDate = Peek();
            if (nextTokenForDate.Type == TokenType.Identifier)
            {
                switch (nextTokenForDate.Text.ToLowerInvariant())
                {
                    case "ago":
                    case "between":
                    case "never":
                        return ParseFuzzyDateClause(field, op);
                    case "morning":
                    case "afternoon":
                    case "evening":
                    case "night":
                        return ParseDaypartClause(field);
                }
            }
        }

        // Stricter check for what a value can be
        var nextToken = Peek();
        if (IsStartOfNewClauseOrSegment(nextToken))
        {
            throw new ParsingException($"Expected a value for field '{field}' but found the start of a new clause '{nextToken.Text}'.", nextToken.Position);
        }

        if (!IsValueToken(nextToken.Type))
            throw new ParsingException($"Expected a value for field '{field}' but found '{nextToken.Text}'.", nextToken.Position);

        var valueToken = Consume(nextToken.Type);

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

    // This helper determines when to stop an implicit AND chain.
    private bool IsImplicitAnd()
    {
        if (IsAtEnd())
            return false;
        return Peek().Type switch
        {
            TokenType.Or or TokenType.Pipe or TokenType.RightParen or
            TokenType.Include or TokenType.Add or
            TokenType.Exclude or TokenType.Remove => false, // These stop the 'and' chain
            _ => true,
        };
    }

    // This helper prevents the parser from consuming a keyword as a value.
    private bool IsStartOfNewClauseOrSegment(Token token)
    {
        if (token.Type == TokenType.Identifier && (
            token.Text.Equals("chance", StringComparison.OrdinalIgnoreCase) ||
            Peek(1).Type == TokenType.Colon))
        {
            return true;
        }
        return false; // The main precedence parser now handles add/remove
    }

    private RandomChanceNode ParseChanceClause()
    {
        Consume(TokenType.Identifier);
        Consume(TokenType.LeftParen, "Expected '(' after 'chance'.");
        var numberToken = Consume(TokenType.Number, "Expected a number for chance percentage.");
        Consume(TokenType.RightParen, "Expected ')' after chance percentage.");

        string numberText = numberToken.Text.Replace("%", "");
        if (int.TryParse(numberText, out int percentage))
        {
            return new RandomChanceNode(percentage);
        }
        throw new ParsingException($"Invalid percentage value '{numberToken.Text}'.", numberToken.Position);
    }

    private IQueryNode ParseFuzzyDateClause(string field, string op)
    {
        var typeToken = Consume(TokenType.Identifier);
        switch (typeToken.Text.ToLowerInvariant())
        {
            case "never":
                // --- ADD: Pass the operator to the node ---
                return new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Never, op);
            case "ago":
                Consume(TokenType.LeftParen, "Expected '(' after 'ago'.");
                var agoVal = Consume(TokenType.StringLiteral, "Expected a time string like \"30d\" or \"1y\".");
                Consume(TokenType.RightParen, "Expected ')' after time string.");
                // --- ADD: Pass the operator to the node ---
                return new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Ago, op, ParseTimeSpan(agoVal.Text));
            case "between":
                Consume(TokenType.LeftParen, "Expected '(' after 'between'.");
                var olderValToken = Consume(TokenType.StringLiteral, "Expected the 'older' time string.");
                Consume(TokenType.Comma, "Expected a comma ',' separating the two date ranges.");
                var newerValToken = Consume(TokenType.StringLiteral, "Expected the 'newer' time string.");
                Consume(TokenType.RightParen, "Expected ')' after the second time string.");
                var olderTimeSpan = ParseTimeSpan(olderValToken.Text);
                var newerTimeSpan = ParseTimeSpan(newerValToken.Text);
                if (olderTimeSpan < newerTimeSpan)
                {
                    throw new ParsingException("The first date in 'between' must be older than the second.", olderValToken.Position);
                }
                // --- ADD: Pass the operator to the node (usually defaults to 'contains') ---
                return new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Between, op, olderTimeSpan, newerTimeSpan);
            default:
                throw new ParsingException($"Unknown fuzzy date qualifier '{typeToken.Text}'.", typeToken.Position);
        }
    }

    private TimeSpan ParseTimeSpan(string text)
    {
        text = text.Replace("ago", "").Trim();
        var match = Regex.Match(text, @"(\d+)\s*([a-zA-Z]+)");
        if (!match.Success)
            throw new ParsingException($"Invalid time span format '{text}'.", 0);

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "d" or "day" or "days" => TimeSpan.FromDays(value),
            "w" or "week" or "weeks" => TimeSpan.FromDays(value * 7),
            "m" or "month" or "months" => TimeSpan.FromDays(value * 30.44),
            "y" or "year" or "years" => TimeSpan.FromDays(value * 365.25),
            _ => throw new ParsingException($"Unknown time unit '{unit}' in '{text}'.", 0)
        };
    }

    private DaypartNode ParseDaypartClause(string field)
    {
        var daypartToken = Consume(TokenType.Identifier);
        var (start, end) = daypartToken.Text.ToLowerInvariant() switch
        {
            "morning" => (TimeSpan.FromHours(6), TimeSpan.FromHours(12)),
            "afternoon" => (TimeSpan.FromHours(12), TimeSpan.FromHours(18)),
            "evening" => (TimeSpan.FromHours(18), TimeSpan.FromHours(22)),
            "night" => (TimeSpan.FromHours(22), TimeSpan.FromHours(6)),
            _ => throw new ParsingException("Invalid daypart specified.", daypartToken.Position)
        };
        return new DaypartNode(field, start, end);
    }

    private Token Previous() => _tokens[_position - 1];

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