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
    private static readonly Dictionary<TokenType, SegmentType> _segmentTypeMap = new()
    {
        { TokenType.Include, SegmentType.Include }, { TokenType.Add, SegmentType.Include },
        { TokenType.Exclude, SegmentType.Exclude }, { TokenType.Remove, SegmentType.Exclude }
    };

    private static readonly HashSet<TokenType> _directiveTokens = new()
        { TokenType.Asc, TokenType.Desc, TokenType.Random, TokenType.Shuffle, TokenType.First, TokenType.Last };

    public static RealmQueryPlan Parse(string rawQuery)
    {
        try
        {
            // 1. Split Query into Filter and Command parts
            var (filterQuery, commandQuery) = SplitFilterAndCommand(rawQuery);

            // 2. Tokenize the entire filter string
            var allTokens = Lexer.Tokenize(filterQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();

            // 3. Separate the main filter tokens from the directive tokens (sort, limit, etc.)
            var (filterTokens, directiveTokens) = SeparateFilterAndDirectives(allTokens);

            // 4. Build the Master AST from ONLY the filter tokens
            var astResult = new AstParser(filterTokens).Parse();

            // If parsing failed, we return the Error Plan gracefully.
            if (!astResult.IsSuccess)
            {
                var match = Regex.Match(astResult.Error!, @"Unknown field '(\w+)'");
                string? suggestion = match.Success ? QueryValidator.SuggestCorrectField(match.Groups[1].Value) : null;
                return CreateErrorPlan(astResult.Error!, suggestion);
            }

            // Proceed safely
            IQueryNode masterAst = astResult.Value!;

            // 5. Split the AST for hybrid execution
            var (databaseAst, inMemoryAst) = AstSplitter.Split(masterAst);

            // 6. Generate the RQL and the in-memory predicate
            var rqlFilter = RqlGenerator.Generate(databaseAst);
            var inMemoryPredicate = new AstEvaluator().CreatePredicate(masterAst);

            // 7. Parse the separated directive tokens
            var sortDescriptions = CreateSortDescriptions(directiveTokens);
            var limiter = CreateLimiterClause(directiveTokens);
            var shuffleNode = CreateShuffleNode(directiveTokens);

            // 8. Parse the Command part (USING NEW RESULT PATTERN)
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

    private static (List<Token> filterTokens, List<Token> directiveTokens) SeparateFilterAndDirectives(List<Token> allTokens)
    {
        var filterTokens = new List<Token>();
        var directiveTokens = new List<Token>();

        for (int i = 0; i < allTokens.Count; i++)
        {
            var token = allTokens[i];

            if (token.Type is TokenType.Asc or TokenType.Desc)
            {
                directiveTokens.Add(token);
                if (i + 1 < allTokens.Count && allTokens[i + 1].Type == TokenType.Identifier)
                {
                    if (i + 2 >= allTokens.Count || allTokens[i + 2].Type != TokenType.Colon)
                    {
                        directiveTokens.Add(allTokens[++i]);
                    }
                }
            }
            else if (token.Type is TokenType.First or TokenType.Last or TokenType.Random or TokenType.Shuffle)
            {
                directiveTokens.Add(token);

                if (i + 1 < allTokens.Count && allTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(allTokens[++i]);
                }

                if (i + 1 < allTokens.Count && allTokens[i + 1].Type == TokenType.Identifier && allTokens[i + 1].Text.Equals("by", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 2 >= allTokens.Count || allTokens[i + 2].Type != TokenType.Colon)
                    {
                        directiveTokens.Add(allTokens[++i]);
                        if (i + 1 < allTokens.Count && allTokens[i + 1].Type == TokenType.Identifier)
                        {
                            directiveTokens.Add(allTokens[++i]);
                            if (i + 1 < allTokens.Count && allTokens[i + 1].Type is TokenType.Asc or TokenType.Desc)
                            {
                                directiveTokens.Add(allTokens[++i]);
                            }
                        }
                    }
                }
            }
            else
            {
                filterTokens.Add(token);
            }
        }

        return (filterTokens, directiveTokens);
    }

    private static RealmQueryPlan CreateErrorPlan(string message, string? suggestion = null)
    {
        Func<SongModel, bool> predicate = _ => false;
        return new RealmQueryPlan("FALSEPREDICATE", predicate, [], null, null, null, message, suggestion);
    }

    private static (string filterQuery, string commandQuery) SplitFilterAndCommand(string rawQuery)
    {
        const string commandStart = " >>";
        const string commandEnd = "!";

        int commandEndIndex = rawQuery.LastIndexOf(commandEnd);
        if (commandEndIndex == rawQuery.Length - 1)
        {
            int commandStartIndex = rawQuery.LastIndexOf(commandStart, commandEndIndex);
            if (commandStartIndex != -1)
            {
                string filterPart = rawQuery.Substring(0, commandStartIndex);
                string commandPart = rawQuery.Substring(commandStartIndex + commandStart.Length, commandEndIndex - (commandStartIndex + commandStart.Length));
                return (filterPart, commandPart);
            }
        }
        return (rawQuery, string.Empty);
    }

    private static ParseResult<CommandNode?> ParseCommand(string commandQuery)
    {
        if (string.IsNullOrWhiteSpace(commandQuery))
        {
            return ParseResult<CommandNode?>.Ok(null);
        }

        var commandTokens = Lexer.Tokenize(commandQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        if (commandTokens.Count == 0 || commandTokens.First().Type != TokenType.Identifier)
        {
            return ParseResult<CommandNode?>.Ok(null);
        }

        var commandToken = commandTokens.First();
        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        switch (commandName)
        {
            case "save":
            case "savepl":
                if (argTokens.Count != 0)
                {
                    var playlistName = string.Join(" ", argTokens.Select(t => t.Text));
                    arguments["playlistName"] = playlistName;
                }
                else
                {
                    return ParseResult<CommandNode?>.Fail("The 'save' command requires a playlist name.", commandToken.Position);
                }
                break;

            case "addnext":
            case "addend":
                break;

            case "addto":
            case "addtopos":
                if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
                {
                    if (int.TryParse(argTokens[0].Text, out int position))
                    {
                        arguments["position"] = position;
                    }
                    else
                    {
                        return ParseResult<CommandNode?>.Fail($"Invalid position '{argTokens[0].Text}' for 'addto' command.", argTokens[0].Position);
                    }
                }
                else
                {
                    return ParseResult<CommandNode?>.Fail("The 'addto' command requires a single number argument (e.g., '> addto 6').", commandToken.Position);
                }
                break;

            case "addall":
                if (argTokens.Count < 2)
                    return ParseResult<CommandNode?>.Fail("The 'addall' command requires indices and a position (e.g., '> addall (1,3) next').", commandToken.Position);

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
                {
                    int.TryParse(argTokens[0].Text, out albumIndex);
                }
                arguments["albumIndex"] = Math.Max(1, albumIndex);
                break;

            case "scrollto":
            case "deletedup":
            case "deleteall":
                break;
        }

        return ParseResult<CommandNode?>.Ok(new CommandNode(commandName, arguments));
    }

    private static List<SortDescription> CreateSortDescriptions(IReadOnlyList<Token> allDirectives)
    {
        var sortDescriptions = new List<SortDescription>();
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
                        var direction = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;
                        sortDescriptions.Add(new SortDescription(fieldDef, direction));
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
                {
                    int.TryParse(allDirectives[i + 1].Text, out count);
                }
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

        var shuffleToken = allDirectives[shuffleTokenIndex];
        int count = int.MaxValue;
        int currentIndex = shuffleTokenIndex + 1;

        if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Number)
        {
            if (int.TryParse(allDirectives[currentIndex].Text, out int parsedCount) && parsedCount > 0)
            {
                count = parsedCount;
            }
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
                var direction = SortDirection.Ascending;
                if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Desc)
                {
                    direction = SortDirection.Descending;
                }
                return new ShuffleNode(count, fieldDef, direction);
            }
        }

        return new ShuffleNode(count);
    }

    private static ParseResult<HashSet<int>> ParseIndexSet(List<Token> tokens)
    {
        var indices = new HashSet<int>();
        if (tokens.Count < 3 || tokens[0].Type != TokenType.LeftParen || tokens.Last().Type != TokenType.RightParen)
        {
            return ParseResult<HashSet<int>>.Fail("Invalid index format. Expected format like (1,3,5-9).", tokens.FirstOrDefault()?.Position ?? 0);
        }

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
                    {
                        indices.Add(j - 1);
                    }
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