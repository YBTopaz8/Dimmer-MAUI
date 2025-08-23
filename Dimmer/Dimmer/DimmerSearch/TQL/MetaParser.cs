﻿using Dimmer.DimmerSearch.Exceptions;
using Dimmer.DimmerSearch.TQL.RealmSection;

using System.Text.RegularExpressions;

namespace Dimmer.DimmerSearch.TQL;
//public record ParsedQueryResult(
//    Func<SongModelView, bool> Predicate,
//    IComparer<SongModelView> Comparer,
//    LimiterClause? Limiter,
//    IQueryNode? CommandNode, // We pass the node, not the evaluated action yet
//    string? ErrorMessage,
//    string? ErrorSuggestion = null
//);

//public  class QuerySegment
//{
//    public SegmentType SegmentType { get; }
//    public List<Token> FilterTokens { get; }
//    public List<Token> DirectiveTokens { get; }
//    public QuerySegment(SegmentType type, List<Token> filter, List<Token> directives)
//    {
//        SegmentType = type;
//        FilterTokens = filter;
//        DirectiveTokens = directives;
//    }
//}

//public enum SegmentType { Main, Include, Exclude }

//public static class MetaParser
//{
//    private static T? FindNode<T>(IQueryNode node) where T : class, IQueryNode
//    {
//        if (node is T found)
//            return found;

//        if (node is LogicalNode logical)
//        {
//            return FindNode<T>(logical.Left) ?? FindNode<T>(logical.Right);
//        }
//        if (node is NotNode not)
//        {
//            return FindNode<T>(not.NodeToNegate);
//        }
//        return null; // Not found
//    }
//    /// <summary>
//    /// A stateless utility class that orchestrates the TQL parsing process.
//    /// It takes a raw query string and returns a complete, executable plan (ParsedQueryResult).
//    /// </summary>

//    private static readonly Dictionary<TokenType, SegmentType> _segmentTypeMap = new()
//       {

//            { TokenType.Include, SegmentType.Include }, { TokenType.Add, SegmentType.Include },
//            { TokenType.Exclude, SegmentType.Exclude }
//        };

//        private static readonly HashSet<TokenType> _directiveTokens = new()
//        { TokenType.Asc, TokenType.Desc, TokenType.Random, TokenType.Shuffle, TokenType.First, TokenType.Last };


//    public static RealmQueryPlan Parse(string rawQuery)
//    {
//        try
//        {
//            const string commandStart = " >>";
//            const string commandEnd = "!";

//            string filterQuery = rawQuery;
//            string commandQuery = string.Empty;

//            var filterTokens = Lexer.Tokenize(filterQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
//            var segments = ParseSegmentsFromTokens(filterTokens);

//            // 3. Build the Master AST from the Segments
//            IQueryNode masterAst = BuildMasterAstFromSegments(segments);

//            // 4. Split the Master AST for Hybrid Execution
//            var (databaseAst, inMemoryAst) = AstSplitter.Split(masterAst);

//            // 5. Generate outputs from the split ASTs
//            var rqlFilter = RqlGenerator.Generate(databaseAst);
//            var inMemoryPredicate = new AstEvaluator().CreatePredicate(inMemoryAst);

//            // 6. Parse Directives from ALL segments combined
//            var allDirectives = segments.SelectMany(s => s.DirectiveTokens).ToList();
//            var sortDescriptions = CreateSortDescriptions(allDirectives);
//            var limiter = CreateLimiterClause(allDirectives);
//            var shuffleNode = CreateShuffleNode(allDirectives);
//            IQueryNode? commandNode = null;
//            if (!string.IsNullOrWhiteSpace(commandQuery))
//            {
//                var commandTokens = Lexer.Tokenize(commandQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
//                if (commandTokens.Any())
//                {
//                    commandNode = ParseAsCommand(commandTokens);
//                }
//            }

//            return new RealmQueryPlan(
//                 rqlFilter,
//                 inMemoryPredicate,
//                 sortDescriptions,
//                 limiter,
//                 commandNode,
//                 shuffleNode
//             );
//        }
//        catch (ParsingException ex)
//        {
//            Func<SongModelView, bool> predicate = _ => false;
//            return new RealmQueryPlan("FALSEPREDICATE", predicate, new List<SortDescription>(), null, null, null, ex.Message, ex.Message);
//        }
//        catch (Exception ex)
//        {
//            Func<SongModelView, bool> predicate = _ => false;
//            return new RealmQueryPlan("FALSEPREDICATE", predicate, new List<SortDescription>(), null, null, null, ex.Message);
//        }
//    }
//    private static IQueryNode BuildMasterAstFromSegments(List<QuerySegment> segments)
//    {
//        var mainAndIncludeNodes = segments
//            .Where(s => s.SegmentType is SegmentType.Main or SegmentType.Include && s.FilterTokens.Any())
//            .Select(s => new AstParser(s.FilterTokens).Parse())
//            .ToList();

