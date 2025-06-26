using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;

public class SemanticParser
{
    private static readonly Dictionary<string, string> _fieldMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        {"title", "Title"}, {"t", "Title"},
        {"artist", "OtherArtistsName"}, {"ar", "OtherArtistsName"},
        {"album", "AlbumName"}, {"al", "AlbumName"},
        {"genre", "Genre.Name"}, {"composer", "Composer"},
        {"lang", "Language"}, {"year", "ReleaseYear"},
        {"bpm", "BitRate"}, {"len", "DurationInSeconds"},
        {"rating", "Rating"}, {"track", "TrackNumber"},
        {"disc", "DiscNumber"}, {"lyrics", "HasLyrics"},
        {"synced", "HasSyncedLyrics"}, {"fav", "IsFavorite"}
    };

    // Add known numeric and boolean fields for smart clause creation
    private static readonly HashSet<string> _numericFields = new() { "ReleaseYear", "BitRate", "DurationInSeconds", "Rating", "TrackNumber", "DiscNumber" };
    private static readonly HashSet<string> _booleanFields = new() { "HasLyrics", "HasSyncedLyrics", "IsFavorite" };
    public SemanticQuery Parse(string searchText)
    {
        var query = new SemanticQuery();

        // 1. Tokenize the entire string once.
        var tokenQueue = new Queue<string>(QueryTokenizer.Tokenize(searchText));

        bool currentInclusionState = true;
        string lastFieldName = "Title"; // Default sort field

        // 2. Process tokens in a robust loop until the queue is empty.
        while (tokenQueue.Count > 0)
        {
            string token = tokenQueue.Dequeue(); // Get and remove the next token
            string lowerToken = token.ToLower();

            // --- Try to process as a Top-Level Directive ---
            if (lowerToken == "exclude")
            { currentInclusionState = false; continue; }
            if (lowerToken == "include")
            { currentInclusionState = true; continue; }
            if (lowerToken == "asc")
            { query.SortDirectives.Add(new SortClause { FieldName = lastFieldName, Direction = SortDirection.Ascending }); continue; }
            if (lowerToken == "desc")
            { query.SortDirectives.Add(new SortClause { FieldName = lastFieldName, Direction = SortDirection.Descending }); continue; }

            // Check for directives that require a second token.
            if (tokenQueue.TryPeek(out string nextToken))
            {
                if (lowerToken == "first" && int.TryParse(nextToken, out int firstCount))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.First, Count = firstCount }; tokenQueue.Dequeue(); continue; }
                if (lowerToken == "last" && int.TryParse(nextToken, out int lastCount))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.Last, Count = lastCount }; tokenQueue.Dequeue(); continue; }
                if (lowerToken == "random" && int.TryParse(nextToken, out int randomCount))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.Random, Count = randomCount }; tokenQueue.Dequeue(); continue; }
            }

            // --- Try to process as a Prefixed Clause ---
            var parts = token.Split(new[] { ':' }, 2);
            if (parts.Length == 2 && _fieldMappings.TryGetValue(parts[0], out var fieldName))
            {
                var clause = CreateClauseFromToken(fieldName, parts[0], parts[1], currentInclusionState);
                if (clause != null)
                {
                    query.Clauses.Add(clause);
                    lastFieldName = fieldName;
                }
                continue; // Successfully processed, move to next token in queue.
            }

            // --- If we reach here, it's a General Search Term ---
            // Add it back to a list of general terms.
            query.GeneralAndTerms.Add(token);
        }

        // --- Post-processing for "Lazy OR" on general terms ---
        // If a single general term contains '|', split it into the OR list.
        var generalText = string.Join(" ", query.GeneralAndTerms);
        if (generalText.Contains('|'))
        {
            query.GeneralOrTerms.AddRange(generalText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            query.GeneralAndTerms.Clear(); // Clear the AND list as it's now an OR query.
        }

        return query;
    }
    /// <summary>
    /// The factory method that intelligently creates the correct clause type
    /// based on the field and parses the value for operators.
    /// </summary>
    private QueryClause CreateClauseFromToken(string fieldName, string keyword, string value, bool isInclusion)
    {
        // Helper action to set the common properties on any created clause.
        var baseProps = new Action<QueryClause>(c => {
            c.Keyword = keyword;
            c.RawValue = value;
            c.IsInclusion = isInclusion;
        });

        if (_numericFields.Contains(fieldName))
        {
            var clause = new NumericClause { FieldName = fieldName, Operator = NumericOperator.Equals }; // Default

            char firstChar = value.FirstOrDefault();
            string numericValue = value;

            if (firstChar == '>')
            { clause.Operator = NumericOperator.GreaterThan; numericValue = value[1..]; }
            else if (firstChar == '<')
            { clause.Operator = NumericOperator.LessThan; numericValue = value[1..]; }

            if (numericValue.Contains('-'))
            {
                clause.Operator = NumericOperator.Between;
                var parts = numericValue.Split('-');
                if (double.TryParse(parts[0], out double start))
                    clause.Value = start;
                if (double.TryParse(parts[1], out double end))
                    clause.UpperValue = end;
            }
            else if (double.TryParse(numericValue, out double num))
            {
                clause.Value = num;
            }

            baseProps(clause);
            return clause;
        }
        else if (_booleanFields.Contains(fieldName))
        {
            var clause = new FlagClause
            {
                FieldName = fieldName,
                TargetValue = "true|yes|1".Contains(value.ToLower())
            };
            baseProps(clause);
            return clause;
        }
        else // Default to TextClause for any other field type
        {
            var clause = new TextClause { FieldName = fieldName };
            var parts = value.Split('|'); // Split by OR first

            foreach (var part in parts)
            {
                var condition = new TextCondition();
                string textValue = part.Trim();

                if (string.IsNullOrEmpty(textValue))
                    continue;

                char firstChar = textValue.FirstOrDefault();

                // Check for operators and strip them from the value
                if (firstChar == '^')
                { condition.Operator = TextOperator.StartsWith; textValue = textValue[1..]; }
                else if (firstChar == '$')
                { condition.Operator = TextOperator.EndsWith; textValue = textValue[1..]; }
                else if (firstChar == '~')
                { condition.Operator = TextOperator.Levenshtein; textValue = textValue[1..]; }
                else if (firstChar == '=')
                { condition.Operator = TextOperator.Equals; textValue = textValue[1..]; }
                // Default operator is already Contains, so no 'else' needed

                condition.Value = textValue;
                clause.Conditions.Add(condition);
            }

            baseProps(clause);
            return clause;
        }
    }
}
