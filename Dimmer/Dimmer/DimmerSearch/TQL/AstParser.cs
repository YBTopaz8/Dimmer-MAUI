using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.TQL;

public class AstParser
{
    private readonly List<Token> _tokens;
    private readonly List<Token> _directiveTokens = new();
    private int _position = 0;

    public IReadOnlyList<Token> DirectiveTokens => _directiveTokens;

    public AstParser(List<Token> tokens)
    {
        _tokens = [.. tokens];
        if (_tokens.Count == 0 || _tokens.Last().Type != TokenType.EndOfFile)
        {
            _tokens.Add(new Token(TokenType.EndOfFile, "", -1));
        }
    }

    public AstParser(string filterQuery) : this(Lexer.Tokenize(filterQuery)) { }

    public ParseResult<IQueryNode> Parse()
    {
        _directiveTokens.Clear();

        if (_tokens.All(t => t.Type == TokenType.EndOfFile))
            return ParseResult<IQueryNode>.Ok(new ClauseNode("any", "matchall", ""));

        ConsumeTopLevelDirectives();

        // Query contained only directives (e.g., "asc title" or "shuffle 50")
        if (IsAtEnd())
            return ParseResult<IQueryNode>.Ok(new ClauseNode("any", "matchall", ""));

        var result = ParseAddRemove();
        if (!result.IsSuccess) return result;

        ConsumeTopLevelDirectives();

        if (!IsAtEnd())
            return ParseResult<IQueryNode>.Fail($"Syntax error: Unexpected token '{Peek().Text}' after valid expression.", Peek().Position);

        return result;
    }

    private ParseResult<IQueryNode> ParseAddRemove()
    {
        ConsumeTopLevelDirectives();

        if (Match(TokenType.Exclude, TokenType.Remove))
        {
            var rightRes = ParseExpression();
            if (!rightRes.IsSuccess) return rightRes;

            var implicitLeft = new ClauseNode("any", "matchall", "");
            return ParseResult<IQueryNode>.Ok(new LogicalNode(implicitLeft, LogicalOperator.And, new NotNode(rightRes.Value!)));
        }

        var leftRes = ParseExpression();
        if (!leftRes.IsSuccess) return leftRes;
        var left = leftRes.Value!;

        while (true)
        {
            ConsumeTopLevelDirectives();
            if (!Match(TokenType.Add, TokenType.Include, TokenType.Remove, TokenType.Exclude))
                break;

            var opToken = Previous();
            var rightRes = ParseExpression();
            if (!rightRes.IsSuccess) return rightRes;

            left = opToken.Type switch
            {
                TokenType.Add or TokenType.Include => new LogicalNode(left, LogicalOperator.Or, rightRes.Value!),
                TokenType.Remove or TokenType.Exclude => new LogicalNode(left, LogicalOperator.And, new NotNode(rightRes.Value!)),
                _ => left
            };
        }

        return ParseResult<IQueryNode>.Ok(left);
    }

    private ParseResult<IQueryNode> ParseExpression()
    {
        var leftRes = ParseTerm();
        if (!leftRes.IsSuccess) return leftRes;
        var left = leftRes.Value!;

        while (true)
        {
            ConsumeTopLevelDirectives();
            if (!Match(TokenType.Or, TokenType.Pipe))
                break;

            var rightRes = ParseTerm();
            if (!rightRes.IsSuccess) return rightRes;
            left = new LogicalNode(left, LogicalOperator.Or, rightRes.Value!);
        }

        return ParseResult<IQueryNode>.Ok(left);
    }

