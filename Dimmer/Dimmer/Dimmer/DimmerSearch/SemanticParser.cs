// --- File: Dimmer/DimmerSearch/SemanticParser.cs ---
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dimmer.DimmerSearch;

public class SemanticParser
{
    private static readonly Dictionary<string, string> _fieldMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        {"t", "Title"}, {"title", "Title"},
        {"ar", "OtherArtistsName"}, {"artist", "OtherArtistsName"},
        {"al", "AlbumName"}, {"album", "AlbumName"},
        {"genre", "Genre.Name"}, {"composer", "Composer"},
        {"lang", "Language"}, {"year", "ReleaseYear"},
        {"bpm", "BitRate"}, {"len", "DurationInSeconds"},
        {"rating", "Rating"}, {"track", "TrackNumber"},
        {"disc", "DiscNumber"}, {"lyrics", "HasLyrics"},
        {"synced", "HasSyncedLyrics"}, {"fav", "IsFavorite"}
    };

    private static readonly HashSet<string> _numericFields = new() { "ReleaseYear", "BitRate", "DurationInSeconds", "Rating", "TrackNumber", "DiscNumber" };
    private static readonly HashSet<string> _booleanFields = new() { "HasLyrics", "HasSyncedLyrics", "IsFavorite" };

    public SemanticQuery Parse(string searchText)
    {
        var query = new SemanticQuery();
        var tokenQueue = new Queue<string>(QueryTokenizer.Tokenize(searchText));
        string lastFieldName = "Title";
        bool currentIsInclusion = true;

        while (tokenQueue.Count > 0)
        {
            string token = tokenQueue.Dequeue();
            string lowerToken = token.ToLower();
            string? nextToken = tokenQueue.TryPeek(out var next) ? next : null;

            // --- Handle Top-Level Directives ---
            if (lowerToken == "include")
            { currentIsInclusion = true; continue; }
            if (lowerToken == "add")
            { currentIsInclusion = true; continue; }
            if (lowerToken == "exclude")
            { currentIsInclusion = false; continue; }
            if (lowerToken == "remove")
            { currentIsInclusion = false; continue; }
            if (lowerToken == "asc")
            { query.SortDirectives.Add(new SortClause { FieldName = lastFieldName, Direction = SortDirection.Ascending }); continue; }
            if (lowerToken == "desc")
            { query.SortDirectives.Add(new SortClause { FieldName = lastFieldName, Direction = SortDirection.Descending }); continue; }
            if (nextToken != null)
            {
                if (lowerToken == "first" && int.TryParse(nextToken, out int first))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.First, Count = first }; tokenQueue.Dequeue(); continue; }
                if (lowerToken == "last" && int.TryParse(nextToken, out int last))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.Last, Count = last }; tokenQueue.Dequeue(); continue; }
                if (lowerToken == "random" && int.TryParse(nextToken, out int rand))
                { query.LimiterDirective = new LimiterClause { Type = LimiterType.Random, Count = rand }; tokenQueue.Dequeue(); continue; }
            }

            // --- Handle Prefixed Clauses ---
            var parts = token.Split(new[] { ':' }, 2);
            if (parts.Length == 2 && _fieldMappings.TryGetValue(parts[0], out var fieldName))
            {
                var clause = CreateClauseFromToken(fieldName, parts[0], parts[1], currentIsInclusion);
                if (clause != null)
                {
                    query.Clauses.Add(clause);
                    lastFieldName = fieldName;
                }
            }
            else
            {
                query.GeneralAndTerms.Add(token);
            }
        }

        // Post-processing for lazy OR
        var generalText = string.Join(" ", query.GeneralAndTerms);
        if (generalText.Contains('|'))
        {
            query.GeneralOrTerms.AddRange(generalText.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            query.GeneralAndTerms.Clear();
        }

        return query;
    }

    private static QueryClause? CreateClauseFromToken(string fieldName, string keyword, string value, bool isInclusion)
    {
        string originalRawValue = value;

        bool isNegated = value.StartsWith("!");
        if (isNegated)
        { value = value[1..]; }

        var baseProps = new Action<QueryClause>(c => 
        { c.FieldName = fieldName; c.Keyword = keyword; c.RawValue = originalRawValue; c.IsNegated = isNegated;
            c.IsInclusion=isInclusion;
        });

        if (_numericFields.Contains(fieldName))
        {
            var clause = new NumericClause { Operator = NumericOperator.Equals };
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
                if (double.TryParse(parts[0], out double s))
                    clause.Value = s;
                if (double.TryParse(parts[1], out double e))
                    clause.UpperValue = e;
            }
            else if (double.TryParse(numericValue, out double n))
            { clause.Value = n; }

            baseProps(clause);
            return clause;
        }
        else if (_booleanFields.Contains(fieldName))
        {
            var clause = new FlagClause { TargetValue = "true|yes|1".Contains(value.ToLower()) };
            baseProps(clause);
            return clause;
        }
        else // Default to TextClause
        {
            var clause = new TextClause();
            var orParts = value.Split('|');
            foreach (var part in orParts)
            {
                var condition = new TextCondition();
                string textValue = part.Trim();
                if (string.IsNullOrEmpty(textValue))
                    continue;
                char firstChar = textValue.FirstOrDefault();
                if (firstChar == '^')
                { condition.Operator = TextOperator.StartsWith; textValue = textValue[1..]; }
                else if (firstChar == '$')
                { condition.Operator = TextOperator.EndsWith; textValue = textValue[1..]; }
                else if (firstChar == '~')
                { condition.Operator = TextOperator.Levenshtein; textValue = textValue[1..]; }
                else if (firstChar == '=')
                { condition.Operator = TextOperator.Equals; textValue = textValue[1..]; }
                condition.Value = textValue;
                clause.Conditions.Add(condition);
            }
            baseProps(clause);
            return clause;
        }
    }
}