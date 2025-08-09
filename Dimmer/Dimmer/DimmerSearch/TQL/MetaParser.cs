using Dimmer.DimmerSearch.Exceptions;

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

public class MetaParser
{
    private static readonly Dictionary<TokenType, SegmentType> _segmentTypeMap = new()
    {
        { TokenType.Include, SegmentType.Include },
        { TokenType.Add,     SegmentType.Include },
        { TokenType.Exclude, SegmentType.Exclude },
        { TokenType.Remove,  SegmentType.Exclude }
    };

    public IReadOnlyList<QuerySegment> GetSegments() => _segments.AsReadOnly();
    private readonly List<QuerySegment> _segments = new();

    private static readonly HashSet<TokenType> _directiveTokens = new()
        { TokenType.Asc, TokenType.Desc, TokenType.Random, TokenType.Shuffle, TokenType.First, TokenType.Last };
    public IQueryNode? ParsedCommand { get; private set; }

    

    public MetaParser(string rawQuery)
    {
        var allTokens = Lexer.Tokenize(rawQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();

        int commandStartFenceIndex = -1;
        int commandEndFenceIndex = -1;
        for (int i = 0; i < allTokens.Count; i++)
        {
            if (allTokens[i].Type == TokenType.GreaterThan) 
            {
                commandStartFenceIndex = i;
                // Once we find the start, look for the end from this point forward.
                for (int j = i + 1; j < allTokens.Count; j++)
                {
                    if (allTokens[j].Type == TokenType.Bang) // Using '!' as the end fence
                    {
                        commandEndFenceIndex = j;
                        break;
                    }
                }
            }
        }

        List<Token> filterAndDirectiveTokens;

        // 2. If a command was found, split the token list
        if (commandStartFenceIndex != -1 && commandEndFenceIndex != -1)
        {
            // The tokens FOR the command are the ones BETWEEN the fences.
            int commandContentStartIndex = commandStartFenceIndex + 1;
            int commandContentCount = commandEndFenceIndex - commandContentStartIndex;

            if (commandContentCount < 0) // Case: >!
            {
                throw new ParsingException("Command cannot be empty.", commandStartFenceIndex);
            }

            var commandTokens = allTokens.GetRange(commandContentStartIndex, commandContentCount);
            ParsedCommand = ParseAsFencedCommand(commandTokens);

            // The tokens for the filter are everything BEFORE and AFTER the fence.
            filterAndDirectiveTokens = allTokens.GetRange(0, commandStartFenceIndex);
            if (commandEndFenceIndex + 1 < allTokens.Count)
            {
                filterAndDirectiveTokens.AddRange(allTokens.GetRange(commandEndFenceIndex + 1, allTokens.Count - (commandEndFenceIndex + 1)));
            }
        }
        else
        {
            // No command fence found, all tokens are for filtering/sorting.
            filterAndDirectiveTokens = allTokens;
        }

        // 3. Parse the filter/directive part as before.
        ParseSegmentsFromTokens(filterAndDirectiveTokens);
    }
    private IQueryNode? ParseAsFencedCommand(List<Token> commandTokens)
    {
        if (commandTokens.Count == 0)
        {
            throw new ParsingException("Command cannot be empty.", -1); // Position can be improved if needed
        }

        var commandToken = commandTokens[0];
        if (commandToken.Type != TokenType.Identifier)
        {
            throw new ParsingException($"Expected a command keyword (like 'save') but found '{commandToken.Text}'.", commandToken.Position);
        }

        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        // This parsing logic becomes more powerful. It can handle more complex arguments.
        switch (commandName)
        {
            case "save":
                // The argument is the rest of the tokens, joined together.
                // This allows for names with spaces without needing quotes inside the fence.
                // > save my awesome playlist ! is now valid.
                if (argTokens.Any())
                {
                    // Reconstruct the argument string from the tokens.
                    var playlistName = string.Join(" ", argTokens.Select(t => t.Text));
                    arguments["playlistName"] = playlistName;
                }
                else
                {
                    throw new ParsingException("The 'save' command requires a playlist name.", commandToken.Position);
                }
                break;

            case "addtonext":
                // A command with no arguments
                if (argTokens.Any())
                {
                    throw new ParsingException("The 'addtonext' command does not take arguments.", argTokens[0].Position);
                }
                // No arguments to add, just the command itself is enough.
                break;

            // Add other commands like 'delete', 'addtoqueue' here
            default:
                throw new ParsingException($"Unknown command '{commandName}'.", commandToken.Position);
        }

        return new CommandNode(commandName, arguments);
    }
    private IQueryNode? ParseAsCommand(List<Token> commandTokens)
    {
        if (commandTokens.Count == 0)
        {
            // This is an error, e.g., "artist:tool > "
            // You could throw a ParsingException here.
            throw new ParsingException("Command expected after '>'.", commandTokens.LastOrDefault()?.Position ?? -1);
        }

        var commandToken = commandTokens[0];
        // The command itself must be an Identifier (e.g., 'save')
        if (commandToken.Type != TokenType.Identifier)
        {
            throw new ParsingException($"Expected a command keyword (like 'save') but found '{commandToken.Text}'.", commandToken.Position);
        }

        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        // Improved argument parsing
        switch (commandName)
        {
            case "save":
                if (argTokens.Count == 1 && (argTokens[0].Type == TokenType.Identifier || argTokens[0].Type == TokenType.StringLiteral))
                {
                    arguments["playlistName"] = argTokens[0].Text;
                }
                else
                {
                    throw new ParsingException("The 'save' command requires a single playlist name.", commandToken.Position);
                }
                break;
            // Add other commands like 'delete', 'addtoqueue' here
            default:
                throw new ParsingException($"Unknown command '{commandName}'.", commandToken.Position);
        }

        return new CommandNode(commandName, arguments);
    }

    private void ParseSegmentsFromTokens(List<Token> allTokens)
    {
        if (allTokens.Count == 0)
        {
            _segments.Add(new QuerySegment(SegmentType.Main, new List<Token>(), new List<Token>()));
            return;
        }

        int segmentStartIndex = 0;
        // FIX: The type is now correctly SegmentType enum.
        SegmentType currentSegmentType = SegmentType.Main;

        for (int i = 0; i < allTokens.Count; i++)
        {
            var token = allTokens[i];

            // Check if the current token is a keyword that starts a new segment.
            if (_segmentTypeMap.TryGetValue(token.Type, out var newSegmentType))
            {
                // Process the segment that just ended.
                var segmentTokens = allTokens.GetRange(segmentStartIndex, i - segmentStartIndex);
                ProcessSegment(segmentTokens, currentSegmentType);

                // Set up for the next segment.
                currentSegmentType = newSegmentType;
                segmentStartIndex = i + 1;
            }
        }

        // Process the final segment after the loop.
        var lastSegmentTokens = allTokens.GetRange(segmentStartIndex, allTokens.Count - segmentStartIndex);
        ProcessSegment(lastSegmentTokens, currentSegmentType);
    }

    private void ProcessSegment(List<Token> segmentTokens, SegmentType segmentType)
    {
        var filterTokens = new List<Token>();
        var directiveTokens = new List<Token>();

        for (int i = 0; i < segmentTokens.Count; i++)
        {
            var token = segmentTokens[i];
            bool isDirective = false;

            // Logic to separate filter tokens from directive tokens
            if ((token.Type == TokenType.Asc || token.Type == TokenType.Desc) && i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Identifier)
            {
                isDirective = true;
                directiveTokens.Add(token);
                directiveTokens.Add(segmentTokens[i + 1]);
                i++; // Skip field token
            }
            else if (_directiveTokens.Contains(token.Type) && !isDirective)
            {
                isDirective = true;
                directiveTokens.Add(token);
                if (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(segmentTokens[i + 1]);
                    i++; // Skip number token
                }
            }

            if (!isDirective)
            {
                filterTokens.Add(token);
            }
        }
        _segments.Add(new QuerySegment(segmentType, filterTokens, directiveTokens));
    }

    public Func<SongModelView, bool> CreateMasterPredicate()
    {
        var predicates = _segments.Select(seg =>
        {
            if (seg.FilterTokens.Count == 0)
                return (seg.SegmentType, (Func<SongModelView, bool>)null);

            var ast = new AstParser(seg.FilterTokens).Parse();
            return (seg.SegmentType, new AstEvaluator().CreatePredicate(ast));

        }).Where(p => p.Item2 != null).ToList();

        var mainIncludes = predicates.Where(p => p.SegmentType == SegmentType.Main || p.SegmentType == SegmentType.Include).Select(p => p.Item2).ToList();
        var excludes = predicates.Where(p => p.SegmentType == SegmentType.Exclude).Select(p => p.Item2).ToList();

        return song =>
        {
            bool isIncluded = mainIncludes.Count == 0 || mainIncludes.Any(p => p(song));
            if (!isIncluded)
                return false;

            bool isExcluded = excludes.Count != 0 && excludes.Any(p => p(song));
            return !isExcluded;
        };
    }

    public IComparer<SongModelView> CreateSortComparer()
    {
        var allDirectives = _segments.SelectMany(s => s.DirectiveTokens).ToList();
        var sortDescriptions = new List<SortDescription>();
        bool hasRandomSort = false;

        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            if (token.Type is TokenType.Random or TokenType.Shuffle)
            {
                hasRandomSort = true;
            }
            else if (token.Type is TokenType.Asc or TokenType.Desc)
            {
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Identifier)
                {
                    var direction = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;
                    string fieldAlias = allDirectives[i + 1].Text;

                    if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
                    {
                        sortDescriptions.Add(new SortDescription(fieldDef, direction));
                    }
                    i++; // Consume the field token
                }
            }
        }

