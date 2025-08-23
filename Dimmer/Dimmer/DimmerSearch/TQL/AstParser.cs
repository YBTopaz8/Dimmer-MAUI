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
        var result = ParseExpression();
        if (!IsAtEnd())
            throw new ParsingException($"Syntax error: Unexpected token '{Peek().Text}' after valid expression.", Peek().Position);
        return result;
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

    private IQueryNode ParseTerm()
    {
        var left = ParseFactor();
        while (!IsAtEnd() && IsImplicitAnd())
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
            Consume(TokenType.RightParen, "Expected ')' after expression.");
            return expression;
        }
        return ParseClause();
    }
    private IQueryNode ParseClause()
    {
        var peekToken = Peek();
        string field = "any";
        string op = "contains"; // Default operator
        bool isNegated = false;

        // --- Step 1: Check for standalone keywords first ---
        if (peekToken.Type == TokenType.Identifier && Peek(1).Type != TokenType.Colon)
        {
            if (peekToken.Text.Equals("chance", StringComparison.OrdinalIgnoreCase))
            {
                return ParseChanceClause();
            }
        }

        // --- Step 2: Parse the field name (if it exists) ---
        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            field = Consume(TokenType.Identifier).Text;
            Consume(TokenType.Colon, $"Expected ':' after field '{field}'.");
        }

        // --- Step 3: Check for operators and negation ---
        if (IsOperator(Peek().Type))
        {
            op = Consume(Peek().Type).Text;
        }
        isNegated = Match(TokenType.Not, TokenType.Bang);

        // --- Step 4: Context-aware value parsing ---
        if (FieldRegistry.FieldsByAlias.TryGetValue(field, out var fieldDef) && fieldDef.Type == FieldType.Date)
        {
            // If the field is a date, check for our special keywords
            var nextToken = Peek();
            if (nextToken.Type == TokenType.Identifier)
            {
                switch (nextToken.Text.ToLowerInvariant())
                {
                    case "ago":
                    case "between":
                    case "never":
                        // --- FIX: Pass the parsed operator to the fuzzy date parser ---
                        return ParseFuzzyDateClause(field, op);
                    case "morning":
                    case "afternoon":
                    case "evening":
                    case "night":
                        return ParseDaypartClause(field);
                }
            }
        }

        // --- Step 5: If it's not a special date keyword, fall back to generic value parsing ---
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

    private bool IsImplicitAnd()
    {
        if (IsAtEnd())
            return false;
        return Peek().Type switch
        {
            TokenType.Or or TokenType.Pipe or TokenType.RightParen or
            TokenType.Include or TokenType.Add or
            TokenType.Exclude or TokenType.Remove => false,

            _ => true,
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