//        var excludeNodes = segments
//            .Where(s => s.SegmentType is SegmentType.Exclude && s.FilterTokens.Any())
//            .Select(s => new AstParser(s.FilterTokens).Parse())
//            .ToList();

//        // Combine all the "include" parts with OR
//        IQueryNode includeRoot = new ClauseNode("any", "matchall", ""); // Default to match nothing if no includes
//        if (mainAndIncludeNodes.Any())
//        {
//            includeRoot = mainAndIncludeNodes.First();
//            for (int i = 1; i < mainAndIncludeNodes.Count; i++)
//            {
//                includeRoot = new LogicalNode(includeRoot, LogicalOperator.Or, mainAndIncludeNodes[i]);
//            }
//        }

//        if (!excludeNodes.Any())
//        {
//            return includeRoot; // No excludes, we're done.
//        }

//        // Combine all the "exclude" parts with OR
//        IQueryNode excludeRoot = excludeNodes.First();
//        for (int i = 1; i < excludeNodes.Count; i++)
//        {
//            excludeRoot = new LogicalNode(excludeRoot, LogicalOperator.Or, excludeNodes[i]);
//        }

//        // Final structure is (Includes) AND NOT (Excludes)
//        return new LogicalNode(includeRoot, LogicalOperator.And, new NotNode(excludeRoot));
//    }
//    private static CommandNode? ParseAsCommand(List<Token> commandTokens)
//    {
//        if (commandTokens.Count == 0 || commandTokens.First().Type != TokenType.Identifier)
//        {
//            return null; // Not a valid command structure
//        }

//        var commandToken = commandTokens.First();
//        var commandName = commandToken.Text.ToLowerInvariant();
//        var arguments = new Dictionary<string, object>();
//        var argTokens = commandTokens.Skip(1).ToList(); // All tokens after the command name

//        try
//        {
//            switch (commandName)
//            {
//                case "save":
//                case "savepl": // Add alias
//                    if (argTokens.Count!=0)
//                    {
//                        // Join all remaining tokens to form the playlist name
//                        var playlistName = string.Join(" ", argTokens.Select(t => t.Text));
//                        arguments["playlistName"] = playlistName;
//                    }
//                    else
//                    {
//                        throw new ParsingException("The 'save' command requires a playlist name.", commandToken.Position);
//                    }
//                    break;

//                case "addnext":
//                    // No arguments needed
//                    break;

//                case "addend":
//                    // No arguments needed
//                    break;

//                case "addto":
//                case "addtopos": // Add alias
//                    if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
//                    {
//                        if (int.TryParse(argTokens[0].Text, out int position))
//                        {
//                            arguments["position"] = position;
//                        }
//                        else
//                        {
//                            throw new ParsingException($"Invalid position '{argTokens[0].Text}' for 'addto' command.", argTokens[0].Position);
//                        }
//                    }
//                    else
//                    {
//                        throw new ParsingException("The 'addto' command requires a single number argument (e.g., '> addto 6').", commandToken.Position);
//                    }
//                    break;
//                case "addall":
//                    if (argTokens.Count < 2)
//                    {
//                        throw new ParsingException("The 'addall' command requires indices and a position (e.g., '> addall (1,3) next').", commandToken.Position);
//                    }

//                    // Find the opening parenthesis of the index set.
//                    int openParenIndex = argTokens.FindIndex(t => t.Type == TokenType.LeftParen);
//                    if (openParenIndex == -1)
//                    {
//                        throw new ParsingException("Missing index set for 'addall' command.", commandToken.Position);
//                    }

//                    // Find the matching closing parenthesis.
//                    int closeParenIndex = argTokens.FindIndex(openParenIndex, t => t.Type == TokenType.RightParen);
//                    if (closeParenIndex == -1)
//                    {
//                        throw new ParsingException("Mismatched parentheses in 'addall' command.", openParenIndex);
//                    }
//                    if (closeParenIndex + 1 >= argTokens.Count)
//                    {
//                        throw new ParsingException("Missing position (e.g., 'next', 'end') after index set for 'addall'.", argTokens[closeParenIndex].Position);
//                    }
//                    // Extract the tokens for the index set and the final argument.
//                    var indexTokens = argTokens.GetRange(openParenIndex, closeParenIndex - openParenIndex + 1);

