using Fastenshtein;


namespace Dimmer.DimmerSearch.TQLActions;


public class AstEvaluator
{

    public Func<SongModelView, bool> CreatePredicate(IQueryNode rootNode)
    {
        return song => Evaluate(rootNode, song);
    }

    private readonly Random _random = new();
    private bool Evaluate(IQueryNode node, SongModelView song) => node switch
    {
        LogicalNode n => EvaluateLogical(n, song),
        NotNode n => !Evaluate(n.NodeToNegate, song),
        ClauseNode n => EvaluateClause(n, song),
        RandomChanceNode n => _random.Next(100) < n.Percentage,
        FuzzyDateNode n => EvaluateFuzzyDate(n, song),
        DaypartNode n => EvaluateDaypart(n, song),
        _ => true
    };

    private bool EvaluateLogical(LogicalNode node, SongModelView song)
    {
        bool leftResult = Evaluate(node.Left, song);
        if (node.Operator == LogicalOperator.And && !leftResult)
            return false;
        if (node.Operator == LogicalOperator.Or && leftResult)
            return true;
        return Evaluate(node.Right, song);
    }

    private bool EvaluateClause(ClauseNode node, SongModelView song)
    {
        if (node.Operator == "matchall")
            return true;
        // --- Step 1: Use FieldRegistry to get the field's definition ---
        // This replaces the old _fieldMappings dictionary lookup.
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
        {
            // If the field alias is invalid, we can treat it as a search on the "any" field.
            fieldDef = FieldRegistry.FieldsByAlias["any"];
        }

        bool result = false;
        switch (fieldDef.Type)
        {
            case FieldType.Text:
                string songValue = SemanticQueryHelpers.GetStringProp(song, fieldDef.PropertyName);
                string queryValue = node.Value.ToString() ?? "";

                switch (node.Operator)
                {
                    case "=":
                        result = songValue.Equals(queryValue, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "^":
                        result = songValue.StartsWith(queryValue, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "$":
                        result = songValue.EndsWith(queryValue, StringComparison.OrdinalIgnoreCase);
                        break;
                    case "~":

                        const int levenshteinThreshold = 2;
                        if (Math.Abs(songValue.Length - queryValue.Length) > levenshteinThreshold)
                        {
                            result = false;
                        }
                        else
                        {

                            result = Levenshtein.Distance(songValue.ToLowerInvariant(), queryValue.ToLowerInvariant()) <= levenshteinThreshold;
                        }
                        break;
                    default:
                        result = songValue.Contains(queryValue, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                break;

            case FieldType.Numeric:
            case FieldType.Duration:
                double songNumericValue = SemanticQueryHelpers.GetNumericProp(song, fieldDef.PropertyName);
                double queryNumericValue = fieldDef.Type == FieldType.Duration
                    ? ParseDuration(node.Value.ToString())
                    : Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);


                switch (node.Operator)
                {
                    case ">":
                        result = songNumericValue > queryNumericValue;
                        break;

                    case "<":
                        result = songNumericValue < queryNumericValue;
                        break;

                    case ">=":
                        result = songNumericValue >= queryNumericValue;
                        break;

                    case "<=":
                        result = songNumericValue <= queryNumericValue;
                        break;

                    case "-":
                        double upperValue = ParseDuration(node.UpperValue?.ToString() ?? "0");
                        result = songNumericValue >= queryNumericValue && songNumericValue <= upperValue;
                        break;

                    default: // This handles every other operator
                        result = Math.Abs(songNumericValue - queryNumericValue) < 0.001;
                        break;
                }
                break;

            case FieldType.Boolean:
                bool songBoolValue = SemanticQueryHelpers.GetBoolProp(song, fieldDef.PropertyName);
                
                bool queryBoolValue = "true|yes|1|oui".Contains(node.Value.ToString()?.ToLower() ?? "false");
                result = songBoolValue == queryBoolValue;
                break;

            case FieldType.Date: 
                var songDate = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);
                if (songDate == null)
                {
                    result = false; // Song doesn't have a date for this field, so it can't match.
                    break;
                }

                var (startRange, endRange) = ParseDateValue(node.Value.ToString());

                // Check if the query was a simple range check like ">" or "<"
                switch (node.Operator)
                {
                    case ">":
                        result = songDate.Value.Date > startRange.Date;
                        break;
                    case "<":
                        result = songDate.Value.Date < startRange.Date;
                        break;
                    case ">=":
                        result = songDate.Value.Date >= startRange.Date;
                        break;
                    case "<=":
                        result = songDate.Value.Date <= startRange.Date;
                        break;
                    case "-": // Handle explicit date ranges like added:2022-2023
                        var (upperStart, upperEnd) = ParseDateValue(node.UpperValue?.ToString());
                        result = songDate.Value.Date >= startRange.Date && songDate.Value.Date <= upperEnd.Date;
                        break;
                    default:
                        // Default behavior for date fields is "is within this date range"
                        result = songDate.Value >= startRange && songDate.Value <= endRange;
                        break;
                }
                break;

        }

        // Finally, apply negation if it exists.
        return node.IsNegated ? !result : result;
    }
    private static double ParseDuration(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
        double totalSeconds = 0;
        var parts = text.Split(':');
        double multiplier = 1;
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                totalSeconds += value * multiplier;
                multiplier *= 60;
            }
            else
            { return 0; }
        }
        return totalSeconds;
    }
    /// <summary>
    /// Parses a user's date query string into a start and end date range.
    /// Handles relative terms like "today", absolute dates like "2023-01-15",
    /// and ranges like "last month".
    /// </summary>
    private static (DateTimeOffset start, DateTimeOffset end) ParseDateValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        char firstChar = text[0];
        if (firstChar == '>' || firstChar == '<')
        {
            string datePart = text[1..];
            if (DateTimeOffset.TryParse(datePart, out var boundaryDate))
            {
                if (firstChar == '>')
                    return (boundaryDate, DateTimeOffset.MaxValue);
                if (firstChar == '<')
                    return (DateTimeOffset.MinValue, boundaryDate);
            }
        }
        var now = DateTimeOffset.UtcNow.Date; // Use start of day for consistency
        text = text.ToLowerInvariant();

