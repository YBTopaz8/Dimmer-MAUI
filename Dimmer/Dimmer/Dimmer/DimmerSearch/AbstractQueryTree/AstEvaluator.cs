using Dimmer.DimmerSearch.AbstractQueryTree.NL;


namespace Dimmer.DimmerSearch.AbstractQueryTree;


public class AstEvaluator
{

    public Func<SongModelView, bool> CreatePredicate(IQueryNode rootNode)
    {
        return song => Evaluate(rootNode, song);
    }

    private bool Evaluate(IQueryNode node, SongModelView song) => node switch
    {
        LogicalNode n => EvaluateLogical(n, song),
        NotNode n => !Evaluate(n.NodeToNegate, song),
        ClauseNode n => EvaluateClause(n, song),
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
        var accessor = fieldDef.PropertyExpression.Compile();
        object? songValueObject = accessor(song);
        bool result = false;
        switch (fieldDef.Type)
        {
            case FieldType.Text:
                string songValue = songValueObject?.ToString() ?? "";
                string queryValue = node.Value.ToString() ?? "";
                result = node.Operator switch
                {
                    "=" => songValue.Equals(queryValue, StringComparison.OrdinalIgnoreCase),
                    "^" => songValue.StartsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                    "$" => songValue.EndsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                    "~" => SemanticQueryHelpers.LevenshteinDistance(songValue.ToLowerInvariant(), queryValue.ToLowerInvariant()) <= 2,
                    _ => songValue.Contains(queryValue, StringComparison.OrdinalIgnoreCase)
                };
                break;

            case FieldType.Numeric:
            case FieldType.Duration:
                if (songValueObject is not IConvertible)
                    return false;
                double songNumericValue = Convert.ToDouble(songValueObject);
                double queryNumericValue = (fieldDef.Type == FieldType.Duration)
                    ? ParseDuration(node.Value.ToString())
                    : Convert.ToDouble(node.Value, CultureInfo.InvariantCulture);

                result = node.Operator switch
                {
                    ">" => songNumericValue > queryNumericValue,
                    "<" => songNumericValue < queryNumericValue,
                    ">=" => songNumericValue >= queryNumericValue,
                    "<=" => songNumericValue <= queryNumericValue,
                    "-" => songNumericValue >= queryNumericValue && songNumericValue <= ParseDuration(node.UpperValue?.ToString() ?? "0"),
                    _ => Math.Abs(songNumericValue - queryNumericValue) < 0.001
                };
                break;

            case FieldType.Boolean:
                if (songValueObject is not bool songBoolValue)
                    return false;
                bool queryBoolValue = "true|yes|1".Contains(node.Value.ToString()?.ToLower() ?? "false");
                result = songBoolValue == queryBoolValue;
                break;

            case FieldType.Date: // This is where our new date logic goes!
                if (songValueObject is not DateTimeOffset songDateValue)
                    return false;
                var queryDateRange = ParseDateValue(node.Value.ToString()); // The helper we wrote before
                result = node.Operator switch
                {
                    ">" => songDateValue.Date > queryDateRange.start.Date,
                    "<" => songDateValue.Date < queryDateRange.start.Date,
                    ">=" => songDateValue.Date >= queryDateRange.start.Date,
                    "<=" => songDateValue.Date <= queryDateRange.end.Date,
                    _ => songDateValue.Date >= queryDateRange.start.Date && songDateValue.Date <= queryDateRange.end.Date
                };
                break;
        }

        // Finally, apply negation if it exists.
        return node.IsNegated ? !result : result;
    }
    private static double ParseDuration(string text)
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
}