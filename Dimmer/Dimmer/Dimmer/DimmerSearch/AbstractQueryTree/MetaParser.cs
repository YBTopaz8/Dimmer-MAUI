using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.AbstractQueryTree;




public class QuerySegment
{
    public string SegmentType { get; }
    public List<Token> FilterTokens { get; }
    public List<Token> DirectiveTokens { get; }
    public QuerySegment(string type, List<Token> filter, List<Token> directives)
    {
        SegmentType = type;
        FilterTokens = filter;
        DirectiveTokens = directives;
    }
}

public class MetaParser
{
    private readonly List<QuerySegment> _segments = new();
    private static readonly HashSet<TokenType> _directiveTokens = new()
        { TokenType.Asc, TokenType.Desc, TokenType.Random, TokenType.Shuffle, TokenType.First, TokenType.Last };

    public MetaParser(string rawQuery)
    {
        var allTokens = Lexer.Tokenize(rawQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        ParseSegmentsFromTokens(allTokens);
    }

    private void ParseSegmentsFromTokens(List<Token> allTokens)
    {
        var segmentStartIndices = new List<(int index, string type)> { (0, "MAIN") };
        var metaKeywords = new HashSet<TokenType> { TokenType.Include, TokenType.Add, TokenType.Exclude, TokenType.Remove };

        for (int i = 0; i < allTokens.Count; i++)
        {
            if (metaKeywords.Contains(allTokens[i].Type))
            {
                segmentStartIndices.Add((i, allTokens[i].Text.ToUpperInvariant()));
            }
        }

        for (int i = 0; i < segmentStartIndices.Count; i++)
        {
            var (startIndex, type) = segmentStartIndices[i];

            // The tokens for this segment start after the keyword (if it's not the first segment)
            var segmentTokenStartIndex = (type == "MAIN") ? startIndex : startIndex + 1;

            var endIndex = (i + 1 < segmentStartIndices.Count)
                ? segmentStartIndices[i + 1].index
                : allTokens.Count;

            var segmentTokens = allTokens.GetRange(segmentTokenStartIndex, endIndex - segmentTokenStartIndex);
            ProcessPart(segmentTokens, type);
        }
    }


    private void ProcessPart(List<Token> segmentTokens, string segmentType)
    {
        var filterTokens = new List<Token>();
        var directiveTokens = new List<Token>();


        // We are GIVEN the tokens for this part. We must not re-tokenize.
        // Loop over the 'segmentTokens' list that was passed in.
        for (int i = 0; i < segmentTokens.Count; i++)
        {
            var token = segmentTokens[i];
            if (_directiveTokens.Contains(token.Type))
            {
                directiveTokens.Add(token);
                // Also grab the number that follows, if it exists
                if (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(segmentTokens[i + 1]);
                    i++; // Skip the number token in the next iteration
                }
            }
            else if (token.Type != TokenType.EndOfFile)
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
            // This part is now correct because QuerySegment holds tokens.
            if (!seg.FilterTokens.Any())
                return (seg.SegmentType, (Func<SongModelView, bool>)null);

            var ast = new AstParser(seg.FilterTokens).Parse(); // Uses the new AstParser constructor
            return (seg.SegmentType, new AstEvaluator().CreatePredicate(ast));

        }).Where(p => p.Item2 != null).ToList();

        var mainIncludes = predicates.Where(p => p.SegmentType == "MAIN" || p.SegmentType == "INCLUDE" || p.SegmentType == "ADD").Select(p => p.Item2).ToList();
        var excludes = predicates.Where(p => p.SegmentType == "EXCLUDE" || p.SegmentType == "REMOVE").Select(p => p.Item2).ToList();

        return song =>
        {
            bool isIncluded = !mainIncludes.Any() || mainIncludes.Any(p => p(song));
            if (!isIncluded)
                return false;

            bool isExcluded = excludes.Any() && excludes.Any(p => p(song));
            return !isExcluded;
        };
    }

    public IComparer<SongModelView> CreateSortComparer()
    {
        var allDirectives = _segments.SelectMany(s => s.DirectiveTokens).ToList();
        var sortDescriptions = new List<SortDescription>();

        // "Last one wins" logic: we find the last sort instruction and build from there.
        int lastSortInstructionIndex = -1;
        for (int i = 0; i < allDirectives.Count; i++)
        {
            var type = allDirectives[i].Type;
            if (type == TokenType.Asc || type == TokenType.Desc || type == TokenType.Shuffle || type == TokenType.Random)
            {
                // If we find 'random' followed by a number, it's a limiter, not a sorter. Ignore it here.
                if (type == TokenType.Random && i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                {
                    continue;
                }
                lastSortInstructionIndex = i;
            }
        }

        if (lastSortInstructionIndex == -1)
        {
            return new SongModelViewComparer(null); // No sort specified, use default.
        }

        // Check if the last sort instruction was random/shuffle
        var lastToken = allDirectives[lastSortInstructionIndex];
        if (lastToken.Type == TokenType.Shuffle || lastToken.Type == TokenType.Random)
        {
            sortDescriptions.Add(new SortDescription("Random", SortDirection.Ascending));
            return new SongModelViewComparer(sortDescriptions);
        }

        // Otherwise, it's an asc/desc sort. Build the list of fields.
        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            if (token.Type == TokenType.Asc || token.Type == TokenType.Desc)
            {
                var direction = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;

                // The next token should be the field name (Identifier)
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Identifier)
                {
                    string fieldAlias = allDirectives[i + 1].Text;
                    if (AstEvaluator.FieldMappings.TryGetValue(fieldAlias, out var propertyName))
                    {
                        sortDescriptions.Add(new SortDescription(propertyName, direction));
                        i++; // Consume the field token
                    }
                }
                else // Default to Title if no field is specified
                {
                    sortDescriptions.Add(new SortDescription("Title", direction));
                }
            }
        }

        return new SongModelViewComparer(sortDescriptions);
    }

    public LimiterClause? CreateLimiterClause()
    {
        var allDirectives = _segments.SelectMany(s => s.DirectiveTokens).ToList();
        LimiterClause? limiter = null; // "Last one wins"

        for (int i = 0; i < allDirectives.Count; i++)
        {
            var token = allDirectives[i];
            LimiterType? limiterType = token.Type switch
            {
                TokenType.First => LimiterType.First,
                TokenType.Last => LimiterType.Last,
                TokenType.Random => LimiterType.Random,
                _ => null
            };

            if (limiterType.HasValue)
            {
                // The next token MUST be a number
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
                {
                    if (int.TryParse(allDirectives[i + 1].Text, out int count) && count > 0)
                    {
                        limiter = new LimiterClause(limiterType.Value, count);
                        i++; // Consume the number token
                    }
                    else
                    {
                        throw new Exception($"Invalid number for '{token.Text}' directive: '{allDirectives[i + 1].Text}'.");
                    }
                }
                else
                {
                    throw new Exception($"The '{token.Text}' directive must be followed by a number.");
                }
            }
        }

        return limiter;
    }
}
