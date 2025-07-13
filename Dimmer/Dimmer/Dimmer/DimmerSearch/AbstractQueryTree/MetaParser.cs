using Dimmer.DimmerSearch.AbstractQueryTree.NL;

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

    public IReadOnlyList<QuerySegment> GetSegments() => _segments.AsReadOnly();
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



        for (int i = 0; i < segmentTokens.Count; i++)
        {
            var token = segmentTokens[i];

            // --- START OF NEW LOGIC ---

            bool isSortDirective = false;
            // A token is ONLY a sort directive if it's asc/desc AND followed by a field name.
            if ((token.Type == TokenType.Asc || token.Type == TokenType.Desc) &&
                (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Identifier))
            {
                isSortDirective = true;
                directiveTokens.Add(token);                  // Add 'asc' or 'desc'
                directiveTokens.Add(segmentTokens[i + 1]);   // Add the field identifier
                i++;                                         // IMPORTANT: Skip the field token
            }
            // Handle other non-sort directives (`first`, `last`, `random`, `shuffle`)
            else if (_directiveTokens.Contains(token.Type) && token.Type != TokenType.Asc && token.Type != TokenType.Desc)
            {
                isSortDirective = true; // Still a directive, just not a sorting one we check above
                directiveTokens.Add(token);
                // Grab the number for `first`, `last`, `random`
                if (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(segmentTokens[i + 1]);
                    i++; // Skip the number token
                }
            }

            if (!isSortDirective && token.Type != TokenType.EndOfFile)
            {
                // If it was not a valid, complete directive, it's a filter token.
                filterTokens.Add(token);
            }
            // --- END OF NEW LOGIC ---
        }

        _segments.Add(new QuerySegment(segmentType, filterTokens, directiveTokens));
    }

    public Func<SongModelView, bool> CreateMasterPredicate()
    {
        var predicates = _segments.Select(seg =>
        {
            // This part is now correct because QuerySegment holds tokens.
            if (seg.FilterTokens.Count==0)
                return (seg.SegmentType, (Func<SongModelView, bool>)null);

            var ast = new AstParser(seg.FilterTokens).Parse(); // Uses the new AstParser constructor
            return (seg.SegmentType, new AstEvaluator().CreatePredicate(ast));

        }).Where(p => p.Item2 != null).ToList();

        var mainIncludes = predicates.Where(p => p.SegmentType == "MAIN" || p.SegmentType == "INCLUDE" || p.SegmentType == "ADD").Select(p => p.Item2).ToList();
        var excludes = predicates.Where(p => p.SegmentType == "EXCLUDE" || p.SegmentType == "REMOVE").Select(p => p.Item2).ToList();

        return song =>
        {
            bool isIncluded = mainIncludes.Count==0 || mainIncludes.Any(p => p(song));
            if (!isIncluded)
                return false;

            bool isExcluded = excludes.Count!=0 && excludes.Any(p => p(song));
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
            if (token.Type == TokenType.Random || token.Type == TokenType.Shuffle)
            {
                hasRandomSort = true;
                // We don't need to check for a field name here, random applies to the whole list
            }
            else
            if (token.Type == TokenType.Asc || token.Type == TokenType.Desc)
            {
                // Because of our new ProcessPart logic, we can be CERTAIN
                // that the next token is the Identifier field name.
                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Identifier)
                {
                    var direction = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;
                    string fieldAlias = allDirectives[i + 1].Text;

                    // Use the shared mapping to get the real property name
                    if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
                    {
                        sortDescriptions.Add(new SortDescription(fieldDef.PrimaryName, direction));
                    }
                    // We don't need an 'else' because an invalid field alias
                    // should just be ignored.

                    i++; // Consume the field token so we don't process it again
                }
            }
        }
        if (hasRandomSort)
        {
            // Return a comparer with a single random sort description
            return new SongModelViewComparer(new List<SortDescription>
        {
            new SortDescription("RandomSort", SortDirection.Random)
        });
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

            // Case 1: Handle `first <num>` and `last <num>`
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
                // If no number, it's not a valid limiter, so we just ignore it.
                continue;
            }

            // Case 2: Handle `shuffle` and `random`
            if (token.Type == TokenType.Shuffle || token.Type == TokenType.Random)
            {
                // By default, it's a full shuffle.
                // int.MaxValue is a sentinel to mean "take all items, but randomized".
                int count = int.MaxValue;

                // Check if it's `random <num>`
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