    private ParseResult<IQueryNode> ParseTerm()
    {
        var leftRes = ParseFactor();
        if (!leftRes.IsSuccess) return leftRes;
        var left = leftRes.Value!;

        while (true)
        {
            ConsumeTopLevelDirectives();
            if (IsAtEnd() || !IsImplicitAnd())
                break;

            Match(TokenType.And);
            ConsumeTopLevelDirectives();

            if (IsAtEnd())
                return ParseResult<IQueryNode>.Fail("Expected an expression after 'AND'.", Peek().Position);

            var rightRes = ParseFactor();
            if (!rightRes.IsSuccess) return rightRes;
            left = new LogicalNode(left, LogicalOperator.And, rightRes.Value!);
        }

        return ParseResult<IQueryNode>.Ok(left);
    }

    private ParseResult<IQueryNode> ParseFactor()
    {
        ConsumeTopLevelDirectives();

        if (Match(TokenType.Not, TokenType.Bang))
        {
            var factorRes = ParseFactor();
            if (!factorRes.IsSuccess) return factorRes;
            return ParseResult<IQueryNode>.Ok(new NotNode(factorRes.Value!));
        }

        if (Match(TokenType.LeftParen))
        {
            var expressionRes = ParseAddRemove();
            if (!expressionRes.IsSuccess) return expressionRes;

            var parenRes = Consume(TokenType.RightParen, "Expected ')' after expression.");
            if (!parenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(parenRes.Error, parenRes.ErrorPosition);

            return expressionRes;
        }

        return ParseClause();
    }

    private ParseResult<IQueryNode> ParseClause()
    {
        var peekToken = Peek();
        string field = "any";
        string op = "contains";
        bool isNegated;

        if (peekToken.Type == TokenType.Identifier && Peek(1).Type != TokenType.Colon)
        {
            if (peekToken.Text.Equals("chance", StringComparison.OrdinalIgnoreCase))
                return ParseChanceClause();
        }

        if (Peek().Type == TokenType.Identifier && Peek(1).Type == TokenType.Colon)
        {
            var identRes = Consume(TokenType.Identifier);
            if (!identRes.IsSuccess) return ParseResult<IQueryNode>.Fail(identRes.Error, identRes.ErrorPosition);
            field = identRes.Value!.Text;

            if (!FieldRegistry.FieldsByAlias.ContainsKey(field))
                return ParseResult<IQueryNode>.Fail($"Unknown field '{field}'.", identRes.Value.Position);

            var colonRes = Consume(TokenType.Colon, $"Expected ':' after field '{field}'.");
            if (!colonRes.IsSuccess) return ParseResult<IQueryNode>.Fail(colonRes.Error, colonRes.ErrorPosition);
        }

        if (IsOperator(Peek().Type))
        {
            var opRes = Consume(Peek().Type);
            if (!opRes.IsSuccess) return ParseResult<IQueryNode>.Fail(opRes.Error, opRes.ErrorPosition);
            op = opRes.Value!.Text;
        }

        isNegated = Match(TokenType.Not, TokenType.Bang);

        // Handle field:in(val1, val2, ...) and field:!in(val1, val2)
        if (Peek().Type == TokenType.Identifier &&
            Peek().Text.Equals("in", StringComparison.OrdinalIgnoreCase) &&
            Peek(1).Type == TokenType.LeftParen)
        {
            Consume(TokenType.Identifier); // consume "in"
            var lParen = Consume(TokenType.LeftParen, "Expected '(' after 'in'.");
            if (!lParen.IsSuccess) return ParseResult<IQueryNode>.Fail(lParen.Error, lParen.ErrorPosition);

            var values = new List<string>();
            while (!Match(TokenType.RightParen))
            {
                if (IsAtEnd())
                    return ParseResult<IQueryNode>.Fail("Expected ')' to close 'in' list.", Peek().Position);

                var itemParts = new List<string>();
                while (!IsAtEnd() && Peek().Type is not (TokenType.Comma or TokenType.RightParen))
                {
                    var partRes = Consume(Peek().Type);
                    if (!partRes.IsSuccess) return ParseResult<IQueryNode>.Fail(partRes.Error, partRes.ErrorPosition);
                    itemParts.Add(partRes.Value!.Text);
                }

                if (itemParts.Count > 0)
                    values.Add(string.Join(" ", itemParts));

                Match(TokenType.Comma);
            }

            return ParseResult<IQueryNode>.Ok(new InNode(field, values, isNegated));
        }

        FieldRegistry.FieldsByAlias.TryGetValue(field, out var fieldDef);

        if (fieldDef?.Type == FieldType.Date)
        {
            var nextTokenForDate = Peek();
            if (nextTokenForDate.Type == TokenType.Identifier)
            {
                switch (nextTokenForDate.Text.ToLowerInvariant())
                {
                    case "ago":
                    case "between":
                    case "never":
                        return ParseFuzzyDateClause(field, op, isNegated);
                    case "morning":
                    case "afternoon":
                    case "evening":
                    case "night":
                        var daypartRes = ParseDaypartClause(field);
                        if (isNegated && daypartRes.IsSuccess)
                            return ParseResult<IQueryNode>.Ok(new NotNode(daypartRes.Value!));
                        return daypartRes;
                }
            }
        }

        var nextToken = Peek();
        if (IsStartOfNewClauseOrSegment(nextToken))
            return ParseResult<IQueryNode>.Fail($"Expected a value for field '{field}' but found the start of a new clause '{nextToken.Text}'.", nextToken.Position);

        if (!IsValueToken(nextToken.Type))
        {
            string example = fieldDef?.Type switch
            {
                FieldType.Numeric => "5",
                FieldType.Boolean => "true",
                FieldType.Date => "today",
                FieldType.Duration => "3:30",
                _ => "\"value\""
            };
            return ParseResult<IQueryNode>.Fail($"Expected a value for '{field}'. Example: {field}:{example}", nextToken.Position);
        }

        // Special handling for Date fields so ISO dates (2023-12-25) aren't torn apart by Minus tokens
        if (fieldDef?.Type == FieldType.Date)
        {
            string dateText = ConsumeDateValue();

            if (Match(TokenType.Minus))
            {
                if (IsValueToken(Peek().Type))
                {
                    string upperDateText = ConsumeDateValue();
                    return ParseResult<IQueryNode>.Ok(new ClauseNode(field, "-", dateText, upperDateText, isNegated));
                }
            }

            return ParseResult<IQueryNode>.Ok(new ClauseNode(field, op, dateText, isNegated: isNegated));
        }

        var valRes = Consume(nextToken.Type);
        if (!valRes.IsSuccess) return ParseResult<IQueryNode>.Fail(valRes.Error, valRes.ErrorPosition);

        Token lastConsumed = valRes.Value!;
        string valueText = lastConsumed.Text;
        bool allowMultiWordText = fieldDef == null || fieldDef.Type == FieldType.Text;

        if (allowMultiWordText)
        {
            while (!IsAtEnd())
            {
                var peek = Peek();

                // Stop if the next word starts a valid field:value clause
                if (peek.Type == TokenType.Identifier &&
                    Peek(1).Type == TokenType.Colon &&
                    FieldRegistry.FieldsByAlias.ContainsKey(peek.Text))
                    break;

                // Stop if it's a boolean/set keyword
                if (IsBooleanOrSetKeyword(peek.Type))
                    break;

                // Stop if it's an unambiguous directive (e.g. "desc plays", "first 10", "shuffle 20")
                if (IsDirectiveStartAtBoundary(0))
                    break;

                // Join adjacent hyphenated text like "AC-DC" or "Blink-182" without spaces
                if (peek.Type == TokenType.Minus &&
                    peek.Position == lastConsumed.Position + lastConsumed.Text.Length &&
                    Peek(1).Type is TokenType.Identifier or TokenType.Number)
                {
                    Consume(TokenType.Minus);
                    var rhs = Consume(Peek().Type).Value!;
                    valueText += "-" + rhs.Text;
                    lastConsumed = rhs;
                    continue;
                }

                if (peek.Type is not (TokenType.Identifier or TokenType.Number or TokenType.StringLiteral
                    or TokenType.First or TokenType.Last or TokenType.Random or TokenType.Shuffle
                    or TokenType.Asc or TokenType.Desc))
                    break;

                var nextPart = Consume(peek.Type).Value!;
                valueText += " " + nextPart.Text;
                lastConsumed = nextPart;
            }

            return ParseResult<IQueryNode>.Ok(new ClauseNode(field, op, valueText, isNegated: isNegated));
        }

        // Numeric / Duration range (e.g., year:2000-2010, len:2:00-4:00)
        if (Match(TokenType.Minus))
        {
            if (IsValueToken(Peek().Type))
            {
                var upperValRes = Consume(Peek().Type);
                if (!upperValRes.IsSuccess) return ParseResult<IQueryNode>.Fail(upperValRes.Error, upperValRes.ErrorPosition);
                return ParseResult<IQueryNode>.Ok(new ClauseNode(field, "-", valueText, upperValRes.Value!.Text, isNegated));
            }
        }

        return ParseResult<IQueryNode>.Ok(new ClauseNode(field, op, valueText, isNegated: isNegated));
    }

    private string ConsumeDateValue()
    {
        var first = Consume(Peek().Type).Value!;

        // Match YYYY-MM-DD (Number, Minus, Number, Minus, Number)
        if (first.Type == TokenType.Number &&
            Peek(0).Type == TokenType.Minus &&
            Peek(1).Type == TokenType.Number &&
            Peek(2).Type == TokenType.Minus &&
            Peek(3).Type == TokenType.Number)
        {
            Consume(TokenType.Minus);
            var month = Consume(TokenType.Number).Value!;
            Consume(TokenType.Minus);
            var day = Consume(TokenType.Number).Value!;
            return $"{first.Text}-{month.Text}-{day.Text}";
        }

        // Allow two-word relative dates like "this week", "last month", "last year"
        if (first.Type == TokenType.Identifier &&
            first.Text.Equals("this", StringComparison.OrdinalIgnoreCase) ||
            first.Text.Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            if (Peek().Type == TokenType.Identifier &&
                Peek().Text.ToLowerInvariant() is "week" or "month" or "year")
            {
                var unit = Consume(TokenType.Identifier).Value!;
                return $"{first.Text} {unit.Text}";
            }
        }

        return first.Text;
    }

    private void ConsumeTopLevelDirectives()
    {
        while (!IsAtEnd() && IsDirectiveStartAtBoundary(0))
        {
            var token = Peek();

            if (token.Type is TokenType.Asc or TokenType.Desc)
            {
                _directiveTokens.Add(Consume(token.Type).Value!);
                _directiveTokens.Add(Consume(TokenType.Identifier).Value!);
            }
            else if (token.Type is TokenType.First or TokenType.Last)
            {
                _directiveTokens.Add(Consume(token.Type).Value!);
                if (Peek().Type == TokenType.Number)
                    _directiveTokens.Add(Consume(TokenType.Number).Value!);
            }
            else if (token.Type is TokenType.Random or TokenType.Shuffle)
            {
                _directiveTokens.Add(Consume(token.Type).Value!);
                if (Peek().Type == TokenType.Number)
                    _directiveTokens.Add(Consume(TokenType.Number).Value!);

                if (Peek().Type == TokenType.Identifier &&
                    Peek().Text.Equals("by", StringComparison.OrdinalIgnoreCase) &&
                    Peek(1).Type == TokenType.Identifier &&
                    FieldRegistry.FieldsByAlias.ContainsKey(Peek(1).Text))
                {
                    _directiveTokens.Add(Consume(TokenType.Identifier).Value!); // by
                    _directiveTokens.Add(Consume(TokenType.Identifier).Value!); // field
                    if (Peek().Type is TokenType.Asc or TokenType.Desc)
                        _directiveTokens.Add(Consume(Peek().Type).Value!);
                }
            }
        }
    }

    private bool IsDirectiveStartAtBoundary(int offset)
    {
        var token = Peek(offset);

        if (token.Type is TokenType.Asc or TokenType.Desc)
        {
            return Peek(offset + 1).Type == TokenType.Identifier &&
                   FieldRegistry.FieldsByAlias.ContainsKey(Peek(offset + 1).Text) &&
                   Peek(offset + 2).Type != TokenType.Colon;
        }

        if (token.Type is TokenType.First or TokenType.Last)
        {
            var next = Peek(offset + 1);
            return next.Type == TokenType.Number || IsBoundaryToken(offset + 1);
        }

        if (token.Type is TokenType.Random or TokenType.Shuffle)
        {
            var next = Peek(offset + 1);
            if (next.Type == TokenType.Number) return true;
            if (next.Type == TokenType.Identifier &&
                next.Text.Equals("by", StringComparison.OrdinalIgnoreCase) &&
                Peek(offset + 2).Type == TokenType.Identifier &&
                FieldRegistry.FieldsByAlias.ContainsKey(Peek(offset + 2).Text))
                return true;

            return IsBoundaryToken(offset + 1);
        }

        return false;
    }

    private bool IsBoundaryToken(int offset)
    {
        var t = Peek(offset);
        return t.Type is TokenType.EndOfFile or TokenType.RightParen
            or TokenType.And or TokenType.Or or TokenType.Pipe
            or TokenType.Include or TokenType.Add or TokenType.Exclude or TokenType.Remove
            or TokenType.Asc or TokenType.Desc or TokenType.First or TokenType.Last
            or TokenType.Random or TokenType.Shuffle
            || (t.Type == TokenType.Identifier && Peek(offset + 1).Type == TokenType.Colon && FieldRegistry.FieldsByAlias.ContainsKey(t.Text));
    }

    private static bool IsBooleanOrSetKeyword(TokenType type) =>
        type is TokenType.And or TokenType.Or or TokenType.Not
        or TokenType.Include or TokenType.Add or TokenType.Exclude or TokenType.Remove;

    private ParseResult<IQueryNode> ParseChanceClause()
    {
        var identRes = Consume(TokenType.Identifier);
        if (!identRes.IsSuccess) return ParseResult<IQueryNode>.Fail(identRes.Error, identRes.ErrorPosition);

        var paren1 = Consume(TokenType.LeftParen, "Expected '(' after 'chance'.");
        if (!paren1.IsSuccess) return ParseResult<IQueryNode>.Fail(paren1.Error, paren1.ErrorPosition);

        var numToken = Consume(TokenType.Number, "Expected a number for chance percentage.");
        if (!numToken.IsSuccess) return ParseResult<IQueryNode>.Fail(numToken.Error, numToken.ErrorPosition);

        var paren2 = Consume(TokenType.RightParen, "Expected ')' after chance percentage.");
        if (!paren2.IsSuccess) return ParseResult<IQueryNode>.Fail(paren2.Error, paren2.ErrorPosition);

        string numberText = numToken.Value!.Text.Replace("%", "");
        if (int.TryParse(numberText, out int percentage))
            return ParseResult<IQueryNode>.Ok(new RandomChanceNode(percentage));

        return ParseResult<IQueryNode>.Fail($"Invalid percentage value '{numToken.Value.Text}'.", numToken.Value.Position);
    }

    private ParseResult<IQueryNode> ParseFuzzyDateClause(string field, string op, bool isNegated)
    {
        var typeTokenRes = Consume(TokenType.Identifier);
        if (!typeTokenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(typeTokenRes.Error, typeTokenRes.ErrorPosition);

        IQueryNode node;
        switch (typeTokenRes.Value!.Text.ToLowerInvariant())
        {
            case "never":
                node = new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Never, op);
                return ParseResult<IQueryNode>.Ok(isNegated ? new NotNode(node) : node);

            case "ago":
                var parenRes = Consume(TokenType.LeftParen, "Expected '(' after 'ago'.");
                if (!parenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(parenRes.Error, parenRes.ErrorPosition);

                string val;
                if (Peek().Type == TokenType.StringLiteral)
                {
                    val = Consume(TokenType.StringLiteral).Value!.Text;
                }
                else if (Peek().Type == TokenType.Number)
                {
                    val = Consume(TokenType.Number).Value!.Text;
                    if (Peek().Type == TokenType.Identifier)
                        val += Consume(TokenType.Identifier).Value!.Text;
                }
                else
                {
                    return ParseResult<IQueryNode>.Fail("Expected a time span (e.g. \"30d\" or 30d).", Peek().Position);
                }

                var rParenRes = Consume(TokenType.RightParen, "Expected ')' after time string.");
                if (!rParenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(rParenRes.Error, rParenRes.ErrorPosition);

                var spanRes = ParseTimeSpan(val);
                if (!spanRes.IsSuccess) return ParseResult<IQueryNode>.Fail(spanRes.Error, spanRes.ErrorPosition);

                node = new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Ago, op, spanRes.Value);
                return ParseResult<IQueryNode>.Ok(isNegated ? new NotNode(node) : node);

            case "between":
                var bParenRes = Consume(TokenType.LeftParen, "Expected '(' after 'between'.");
                if (!bParenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(bParenRes.Error, bParenRes.ErrorPosition);

                var olderValToken = Consume(TokenType.StringLiteral, "Expected the 'older' time string.");
                if (!olderValToken.IsSuccess) return ParseResult<IQueryNode>.Fail(olderValToken.Error, olderValToken.ErrorPosition);

                var commaRes = Consume(TokenType.Comma, "Expected a comma ',' separating the two date ranges.");
                if (!commaRes.IsSuccess) return ParseResult<IQueryNode>.Fail(commaRes.Error, commaRes.ErrorPosition);

                var newerValToken = Consume(TokenType.StringLiteral, "Expected the 'newer' time string.");
                if (!newerValToken.IsSuccess) return ParseResult<IQueryNode>.Fail(newerValToken.Error, newerValToken.ErrorPosition);

                var rbParenRes = Consume(TokenType.RightParen, "Expected ')' after the second time string.");
                if (!rbParenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(rbParenRes.Error, rbParenRes.ErrorPosition);

                var olderTimeSpan = ParseTimeSpan(olderValToken.Value!.Text);
                var newerTimeSpan = ParseTimeSpan(newerValToken.Value!.Text);

                if (!olderTimeSpan.IsSuccess) return ParseResult<IQueryNode>.Fail(olderTimeSpan.Error, olderValToken.ErrorPosition);
                if (!newerTimeSpan.IsSuccess) return ParseResult<IQueryNode>.Fail(newerTimeSpan.Error, newerValToken.ErrorPosition);

                if (olderTimeSpan.Value < newerTimeSpan.Value)
                    return ParseResult<IQueryNode>.Fail("The first date in 'between' must be older than the second.", olderValToken.Value.Position);

                node = new FuzzyDateNode(field, FuzzyDateNode.Qualifier.Between, op, olderTimeSpan.Value, newerTimeSpan.Value);
                return ParseResult<IQueryNode>.Ok(isNegated ? new NotNode(node) : node);

            default:
                return ParseResult<IQueryNode>.Fail($"Unknown fuzzy date qualifier '{typeTokenRes.Value.Text}'.", typeTokenRes.Value.Position);
        }
    }

    private ParseResult<TimeSpan> ParseTimeSpan(string text)
    {
        text = text.Replace("ago", "").Trim();
        var match = Regex.Match(text, @"(\d+)\s*([a-zA-Z]+)");
        if (!match.Success)
            return ParseResult<TimeSpan>.Fail($"Invalid time span format '{text}'.", 0);

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "d" or "day" or "days" => ParseResult<TimeSpan>.Ok(TimeSpan.FromDays(value)),
            "w" or "week" or "weeks" => ParseResult<TimeSpan>.Ok(TimeSpan.FromDays(value * 7)),
            "m" or "month" or "months" => ParseResult<TimeSpan>.Ok(TimeSpan.FromDays(value * 30.44)),
            "y" or "year" or "years" => ParseResult<TimeSpan>.Ok(TimeSpan.FromDays(value * 365.25)),
            _ => ParseResult<TimeSpan>.Fail($"Unknown time unit '{unit}' in '{text}'.", 0)
        };
    }

    private ParseResult<IQueryNode> ParseDaypartClause(string field)
    {
        var daypartTokenRes = Consume(TokenType.Identifier);
        if (!daypartTokenRes.IsSuccess) return ParseResult<IQueryNode>.Fail(daypartTokenRes.Error, daypartTokenRes.ErrorPosition);

        var (start, end) = daypartTokenRes.Value!.Text.ToLowerInvariant() switch
        {
            "morning" => (TimeSpan.FromHours(6), TimeSpan.FromHours(12)),
            "afternoon" => (TimeSpan.FromHours(12), TimeSpan.FromHours(18)),
            "evening" => (TimeSpan.FromHours(18), TimeSpan.FromHours(22)),
            "night" => (TimeSpan.FromHours(22), TimeSpan.FromHours(6)),
            _ => (TimeSpan.Zero, TimeSpan.Zero)
        };

        if (start == TimeSpan.Zero && end == TimeSpan.Zero)
            return ParseResult<IQueryNode>.Fail("Invalid daypart specified.", daypartTokenRes.Value.Position);

        return ParseResult<IQueryNode>.Ok(new DaypartNode(field, start, end));
    }

    private bool IsImplicitAnd()
    {
        if (IsAtEnd()) return false;
        return Peek().Type switch
        {
            TokenType.Or or TokenType.Pipe or TokenType.RightParen or
            TokenType.Include or TokenType.Add or
            TokenType.Exclude or TokenType.Remove => false,
            _ => true,
        };
    }

    private bool IsStartOfNewClauseOrSegment(Token token)
    {
        return token.Type == TokenType.Identifier && (
            token.Text.Equals("chance", StringComparison.OrdinalIgnoreCase) ||
            (Peek(1).Type == TokenType.Colon && FieldRegistry.FieldsByAlias.ContainsKey(token.Text)));
    }

    private Token Previous() => _tokens[_position - 1];
    private Token Peek(int offset = 0) => _position + offset >= _tokens.Count ? _tokens.Last() : _tokens[_position + offset];
    private bool IsAtEnd() => Peek().Type == TokenType.EndOfFile;

    private ParseResult<Token> Consume(TokenType type, string message)
    {
        if (Peek().Type == type) return ParseResult<Token>.Ok(_tokens[_position++]);
        return ParseResult<Token>.Fail(message, Peek().Position);
    }

    private ParseResult<Token> Consume(TokenType type) => Consume(type, $"Expected {type} but got {Peek().Type}.");

    private bool Match(params TokenType[] types)
    {
        if (IsAtEnd() || !types.Contains(Peek().Type)) return false;
        _position++;
        return true;
    }

    private static bool IsOperator(TokenType type) =>
        type is TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterThanOrEqual
             or TokenType.LessThanOrEqual or TokenType.Equals or TokenType.Tilde
             or TokenType.Caret or TokenType.Dollar;

    private static bool IsValueToken(TokenType type) =>
        type is TokenType.Identifier or TokenType.Number or TokenType.StringLiteral
        or TokenType.First or TokenType.Last or TokenType.Random or TokenType.Shuffle
        or TokenType.Asc or TokenType.Desc or TokenType.And or TokenType.Or or TokenType.Not
        or TokenType.Add or TokenType.Include or TokenType.Remove or TokenType.Exclude;
}