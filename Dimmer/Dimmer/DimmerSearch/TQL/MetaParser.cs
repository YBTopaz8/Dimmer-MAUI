using System.Text.RegularExpressions;
using Dimmer.DimmerSearch.TQLActions;

namespace Dimmer.DimmerSearch.TQL;

public class QuerySegment
{
    public SegmentType SegmentType { get; }
    public List<Token> FilterTokens { get; }
    public List<Token> DirectiveTokens { get; }
    public QuerySegment(SegmentType type, List<Token> filter, List<Token> directives)
    {
        SegmentType = type;
        FilterTokens = filter;
        DirectiveTokens = directives;
    }
}

public enum SegmentType { Main, Include, Exclude }

public static class MetaParser
{
    public static RealmQueryPlan Parse(string rawQuery)
    {
        try
        {
            // 1. Split Query into Filter and Command parts
            var (filterQuery, commandQuery) = SplitFilterAndCommand(rawQuery);

            // 2. Tokenize the filter string
            var filterTokens = Lexer.Tokenize(filterQuery)
                .Where(t => t.Type != TokenType.EndOfFile)
                .ToList();

            // 3. Parse AST + extract context-aware directive tokens
            var parser = new AstParser(filterTokens);
            var astResult = parser.Parse();

            if (!astResult.IsSuccess)
            {
                var match = Regex.Match(astResult.Error!, @"Unknown field '(\w+)'");
                string? suggestion = match.Success ? QueryValidator.SuggestCorrectField(match.Groups[1].Value) : null;
                return CreateErrorPlan(astResult.Error!, suggestion);
            }

            // 4. Semantic validation (numeric, boolean, duration formats)
            var validation = QueryValidator.Validate(filterTokens);
            if (!validation.IsValid)
            {
                return CreateErrorPlan(validation.Message);
            }

            IQueryNode masterAst = astResult.Value!;

            // 5. Split the AST for hybrid execution
            var (databaseAst, _) = AstSplitter.Split(masterAst);

            // 6. Generate the RQL and the in-memory predicate
            var rqlFilter = RqlGenerator.Generate(databaseAst);
            var inMemoryPredicate = new AstEvaluator().CreatePredicate(masterAst);

            // 7. Parse the extracted directive tokens
            var directiveTokens = parser.DirectiveTokens;
            var sortDescriptions = CreateSortDescriptions(directiveTokens);
            var limiter = CreateLimiterClause(directiveTokens);
            var shuffleNode = CreateShuffleNode(directiveTokens);

            // 8. Parse the Command part
            var commandResult = ParseCommand(commandQuery);
            if (!commandResult.IsSuccess)
            {
                return CreateErrorPlan(commandResult.Error!);
            }

            // 9. Assemble and return the final plan
            return new RealmQueryPlan(rqlFilter, inMemoryPredicate, sortDescriptions, limiter, commandResult.Value, shuffleNode);
        }
        catch (Exception ex)
        {
            return CreateErrorPlan("An unexpected error occurred during parsing. " + ex.Message);
        }
    }

    private static RealmQueryPlan CreateErrorPlan(string message, string? suggestion = null)
    {
        Func<SongModel, bool> predicate = _ => false;
        return new RealmQueryPlan("FALSEPREDICATE", predicate, [], null, null, null, message, suggestion);
    }

    private static (string filterQuery, string commandQuery) SplitFilterAndCommand(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
            return (string.Empty, string.Empty);

        const string commandStart = ">>";
        int commandStartIndex = rawQuery.LastIndexOf(commandStart, StringComparison.Ordinal);

        if (commandStartIndex != -1 && (commandStartIndex == 0 || char.IsWhiteSpace(rawQuery[commandStartIndex - 1])))
        {
            string filterPart = rawQuery.Substring(0, commandStartIndex).Trim();
            string commandPart = rawQuery.Substring(commandStartIndex + commandStart.Length).Trim();

            if (commandPart.EndsWith('!'))
                commandPart = commandPart[..^1].TrimEnd();

            return (filterPart, commandPart);
        }

        return (rawQuery, string.Empty);
    }

