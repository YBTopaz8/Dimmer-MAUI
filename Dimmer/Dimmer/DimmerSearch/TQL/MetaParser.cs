using Dimmer.DimmerSearch.Exceptions;

using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.TQL;
public record ParsedQueryResult(
    Func<SongModelView, bool> Predicate,
    IComparer<SongModelView> Comparer,
    LimiterClause? Limiter,
    IQueryNode? CommandNode, // We pass the node, not the evaluated action yet
    string? ErrorMessage,
    string? ErrorSuggestion = null
);

public  class QuerySegment
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
    /// <summary>
    /// A stateless utility class that orchestrates the TQL parsing process.
    /// It takes a raw query string and returns a complete, executable plan (ParsedQueryResult).
    /// </summary>
 
       private static readonly Dictionary<TokenType, SegmentType> _segmentTypeMap = new()
       {
            { TokenType.Include, SegmentType.Include }, { TokenType.Add, SegmentType.Include },
            { TokenType.Exclude, SegmentType.Exclude }, { TokenType.Remove, SegmentType.Exclude }
        };

        private static readonly HashSet<TokenType> _directiveTokens = new()
        { TokenType.Asc, TokenType.Desc, TokenType.Random, TokenType.Shuffle, TokenType.First, TokenType.Last };

        public static ParsedQueryResult Parse(string rawQuery)
        {
            try
            {
                const string commandInitiator = " >";
                int commandInitiatorIndex = rawQuery.IndexOf(commandInitiator);

                string filterQuery = (commandInitiatorIndex != -1) ? rawQuery[..commandInitiatorIndex] : rawQuery;
                string commandQuery = (commandInitiatorIndex != -1) ? rawQuery[(commandInitiatorIndex + commandInitiator.Length)..] : string.Empty;

                var filterTokens = Lexer.Tokenize(filterQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
                var segments = ParseSegmentsFromTokens(filterTokens);

                var predicate = CreateMasterPredicate(segments);
                var comparer = CreateSortComparer(segments);
                var limiter = CreateLimiterClause(segments);
                IQueryNode? commandNode = null;

                if (!string.IsNullOrWhiteSpace(commandQuery))
                {
                    var commandTokens = Lexer.Tokenize(commandQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
                    if (commandTokens.Count!=0)
                    {
                        commandNode = ParseAsCommand(commandTokens);
                    }
                }

                return new ParsedQueryResult(predicate, comparer, limiter, commandNode, null);
            }
            catch (ParsingException ex)
            {
                var match = Regex.Match(ex.Message, @"Unknown field '(\w+)'");
                string? suggestion = match.Success ? QueryValidator.SuggestCorrectField(match.Groups[1].Value) : null;
                return new ParsedQueryResult(s => false, new SongModelViewComparer(null), null, null, ex.Message, suggestion);
            }
            catch (Exception ex)
            {
                // TODO: Log the full exception 'ex' with your logger
                return new ParsedQueryResult(s => false, new SongModelViewComparer(null), null, null, "An unexpected error occurred during parsing. "+ex.Message, null);
            }
        }
        private static CommandNode? ParseAsCommand(List<Token> commandTokens)
    {
        var commandToken = commandTokens.FirstOrDefault();
        if (commandToken?.Type != TokenType.Identifier)
        {
            
            
            return null;
        }

        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        switch (commandName)
        {
            case "save":
                if (argTokens.Count!=0)
                {
                    
                    
                    var playlistName = string.Join(" ", argTokens.Select(t => t.Text));
                    arguments["playlistName"] = playlistName;
                }
                else
                {
                    throw new ParsingException("The 'save' command requires a playlist name.", commandToken.Position);
                }
                break;
            case "addnext":
                arguments["position"] = "next";
                break;
            case "addend":
                arguments["position"] = "end";
                break;
            case "deletedup":
                arguments["type"] = "duplicates";
                break;

            case "deleteall":
                arguments["type"] = "all";
                break;
                
            default:
                
                
                return null;
        }

        return new CommandNode(commandName, arguments);
    }

    private static List<QuerySegment> ParseSegmentsFromTokens(List<Token> allTokens)
    {
        var segments = new List<QuerySegment>();
        if (allTokens.Count==0)
        {
            segments.Add(new QuerySegment(SegmentType.Main, new List<Token>(), new List<Token>()));
            return segments;
        }

        int segmentStartIndex = 0;
        SegmentType currentSegmentType = SegmentType.Main;

        for (int i = 0; i < allTokens.Count; i++)
        {
            var token = allTokens[i];
            if (_segmentTypeMap.TryGetValue(token.Type, out var newSegmentType))
            {
                var segmentTokens = allTokens.GetRange(segmentStartIndex, i - segmentStartIndex);
                ProcessSegment(segmentTokens, currentSegmentType, segments);
                currentSegmentType = newSegmentType;
                segmentStartIndex = i + 1;
            }
        }

        var lastSegmentTokens = allTokens.GetRange(segmentStartIndex, allTokens.Count - segmentStartIndex);
        ProcessSegment(lastSegmentTokens, currentSegmentType, segments);
        return segments;
    }

    private static void ProcessSegment(List<Token> segmentTokens, SegmentType segmentType, List<QuerySegment> segments)
    {
        var filterTokens = new List<Token>();
        var directiveTokens = new List<Token>();

        for (int i = 0; i < segmentTokens.Count; i++)
        {
            var token = segmentTokens[i];
            bool isDirective = false;

            if ((token.Type == TokenType.Asc || token.Type == TokenType.Desc) && i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Identifier)
            {
                isDirective = true;
                directiveTokens.Add(token);
                directiveTokens.Add(segmentTokens[i + 1]);
                i++;
            }
            else if (_directiveTokens.Contains(token.Type))
            {
                isDirective = true;
                directiveTokens.Add(token);
                if (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(segmentTokens[i + 1]);
                    i++;
                }
            }

            if (!isDirective)
            {
                filterTokens.Add(token);
            }
        }
        segments.Add(new QuerySegment(segmentType, filterTokens, directiveTokens));
    }
    private static Func<SongModelView, bool> CreateMasterPredicate(IReadOnlyList<QuerySegment> segments)
    {
        var predicates = segments.Select(seg =>
        {
            if (seg.FilterTokens.Count==0)
                return (seg.SegmentType, (Func<SongModelView, bool>?)null);

            var ast = new AstParser(seg.FilterTokens).Parse();
            return (seg.SegmentType, new AstEvaluator().CreatePredicate(ast));

        }).Where(p => p.Item2 != null).ToList();

        var mainIncludes = predicates.Where(p => p.SegmentType == SegmentType.Main || p.SegmentType == SegmentType.Include).Select(p => p.Item2!).ToList();
        var excludes = predicates.Where(p => p.SegmentType == SegmentType.Exclude).Select(p => p.Item2!).ToList();

        return song =>
        {
            bool isIncluded = mainIncludes.Count==0 || mainIncludes.Any(p => p(song));
            if (!isIncluded)
                return false;

            bool isExcluded = excludes.Count!=0 && excludes.Any(p => p(song));
            return !isExcluded;
        };
    }

    private static IComparer<SongModelView> CreateSortComparer(IReadOnlyList<QuerySegment> segments)
    {
        var allDirectives = segments.SelectMany(s => s.DirectiveTokens).ToList();
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
                    i++;
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

    private static LimiterClause? CreateLimiterClause(IReadOnlyList<QuerySegment> segments)
    {
        var allDirectives = segments.SelectMany(s => s.DirectiveTokens).ToList();
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
                int count = 1; // Default for 'first' or 'last'
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                {
                    if (int.TryParse(allDirectives[i + 1].Text, out int parsedCount) && parsedCount > 0)
                    {
                        count = parsedCount;
                        i++;
                    }
                }
                limiter = new LimiterClause(limiterType.Value, count);
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
                        i++;
                    }
                }
                limiter = new LimiterClause(LimiterType.Random, count);
            }
        }
        return limiter;
    }
}