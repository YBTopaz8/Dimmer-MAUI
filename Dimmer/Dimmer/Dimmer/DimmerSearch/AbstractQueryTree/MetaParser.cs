using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.AbstractQueryTree;




public class QuerySegment
{
    public string SegmentType { get; }
    public string FilterQuery { get; }
    public List<Token> DirectiveTokens { get; }
    public QuerySegment(string type, string query, List<Token> directives)
    {
        SegmentType = type;
        FilterQuery = query.Trim();
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
        ParseSegments(rawQuery);
    }

    private void ParseSegments(string rawQuery)
    {
        var keywords = new[] { "include", "add", "exclude", "remove" };
        var pattern = $@"\s+(?=(?:{string.Join("|", keywords.Select(Regex.Escape))})\b)";
        var parts = Regex.Split(rawQuery, pattern, RegexOptions.IgnoreCase);

        ProcessPart(parts[0], "MAIN");
        for (int i = 1; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            var keywordMatch = Regex.Match(part, @"^\w+");
            var keyword = keywordMatch.Value.ToUpperInvariant();
            var restOfPart = part.Substring(keywordMatch.Length).Trim();
            ProcessPart(restOfPart, keyword);
        }
    }

    private void ProcessPart(string part, string segmentType)
    {
        var allTokens = Lexer.Tokenize(part);
        var filterTokens = new List<Token>();
        var directiveTokens = new List<Token>();

        for (int i = 0; i < allTokens.Count; i++)
        {
            var token = allTokens[i];
            if (_directiveTokens.Contains(token.Type))
            {
                directiveTokens.Add(token);
                // Also grab the number that follows, if it exists
                if (i + 1 < allTokens.Count && allTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(allTokens[i + 1]);
                    i++; // Skip the number token in the next iteration
                }
            }
            else if (token.Type != TokenType.EndOfFile)
            {
                filterTokens.Add(token);
            }
        }

        string filterQuery = string.Join(" ", filterTokens.Select(t => t.Text));
        _segments.Add(new QuerySegment(segmentType, filterQuery, directiveTokens));
    }

    public Func<SongModelView, bool> CreateMasterPredicate()
    {
        var predicates = _segments.Select(seg =>
        {
            if (string.IsNullOrWhiteSpace(seg.FilterQuery))
                return (seg.SegmentType, (Func<SongModelView, bool>)null);
            var ast = new AstParser(seg.FilterQuery).Parse();
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
        // Logic to parse sort directives from allDirectives...
        // This remains largely the same as the previous version.
        return new SongModelViewComparer(null); // Placeholder for your sort logic
    }

    public LimiterClause? CreateLimiterClause()
    {
        var allDirectives = _segments.SelectMany(s => s.DirectiveTokens).ToList();
        // Logic to parse limiter clauses from allDirectives...
        // This remains largely the same as the previous version.
        return null; // Placeholder for your limiter logic
    }
}
