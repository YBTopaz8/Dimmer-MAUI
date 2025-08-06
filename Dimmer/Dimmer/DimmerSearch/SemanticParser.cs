// --- File: Dimmer/DimmerSearch/SemanticParser.cs (DEFINITIVE AND COMPLETE) ---
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
        {"disc", "DiscNumber"}, {"haslyrics", "HasLyrics"},
        {"synced", "HasSyncedLyrics"}, {"fav", "IsFavorite"},
    {"lyrics", "SyncLyrics"},
    {"slyrics", "EmbeddedSync"},
    {"lyric", "SyncLyrics"},{"note", "UserNoteText"}, // Search text in user notes
{"comment", "UserNoteText"}
};

    private static readonly HashSet<string> _numericFields = new() { "ReleaseYear", "BitRate", "DurationInSeconds", "Rating", "TrackNumber", "DiscNumber" };
    private static readonly HashSet<string> _booleanFields = new() { "HasLyrics", "HasSyncedLyrics", "IsFavorite" };
    private enum ParseState { Main, Inclusion, Exclusion }

    public SemanticModel Parse(string searchText)
    {
        var query = new SemanticModel();
        var tokenQueue = new Queue<string>(QueryTokenizer.Tokenize(searchText));
        string lastFieldName = "Title";


        var currentState = ParseState.Main;


        while (tokenQueue.Count > 0)
        {
            string token = tokenQueue.Dequeue();
            string lowerToken = token.ToLower();
            string? nextToken = tokenQueue.TryPeek(out var next) ? next : null;

            // --- Handle Top-Level Directives ---
            if (lowerToken == "include" || lowerToken == "add")
            {
                currentState = ParseState.Inclusion;
                continue; // Move to the next token, which will be part of the INCLUDE block
            }
            if (lowerToken == "exclude" || lowerToken == "remove")
            {
                currentState = ParseState.Exclusion;
                continue; // Move to the next token, which will be part of the EXCLUDE block
            }

            if (lowerToken == "random" || lowerToken == "shuffle")
            {
                // If the NEXT token is a number (e.g., "random 10"), it's a limiter.
                if (nextToken != null && int.TryParse(nextToken, out int rand))
                {
                    query.LimiterDirective = new LimiterClause { Type = LimiterType.Random, Count = rand };
                    tokenQueue.Dequeue(); // Consume the number
                }
                else // Otherwise, it's a sort command (e.g., "artist random")
                {
                    query.SortDirectives.Add(new SortClause { FieldName = lastFieldName, Direction = SortDirection.Random });
                }
                continue; // Move to the next token
            }

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
                // NEW: Note the simplified call here. No more boolean parameter.
                var clause = CreateClauseFromToken(fieldName, parts[0], parts[1]);
                if (clause != null)
                {
                    // NEW: Add the created clause to the CORRECT list based on our current state.
                    switch (currentState)
                    {
                        case ParseState.Main:
                            query.MainClauses.Add(clause);
                            break;
                        case ParseState.Inclusion:
                            query.InclusionClauses.Add(clause);
                            break;
                        case ParseState.Exclusion:
                            query.ExclusionClauses.Add(clause);
                            break;
                    }
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

    private static QueryClause? CreateClauseFromToken(string fieldName, string keyword, string value)
    {
        string originalRawValue = value;
        bool isNegated = value.StartsWith('!');
        if (isNegated)
        { value = value[1..]; }

        var baseProps = new Action<QueryClause>(c =>
        {
            c.FieldName = fieldName;
            c.Keyword = keyword;
            c.RawValue = originalRawValue;
            c.IsNegated = isNegated;
            // REMOVED: c.IsInclusion = isInclusion;
        });
        if (_numericFields.Contains(fieldName))
        {
            var clause = new NumericClause();

            // Handle operators like > < >= <=
            if (value.StartsWith(">="))
            { clause.Operator = NumericOperator.GreaterThanOrEqual; value = value[2..]; }
            else if (value.StartsWith("<="))
            { clause.Operator = NumericOperator.LessThanOrEqual; value = value[2..]; }
            else if (value.StartsWith('>'))
            { clause.Operator = NumericOperator.GreaterThan; value = value[1..]; }
            else if (value.StartsWith('<'))
            { clause.Operator = NumericOperator.LessThan; value = value[1..]; }
            else if (value.StartsWith('='))
            { clause.Operator = NumericOperator.Equals; value = value[1..]; }
            else
            { clause.Operator = NumericOperator.Equals; }

            // *** START OF THE FIX FOR 'len' ***
            if (fieldName == "DurationInSeconds")
            {
                // Check for a range first (we'll use this in a later feature)
                var rangeParts = value.Split('-', 2);
                if (rangeParts.Length == 2 && TryParseDuration(rangeParts[0], out double low) && TryParseDuration(rangeParts[1], out double high))
                {
                    clause.Operator = NumericOperator.Between;
                    clause.Value = low;
                    clause.UpperValue = high;
                }
                else if (TryParseDuration(value, out double duration))
                {
                    clause.Value = duration;
                }
            }
            else // For all other numeric fields (year, bpm, etc.)
            {
                // Regular numeric parsing
                var rangeParts = value.Split('-', 2);
                if (rangeParts.Length == 2 && double.TryParse(rangeParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double low) && double.TryParse(rangeParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double high))
                {
                    clause.Operator = NumericOperator.Between;
                    clause.Value = low;
                    clause.UpperValue = high;
                }
                else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                {
                    clause.Value = val;
                }
            }
            // *** END OF THE FIX ***

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

            // MODIFICATION: Handle `note:!""`
            if (value == "\"\"" && isNegated)
            {
                // If the query is `field:!""`, create a single `IsNotEmpty` condition.
                clause.Conditions.Add(new TextCondition { Operator = TextOperator.IsNotEmpty, Value = "" });
                isNegated = false; // The negation is now part of the operator, so reset it.
            }
            else
            {
                AddTextConditionsToClause(clause, value);
            }

            baseProps(clause);
            clause.IsNegated = isNegated; // Apply the final negation status
            return clause;
        }
        return null; // If no valid clause was created  
    }

    // --- NEW, ROBUST HELPER FOR ALL TEXT PARSING ---
    private static void AddTextConditionsToClause(TextClause clause, string value)
    {
        bool isQuotedPhrase = value.StartsWith("\"") && value.EndsWith("\"");

        // If it's a complex OR group AND not a quoted phrase
        if (value.Contains('|') && !isQuotedPhrase)
        {
            var orParts = value.Split('|');
            foreach (var part in orParts)
            {
                clause.Conditions.Add(CreateSingleTextCondition(part));
            }
        }
        else // It's a single value (could be quoted or a single word)
        {
            clause.Conditions.Add(CreateSingleTextCondition(value));
        }
    }

    // This helper creates one condition from one piece of text.
    private static TextCondition CreateSingleTextCondition(string value)
    {
        var condition = new TextCondition(); // Default operator is Contains
        string textValue = value.Trim();
        if (textValue == "\"\"")
        {
            condition.Operator = TextOperator.IsEmpty;
            condition.Value = string.Empty;
            return condition;
        }
        // Handle quoted phrases first to preserve their content
        if (textValue.StartsWith("\"") && textValue.EndsWith("\""))
        {
            textValue = textValue.Trim('"');
            // For a quoted phrase, the operator is always 'Contains'.
            // If the user wants an exact match, they can use the '=' operator.
            condition.Operator = TextOperator.Contains;
        }
        else // Handle operators for unquoted text
        {
            char firstChar = textValue.FirstOrDefault();
            if (firstChar == '^')
            { condition.Operator = TextOperator.StartsWith; textValue = textValue[1..]; }
            else if (firstChar == '$')
            { condition.Operator = TextOperator.EndsWith; textValue = textValue[1..]; }
            else if (firstChar == '~')
            { condition.Operator = TextOperator.Levenshtein; textValue = textValue[1..]; }
            else if (firstChar == '=')
            { condition.Operator = TextOperator.Equals; textValue = textValue[1..]; }
        }

        // Handle the "is empty" search case for both quoted and unquoted
        if (string.IsNullOrEmpty(textValue))
        {
            condition.Operator = TextOperator.Equals;
            condition.Value = string.Empty;
        }
        else
        {
            condition.Value = textValue;
        }

        return condition;
    }
    private static bool TryParseDuration(string text, out double totalSeconds)
    {
        totalSeconds = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Split(':');
        double multiplier = 1;
        bool success = true;
        Array.Reverse(parts);
        foreach (var part in parts)
        {
            if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                totalSeconds += value * multiplier;
                multiplier *= 60;
            }
            else
            {
                success = false;
                break;
            }
        }
        return success;
    }


}