//                    var positionToken = argTokens[closeParenIndex + 1];

//                    // Parse the indices using our new helper.
//                    var parsedIndices = ParseIndexSet(indexTokens);
//                    arguments["indices"] = parsedIndices;
//                    arguments["position"] = positionToken.Text.ToLowerInvariant();
//                    break;
//                case "viewal":
//                    // Default to the first album if no number is given
//                    int albumIndex = 1;
//                    if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
//                    {
//                        int.TryParse(argTokens[0].Text, out albumIndex);
//                    }
//                    arguments["albumIndex"] = Math.Max(1, albumIndex); // Ensure index is at least 1
//                    break;

//                case "scrollto":
//                    // No arguments needed
//                    break;

//                case "deletedup":
//                case "deleteall":
//                    // No changes needed for these
//                    break;

//                default:
//                    // Let the evaluator handle it as an unrecognized command
//                    break;
//            }

//            return new CommandNode(commandName, arguments);
//        }
//        catch (ParsingException)
//        {
//            // Re-throw to be caught by the main parser error handler
//            throw;
//        }
//    }
//    private static List<QuerySegment> ParseSegmentsFromTokens(List<Token> allTokens)
//    {
//        var segments = new List<QuerySegment>();
//        if (allTokens.Count==0)
//        {
//            segments.Add(new QuerySegment(SegmentType.Main, new List<Token>(), new List<Token>()));
//            return segments;
//        }

//        int segmentStartIndex = 0;
//        SegmentType currentSegmentType = SegmentType.Main;

//        for (int i = 0; i < allTokens.Count; i++)
//        {
//            var token = allTokens[i];
//            if (_segmentTypeMap.TryGetValue(token.Type, out var newSegmentType))
//            {
//                var segmentTokens = allTokens.GetRange(segmentStartIndex, i - segmentStartIndex);
//                ProcessSegment(segmentTokens, currentSegmentType, segments);
//                currentSegmentType = newSegmentType;
//                segmentStartIndex = i + 1;
//            }
//        }

//        var lastSegmentTokens = allTokens.GetRange(segmentStartIndex, allTokens.Count - segmentStartIndex);
//        ProcessSegment(lastSegmentTokens, currentSegmentType, segments);
//        return segments;
//    }

//    private static void ProcessSegment(List<Token> segmentTokens, SegmentType segmentType, List<QuerySegment> segments)
//    {
//        var filterTokens = new List<Token>();
//        var directiveTokens = new List<Token>();

//        for (int i = 0; i < segmentTokens.Count; i++)
//        {
//            var token = segmentTokens[i];
//            bool isDirective = false;

//            if ((token.Type == TokenType.Asc || token.Type == TokenType.Desc) && i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Identifier)
//            {
//                isDirective = true;
//                directiveTokens.Add(token);
//                directiveTokens.Add(segmentTokens[i + 1]);
//                i++;
//            }
//            else if (_directiveTokens.Contains(token.Type))
//            {
//                isDirective = true;
//                directiveTokens.Add(token);
//                if (i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
//                {
//                    directiveTokens.Add(segmentTokens[i + 1]);
//                    i++;
//                }
//            }

//            if (!isDirective)
//            {
//                filterTokens.Add(token);
//            }
//        }
//        segments.Add(new QuerySegment(segmentType, filterTokens, directiveTokens));
//    }
//     private static List<SortDescription> CreateSortDescriptions(IReadOnlyList<QuerySegment> segments)
//    {
//        var allDirectives = segments.SelectMany(s => s.DirectiveTokens).ToList();
//        var sortDescriptions = new List<SortDescription>();
//        bool hasRandomSort = false;

//        for (int i = 0; i < allDirectives.Count; i++)
//        {
//            var token = allDirectives[i];
//            if (token.Type is TokenType.Random or TokenType.Shuffle)
//            {
//                hasRandomSort = true;
//            }
//            else if (token.Type is TokenType.Asc or TokenType.Desc)
//            {
//                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Identifier)
//                {
//                    var direction = token.Type == TokenType.Asc ? SortDirection.Ascending : SortDirection.Descending;
//                    string fieldAlias = allDirectives[i + 1].Text;

//                    if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
//                    {
//                        sortDescriptions.Add(new SortDescription(fieldDef, direction));
//                    }
//                    i++;
//                }
//            }

