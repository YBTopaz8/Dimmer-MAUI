using System;
using System.Collections.Generic;
using System.Linq;

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

    public MetaParser(string rawQuery)
    {
        var allTokens = Lexer.Tokenize(rawQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        ParseSegmentsFromTokens(allTokens);
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