        if (hasRandomSort)
        {
            var randomFieldDef = new FieldDefinition("RandomSort", FieldType.Text, Array.Empty<string>(), "A placeholder for random sorting", "random");
            return new SongModelViewComparer(new List<SortDescription> { new SortDescription(randomFieldDef, SortDirection.Random) });
        }

        return new SongModelViewComparer(sortDescriptions);
    }

    public LimiterClause? CreateLimiterClause()
    {
        var allDirectives = _segments.SelectMany(s => s.DirectiveTokens).ToList();
        LimiterClause? limiter = null;

        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            LimiterType? limiterType = token.Type switch
            {
                TokenType.First => LimiterType.First,
                TokenType.Last => LimiterType.Last,
                _ => null
            };

            if (limiterType.HasValue)
            {
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                {
                    if (int.TryParse(allDirectives[i + 1].Text, out int count) && count > 0)
                    {
                        limiter = new LimiterClause(limiterType.Value, count);
                        i++; // Consume the number token
                    }
                }
                continue;
            }

            if (token.Type is TokenType.Shuffle or TokenType.Random)
            {
                int count = int.MaxValue;
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                {
                    if (int.TryParse(allDirectives[i + 1].Text, out int parsedCount) && parsedCount > 0)
                    {
                        count = parsedCount;
                        i++; // Consume the number token
                    }
                }
                limiter = new LimiterClause(LimiterType.Random, count);
            }
        }
        return limiter;
    }
}