//        }

//        return sortDescriptions;
//    }


//    private static LimiterClause? CreateLimiterClause(IReadOnlyList<Token> allDirectives)
//    {
//        // This method now ONLY looks for 'first' or 'last'.
//        for (int i = 0; i < allDirectives.Count; i++)
//        {
//            var token = allDirectives[i];
//            LimiterType? limiterType = token.Type switch
//            {
//                TokenType.First => LimiterType.First,
//                TokenType.Last => LimiterType.Last,
//                _ => null
//            };

//            if (limiterType.HasValue)
//            {
//                int count = 1; // Default for 'first' or 'last'
//                if (i + 1 < allDirectives.Count && allDirectives[i + 1].Type == TokenType.Number)
//                {
//                    if (int.TryParse(allDirectives[i + 1].Text, out int parsedCount) && parsedCount > 0)
//                    {
//                        count = parsedCount;
//                    }
//                }
//                // Since we only find the first one, we can return immediately.
//                return new LimiterClause(limiterType.Value, count);
//            }
//        }

//        // No 'first' or 'last' directive was found.
//        return null;
//    }

//    

//}
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

            // 2. Tokenize and Segment the Filter part
            var filterTokens = Lexer.Tokenize(filterQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
            var segments = ParseSegmentsFromTokens(filterTokens);

            // 3. Build the Master AST from the Segments
            IQueryNode masterAst = BuildMasterAstFromSegments(segments);

            // 4. Split the Master AST for Hybrid (DB vs. In-Memory) Execution
            var (databaseAst, inMemoryAst) = AstSplitter.Split(masterAst);

            // 5. Generate outputs from the split ASTs
            var rqlFilter = RqlGenerator.Generate(databaseAst);
            var inMemoryPredicate = new AstEvaluator().CreatePredicate(inMemoryAst);

            // 6. Parse Directives from ALL segments combined
            var allDirectives = segments.SelectMany(s => s.DirectiveTokens).ToList();
            var sortDescriptions = CreateSortDescriptions(allDirectives);
            var limiter = CreateLimiterClause(allDirectives);
            var shuffleNode = CreateShuffleNode(allDirectives);

            // 7. Parse the Command part
            IQueryNode? commandNode = ParseCommand(commandQuery);

            // 8. Assemble the final, complete plan
            return new RealmQueryPlan(
                rqlFilter,
                inMemoryPredicate,
                sortDescriptions,
                limiter,
                commandNode,
                shuffleNode
            );
        }
        catch (ParsingException ex)
        {
            var match = Regex.Match(ex.Message, @"Unknown field '(\w+)'");
            string? suggestion = match.Success ? QueryValidator.SuggestCorrectField(match.Groups[1].Value) : null;
            return CreateErrorPlan(ex.Message, suggestion);
        }
        catch (Exception ex)
        {
            return CreateErrorPlan("An unexpected error occurred during parsing. " + ex.Message);
        }
    }

    private static RealmQueryPlan CreateErrorPlan(string message, string? suggestion = null)
    {
        Func<SongModelView, bool> predicate = _ => false; // Predicate that always returns false
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

    private static List<QuerySegment> ParseSegmentsFromTokens(List<Token> allTokens)
    {
        var segments = new List<QuerySegment>();
        if (!allTokens.Any())
        {
            segments.Add(new QuerySegment(SegmentType.Main, [], []));
            return segments;
        }

        int segmentStartIndex = 0;
        var currentSegmentType = SegmentType.Main;

        for (int i = 0; i < allTokens.Count; i++)
        {
            if (_segmentTypeMap.TryGetValue(allTokens[i].Type, out var newSegmentType))
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
            bool isDirective = _directiveTokens.Contains(token.Type);

            if (isDirective)
            {
                directiveTokens.Add(token);
                // Handle directives that take arguments (e.g., 'asc title', 'first 10')
                if ((token.Type is TokenType.Asc or TokenType.Desc) && i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Identifier)
                {
                    directiveTokens.Add(segmentTokens[++i]);
                }
                else if ((token.Type is TokenType.First or TokenType.Last or TokenType.Random or TokenType.Shuffle) && i + 1 < segmentTokens.Count && segmentTokens[i + 1].Type == TokenType.Number)
                {
                    directiveTokens.Add(segmentTokens[++i]);
                }
            }
            else
            {
                filterTokens.Add(token);
            }
        }
        segments.Add(new QuerySegment(segmentType, filterTokens, directiveTokens));
    }

    private static IQueryNode BuildMasterAstFromSegments(List<QuerySegment> segments)
    {
        var mainAndIncludeNodes = segments
            .Where(s => s.SegmentType is SegmentType.Main or SegmentType.Include && s.FilterTokens.Any())
            .Select(s => new AstParser(s.FilterTokens).Parse())
            .ToList();

        var excludeNodes = segments
            .Where(s => s.SegmentType is SegmentType.Exclude && s.FilterTokens.Any())
            .Select(s => new AstParser(s.FilterTokens).Parse())
            .ToList();

        // If there are no include clauses, the result is "match everything".
        // If there are no clauses at all, this will also correctly result in TRUEPREDICATE.
        IQueryNode includeRoot = new ClauseNode("any", "matchall", "");
        if (mainAndIncludeNodes.Any())
        {
            includeRoot = mainAndIncludeNodes.Aggregate((current, next) => new LogicalNode(current, LogicalOperator.Or, next));
        }

        if (!excludeNodes.Any())
        {
            return includeRoot;
        }

        IQueryNode excludeRoot = excludeNodes.Aggregate((current, next) => new LogicalNode(current, LogicalOperator.Or, next));

        return new LogicalNode(includeRoot, LogicalOperator.And, new NotNode(excludeRoot));
    }

    private static CommandNode? ParseCommand(string commandQuery)
    {
        if (string.IsNullOrWhiteSpace(commandQuery))
        {
            return null;
        }

        var commandTokens = Lexer.Tokenize(commandQuery).Where(t => t.Type != TokenType.EndOfFile).ToList();
        if (!commandTokens.Any() || commandTokens.First().Type != TokenType.Identifier)
        {
            return null;
        }

        // This is your existing `ParseAsCommand` logic, slightly refactored to fit here.
        var commandToken = commandTokens.First();
        var commandName = commandToken.Text.ToLowerInvariant();
        var arguments = new Dictionary<string, object>();
        var argTokens = commandTokens.Skip(1).ToList();

        switch (commandName)
        {
            case "save":
            case "savepl": // Add alias
                if (argTokens.Count!=0)
                {
                    // Join all remaining tokens to form the playlist name
                    var playlistName = string.Join(" ", argTokens.Select(t => t.Text));
                    arguments["playlistName"] = playlistName;
                }
                else
                {
                    throw new ParsingException("The 'save' command requires a playlist name.", commandToken.Position);
                }
                break;

            case "addnext":
                // No arguments needed
                break;

            case "addend":
                // No arguments needed
                break;

            case "addto":
            case "addtopos": // Add alias
                if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
                {
                    if (int.TryParse(argTokens[0].Text, out int position))
                    {
                        arguments["position"] = position;
                    }
                    else
                    {
                        throw new ParsingException($"Invalid position '{argTokens[0].Text}' for 'addto' command.", argTokens[0].Position);
                    }
                }
                else
                {
                    throw new ParsingException("The 'addto' command requires a single number argument (e.g., '> addto 6').", commandToken.Position);
                }
                break;
            case "addall":
                if (argTokens.Count < 2)
                {
                    throw new ParsingException("The 'addall' command requires indices and a position (e.g., '> addall (1,3) next').", commandToken.Position);
                }

                // Find the opening parenthesis of the index set.
                int openParenIndex = argTokens.FindIndex(t => t.Type == TokenType.LeftParen);
                if (openParenIndex == -1)
                {
                    throw new ParsingException("Missing index set for 'addall' command.", commandToken.Position);
                }

                // Find the matching closing parenthesis.
                int closeParenIndex = argTokens.FindIndex(openParenIndex, t => t.Type == TokenType.RightParen);
                if (closeParenIndex == -1)
                {
                    throw new ParsingException("Mismatched parentheses in 'addall' command.", openParenIndex);
                }
                if (closeParenIndex + 1 >= argTokens.Count)
                {
                    throw new ParsingException("Missing position (e.g., 'next', 'end') after index set for 'addall'.", argTokens[closeParenIndex].Position);
                }
                // Extract the tokens for the index set and the final argument.
                var indexTokens = argTokens.GetRange(openParenIndex, closeParenIndex - openParenIndex + 1);

                var positionToken = argTokens[closeParenIndex + 1];

                // Parse the indices using our new helper.
                var parsedIndices = ParseIndexSet(indexTokens);
                arguments["indices"] = parsedIndices;
                arguments["position"] = positionToken.Text.ToLowerInvariant();
                break;
            case "viewal":
                // Default to the first album if no number is given
                int albumIndex = 1;
                if (argTokens.Count == 1 && argTokens[0].Type == TokenType.Number)
                {
                    int.TryParse(argTokens[0].Text, out albumIndex);
                }
                arguments["albumIndex"] = Math.Max(1, albumIndex); // Ensure index is at least 1
                break;

            case "scrollto":
                // No arguments needed
                break;

            case "deletedup":
            case "deleteall":
                // No changes needed for these
                break;

            default:
                // Let the evaluator handle it as an unrecognized command
                break;
        }

        return new CommandNode(commandName, arguments);
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
                    i++; // Consume the field token
                }
            }
        }
        return sortDescriptions;
    }

    private static LimiterClause? CreateLimiterClause(IReadOnlyList<Token> allDirectives)
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
        // Find the 'shuffle' or 'random' token.
        var shuffleTokenIndex = allDirectives.ToList().FindIndex(t => t.Type is TokenType.Shuffle or TokenType.Random);
        if (shuffleTokenIndex == -1)
        {
            return null; // No shuffle directive found.
        }

        var shuffleToken = allDirectives[shuffleTokenIndex];
        int count = int.MaxValue;
        int currentIndex = shuffleTokenIndex + 1;

        // Check for a count (e.g., "shuffle 50")
        if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Number)
        {
            if (int.TryParse(allDirectives[currentIndex].Text, out int parsedCount) && parsedCount > 0)
            {
                count = parsedCount;
            }
            currentIndex++;
        }

        // Check for a bias (e.g., "shuffle by rating desc")
        if (currentIndex + 1 < allDirectives.Count &&
            allDirectives[currentIndex].Text.Equals("by", StringComparison.OrdinalIgnoreCase) &&
            allDirectives[currentIndex + 1].Type == TokenType.Identifier)
        {
            string fieldAlias = allDirectives[currentIndex + 1].Text;
            currentIndex += 2;

            if (FieldRegistry.FieldsByAlias.TryGetValue(fieldAlias, out var fieldDef))
            {
                // The bias has been found. Now check for an optional direction.
                var direction = SortDirection.Ascending; // Default bias direction
                if (currentIndex < allDirectives.Count && allDirectives[currentIndex].Type == TokenType.Desc)
                {
                    direction = SortDirection.Descending;
                }

                // Return a biased shuffle node
                return new ShuffleNode(count, fieldDef, direction);
            }
        }

        // If no valid bias was found, return a simple, pure random shuffle node.
        return new ShuffleNode(count);
    }
    private static HashSet<int> ParseIndexSet(List<Token> tokens)
    {
        var indices = new HashSet<int>();
        if (tokens.Count < 3 || tokens[0].Type != TokenType.LeftParen || tokens.Last().Type != TokenType.RightParen)
        {
            throw new ParsingException("Invalid index format. Expected format like (1,3,5-9).", tokens.FirstOrDefault()?.Position ?? 0);
        }

        // We only care about the tokens inside the parentheses.
        var innerTokens = tokens.Skip(1).Take(tokens.Count - 2).ToList();

        // Use a simple loop to process numbers, commas, and hyphens.
        for (int i = 0; i < innerTokens.Count; i++)
        {
            var currentToken = innerTokens[i];

            if (currentToken.Type == TokenType.Number)
            {
                if (!int.TryParse(currentToken.Text, out int index))
                {
                    throw new ParsingException($"Invalid number '{currentToken.Text}' in index set.", currentToken.Position);
                }

                // Check if the next token is a hyphen for a range.
                if (i + 2 < innerTokens.Count && innerTokens[i + 1].Type == TokenType.Minus && innerTokens[i + 2].Type == TokenType.Number)
                {
                    if (!int.TryParse(innerTokens[i + 2].Text, out int endIndex))
                    {
                        throw new ParsingException($"Invalid end range number '{innerTokens[i+2].Text}'.", innerTokens[i+2].Position);
                    }

                    for (int j = index; j <= endIndex; j++)
                    {
                        // Convert from 1-based (user input) to 0-based (list index).
                        indices.Add(j - 1);
                    }
                    i += 2; // Skip the hyphen and the end number.
                }
                else
                {
                    // It's a single number.
                    indices.Add(index - 1);
                }
            }
            else if (currentToken.Type == TokenType.Comma)
            {
                // Commas are separators, we can just continue.
                continue;
            }
            else
            {
                throw new ParsingException($"Unexpected token '{currentToken.Text}' in index set.", currentToken.Position);
            }
        }

        return indices;
    }
}