    private static ParseResult<CommandNode?> ParseCommand(string commandQuery)
    {
        if (string.IsNullOrWhiteSpace(commandQuery))
            return ParseResult<CommandNode?>.Ok(null);

        var commandTokens = Lexer.Tokenize(commandQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        if (commandTokens.Count == 0 || commandTokens.First().Type != TokenType.Identifier)
            return ParseResult<CommandNode?>.Ok(null);

        var commandToken = commandTokens.First();
        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        switch (commandName)
        {
            case "save":
            case "savepl":
                if (argTokens.Count != 0)
                    arguments["playlistName"] = string.Join(" ", argTokens.Select(t => t.Text));
                else
                    return ParseResult<CommandNode?>.Fail("The 'save' command requires a playlist name.", commandToken.Position);
                break;

            case "addnext":
            case "addend":
                break;

            case "addto":
            case "addtopos":
                if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number && int.TryParse(argTokens[0].Text, out int position))
                    arguments["position"] = position;
                else
                    return ParseResult<CommandNode?>.Fail("The 'addto' command requires a single number argument (e.g., '>> addto 6!').", commandToken.Position);
                break;

            case "addall":
                if (argTokens.Count < 2)
                    return ParseResult<CommandNode?>.Fail("The 'addall' command requires indices and a position (e.g., '>> addall (1,3) next!').", commandToken.Position);

                int openParenIndex = argTokens.FindIndex(t => t.Type == TokenType.LeftParen);
                if (openParenIndex == -1)
                    return ParseResult<CommandNode?>.Fail("Missing index set for 'addall' command.", commandToken.Position);

                int closeParenIndex = argTokens.FindIndex(openParenIndex, t => t.Type == TokenType.RightParen);
                if (closeParenIndex == -1)
                    return ParseResult<CommandNode?>.Fail("Mismatched parentheses in 'addall' command.", openParenIndex);

                if (closeParenIndex + 1 >= argTokens.Count)
                    return ParseResult<CommandNode?>.Fail("Missing position (e.g., 'next', 'end') after index set for 'addall'.", argTokens[closeParenIndex].Position);

                var indexTokens = argTokens.GetRange(openParenIndex, closeParenIndex - openParenIndex + 1);
                var positionToken = argTokens[closeParenIndex + 1];

                var parsedIndicesRes = ParseIndexSet(indexTokens);
                if (!parsedIndicesRes.IsSuccess)
                    return ParseResult<CommandNode?>.Fail(parsedIndicesRes.Error, parsedIndicesRes.ErrorPosition);

                arguments["indices"] = parsedIndicesRes.Value!;
                arguments["position"] = positionToken.Text.ToLowerInvariant();
                break;

            case "viewal":
                int albumIndex = 1;
                if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
                    int.TryParse(argTokens[0].Text, out albumIndex);
                arguments["albumIndex"] = Math.Max(1, albumIndex);
                break;

            case "scrollto":
            case "deletedup":
            case "deleteall":
            case "play":
                break;
        }

        return ParseResult<CommandNode?>.Ok(new CommandNode(commandName, arguments));
    }

