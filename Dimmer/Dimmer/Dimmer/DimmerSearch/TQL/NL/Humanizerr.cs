using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree.NL;
public static class QueryHumanizer
{
    // A dictionary to make field names more readable
    private static readonly Dictionary<string, string> _friendlyFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        {"any", "Anything"}, {"t", "Title"}, {"title", "Title"}, {"ar", "Artist"}, {"artist", "Artist"},
        {"al", "Album"}, {"album", "Album"}, {"genre", "Genre"}, {"year", "Year"}, {"rating", "Rating"},
        {"len", "Duration"}, {"fav", "Favorites"}, {"haslyrics", "Has Lyrics"}, {"synced", "Has Synced Lyrics"},
        {"note", "Notes"}, {"comment", "Notes"}
    };

    private static readonly HashSet<string> _booleanFields = new() { "fav", "haslyrics", "synced" };

    public static string Humanize(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Showing all songs.";
        }

        try
        {
            var metaParser = new MetaParser(query);
            var mainPredicate = metaParser.CreateMasterPredicate(); // We need to run this to parse segments
            var comparer = metaParser.CreateSortComparer(); // And this for directives
            var limiter = metaParser.CreateLimiterClause();

            // We need to access the private segments. Let's make a small modification to MetaParser...
            // For now, let's assume we can get the segments.
            // (See MetaParser modification below)
            var segments = metaParser.GetSegments();

            var mainFilters = segments.Where(s => s.SegmentType is "MAIN" or "INCLUDE" or "ADD").ToList();
            var excludeFilters = segments.Where(s => s.SegmentType is "EXCLUDE" or "REMOVE").ToList();

            var sb = new StringBuilder("Showing songs ");

            if (mainFilters.Any(s => s.FilterTokens.Any()))
            {
                sb.Append("where ");
                var mainClauses = mainFilters.Select(s => HumanizeSegment(s.FilterTokens)).Where(s => !string.IsNullOrEmpty(s));
                sb.Append(string.Join(" or ", mainClauses));
            }
            else
            {
                sb.Append("from the entire library");
            }

            if (excludeFilters.Any(s => s.FilterTokens.Any()))
            {
                sb.Append(", excluding those where ");
                var excludeClauses = excludeFilters.Select(s => HumanizeSegment(s.FilterTokens)).Where(s => !string.IsNullOrEmpty(s));
                sb.Append(string.Join(" or ", excludeClauses));
            }

            // Humanize Sorter
            var sortDescriptions = (comparer as SongModelViewComparer)?.SortDescriptions;
            if (sortDescriptions != null && sortDescriptions.Any())
            {
                sb.Append(", sorted by ");
                var sortParts = sortDescriptions.Select(sd => $"{sd.PropertyName} {(sd.Direction == SortDirection.Ascending ? "ascending" : "descending")}");
                sb.Append(string.Join(", then by ", sortParts));
            }

            // Humanize Limiter
            if (limiter != null)
            {
                sb.Append(", ");
                // Access Type and Count directly on the limiter object
                sb.Append(limiter.Type switch
                {
                    LimiterType.First => $"taking the first {limiter.Count}",
                    LimiterType.Last => $"taking the last {limiter.Count}",
                    // Note: Your LimiterType enum doesn't have Random, I've used the one from your previous code.
                    // If you intend to have Random, add it to your LimiterType enum.
                    LimiterType.Random when limiter.Count == int.MaxValue => "in a random order",
                    LimiterType.Random => $"taking {limiter.Count} random songs",
                    _ => ""
                });
            }
            sb.Append('.');
            return sb.ToString();
        }
        catch (Exception)
        {
            return "Could not understand the query.";
        }
    }

    private static string HumanizeSegment(List<Token> tokens)
    {
        if (!tokens.Any())
            return "";

        // This is a simplified humanizer. A full one would build a phrase tree.
        // For now, we just describe the clauses.
        var parser = new AstParser(tokens);
        var ast = parser.Parse();
        return HumanizeNode(ast);
    }

    private static string HumanizeNode(IQueryNode node) => node switch
    {
        LogicalNode n => $"{HumanizeNode(n.Left)} {n.Operator.ToString().ToLower()} {HumanizeNode(n.Right)}",
        NotNode n => $"not ({HumanizeNode(n.NodeToNegate)})",
        ClauseNode n => HumanizeClause(n),
        _ => ""
    };

    private static string HumanizeClause(ClauseNode clause)
    {
        var field = _friendlyFieldNames.GetValueOrDefault(clause.Field, clause.Field);
        var value = $"\"{clause.Value}\"";

        if (_booleanFields.Contains(clause.Field.ToLower()))
        {
            return (clause.Value.ToString()?.ToLower() == "true") ? $"{field} is true" : $"{field} is false";
        }

        return clause.Operator switch
        {
            "contains" => $"{field} contains {value}",
            "=" => $"{field} is exactly {value}",
            ">" => $"{field} is greater than {value}",
            "<" => $"{field} is less than {value}",
            ">=" => $"{field} is {value} or more",
            "<=" => $"{field} is {value} or less",
            "-" => $"{field} is between {value} and \"{clause.UpperValue}\"",
            "^" => $"{field} starts with {value}",
            "$" => $"{field} ends with {value}",
            "~" => $"{field} sounds like {value}",
            _ => $"{field} {clause.Operator} {value}"
        };
    }
}