        switch (text)
        {
            case "today":
                return (now, now.AddDays(1).AddTicks(-1));
            case "yesterday":
                var yesterday = now.AddDays(-1);
                return (yesterday, yesterday.AddDays(1).AddTicks(-1));
            case "this week":
                var startOfWeek = now.AddDays(-(int)now.DayOfWeek); // Assumes Sunday is start of week
                return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
            case "last week":
                var startOfLastWeek = now.AddDays(-(int)now.DayOfWeek - 7);
                return (startOfLastWeek, startOfLastWeek.AddDays(7).AddTicks(-1));
            case "last few days":
                var startOfFewDays = now.AddDays(-(int)now.DayOfWeek - 4);
                return (startOfFewDays, startOfFewDays.AddDays(4).AddTicks(-1));
            case "this month":
                var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.TimeOfDay);
                return (startOfMonth, startOfMonth.AddMonths(1).AddTicks(-1));
            case "last month":
                var startOfLastMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.TimeOfDay).AddMonths(-1);
                return (startOfLastMonth, startOfLastMonth.AddMonths(1).AddTicks(-1));
            case "this year":
                var startOfYear = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, now.TimeOfDay);
                return (startOfYear, startOfYear.AddYears(1).AddTicks(-1));
            default:
                // Try to parse as an absolute date, e.g., "2023-12-25"
                if (DateTimeOffset.TryParse(text, out var absoluteDate))
                {
                    return (absoluteDate.Date, absoluteDate.Date.AddDays(1).AddTicks(-1));
                }
                // Could add more complex parsing here like "3 days ago"
                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue); // Invalid format
        }
    }

    private bool EvaluateFuzzyDate(FuzzyDateNode node, SongModelView song)
    {
        // --- FIX: We must look up the real property name from the alias in the node.
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.DateField, out var fieldDef))
        {
            return false; // Invalid field alias used, e.g., "playedd:ago(...)"
        }
        var propertyName = fieldDef.PropertyName;
        var songDate = SemanticQueryHelpers.GetDateProp(song, propertyName);
        // --- END FIX ---

        if (node.Type == FuzzyDateNode.Qualifier.Never)
        {
            return songDate == null;
        }

        if (songDate == null)
            return false;

        var now = DateTimeOffset.UtcNow;
        switch (node.Type)
        {
            case FuzzyDateNode.Qualifier.Ago:
                if (node.OlderThan.HasValue)
                {
                    var boundaryDate = now.Subtract(node.OlderThan.Value);
                    // Now we use the operator from the node
                    return node.Operator switch
                    {
                        ">" => songDate.Value > boundaryDate,  // "more recent than 1 week ago"
                        "<" => songDate.Value < boundaryDate,  // "older than 1 week ago"
                        _ => songDate.Value > boundaryDate,  // Default 'ago' means "within this period"
                    };
                }
                break;

            case FuzzyDateNode.Qualifier.Between:
                if (node.OlderThan.HasValue && node.NewerThan.HasValue)
                {
                    var olderBoundary = now.Subtract(node.OlderThan.Value);
                    var newerBoundary = now.Subtract(node.NewerThan.Value);
                    // 'between' is a range, so it ignores other operators and implies an inclusive check.
                    return songDate.Value >= olderBoundary && songDate.Value <= newerBoundary;
                }
                break;
        }
        return false;
    }

    private bool EvaluateDaypart(DaypartNode node, SongModelView song)
    {
        // --- FIX: We must look up the real property name from the alias in the node.
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.DateField, out var fieldDef))
        {
            return false; // Invalid field alias used
        }
        var propertyName = fieldDef.PropertyName;
        var songDate = SemanticQueryHelpers.GetDateProp(song, propertyName);
        // --- END FIX ---

        if (songDate == null)
            return false;

        var timeOfDay = songDate.Value.TimeOfDay;

        if (node.StartTime > node.EndTime)
        {
            return timeOfDay >= node.StartTime || timeOfDay < node.EndTime;
        }

        return timeOfDay >= node.StartTime && timeOfDay < node.EndTime;
    }
}