    private static List<TQLSortDescription> CreateSortDescriptions(IReadOnlyList<Token> allDirectives)
    {
        var sortDescriptions = new List<TQLSortDescription>();
        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            if (token.Type is TokenType.Asc or TokenType.Desc)
            {
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Identifier)
                {
                    string fieldAlias = allDirectives[i + 1].Text;
                    if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
                    {
                        var direction = token.Type == TokenType.Asc ? TQLSortDirection.Ascending : TQLSortDirection.Descending;
                        sortDescriptions.Add(new TQLSortDescription(fieldDef, direction));
                    }
                    i++;
                }
            }
        }
        return sortDescriptions;
    }

    public static LimiterClause? CreateLimiterClause(IReadOnlyList<Token> allDirectives)
    {
        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            var limiterType = token.Type switch
            {
                TokenType.First => LimiterType.First,
                TokenType.Last => LimiterType.Last,
                _ => (LimiterType?)null
            };

            if (limiterType.HasValue)
            {
                int count = 1;
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                    int.TryParse(allDirectives[i + 1].Text, out count);
                return new LimiterClause(limiterType.Value, Math.Max(1, count));
            }
        }
        return null;
    }

    private static ShuffleNode? CreateShuffleNode(IReadOnlyList<Token> allDirectives)
    {
        var shuffleTokenIndex = -1;
        for (int i = 0; i < allDirectives.Count; i++)
        {
            if (allDirectives[i].Type is TokenType.Shuffle or TokenType.Random)
            {
                shuffleTokenIndex = i;
                break;
            }
        }

        if (shuffleTokenIndex == -1) return null;

        int count = int.MaxValue;
        int currentIndex = shuffleTokenIndex + 1;

        if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Number)
        {
            if (int.TryParse(allDirectives[currentIndex].Text, out int parsedCount) && parsedCount > 0)
                count = parsedCount;
            currentIndex++;
        }

        if (currentIndex + 1 < allDirectives.Count &&
            allDirectives[currentIndex].Text.Equals("by", StringComparison.OrdinalIgnoreCase) &&
            allDirectives[currentIndex + 1].Type == TokenType.Identifier)
        {
            string fieldAlias = allDirectives[currentIndex + 1].Text;
            currentIndex += 2;

            if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
            {
                var direction = TQLSortDirection.Ascending;
                if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Desc)
                    direction = TQLSortDirection.Descending;
                return new ShuffleNode(count, fieldDef, direction);
            }
        }

        return new ShuffleNode(count);
    }

    private static ParseResult<HashSet<int>> ParseIndexSet(List<Token> tokens)
    {
        var indices = new HashSet<int>();
        if (tokens.Count < 3 || tokens[0].Type != TokenType.LeftParen || tokens.Last().Type != TokenType.RightParen)
            return ParseResult<HashSet<int>>.Fail("Invalid index format. Expected format like (1,3,5-9).", tokens.FirstOrDefault()?.Position ?? 0);

        var innerTokens = tokens.Skip(1).Take(tokens.Count - 2).ToList();

        for (int i = 0; i < innerTokens.Count; i++)
        {
            var currentToken = innerTokens[i];

            if (currentToken.Type == TokenType.Number)
            {
                if (!int.TryParse(currentToken.Text, out int index))
                    return ParseResult<HashSet<int>>.Fail($"Invalid number '{currentToken.Text}' in index set.", currentToken.Position);

                if (i + 2 < innerTokens.Count && innerTokens[i + 1].Type == TokenType.Minus && innerTokens[i + 2].Type == TokenType.Number)
                {
                    if (!int.TryParse(innerTokens[i + 2].Text, out int endIndex))
                        return ParseResult<HashSet<int>>.Fail($"Invalid end range number '{innerTokens[i + 2].Text}'.", innerTokens[i + 2].Position);

                    if (endIndex < index)
                        return ParseResult<HashSet<int>>.Fail("End range must be greater than or equal to start range.", innerTokens[i + 2].Position);

                    for (int j = index; j <= endIndex; j++)
                        indices.Add(j - 1);
                    i += 2;
                }
                else
                {
                    indices.Add(index - 1);
                }
            }
            else if (currentToken.Type == TokenType.Comma)
            {
                continue;
            }
            else
            {
                return ParseResult<HashSet<int>>.Fail($"Unexpected token '{currentToken.Text}' in index set.", currentToken.Position);
            }
        }

        return ParseResult<HashSet<int>>.Ok(indices);
    }
}