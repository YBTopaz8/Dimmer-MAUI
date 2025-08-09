using Fastenshtein;


namespace Dimmer.DimmerSearch.TQL;


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

            case FieldType.Date: // This is where our new date logic goes!
                var songDateValue = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);
                var (start, end)= ParseDateValue(node.Value.ToString()); 
                result = node.Operator switch
                {
                    ">" => songDateValue.Value.Date > end.Date,
                    "<" => songDateValue.Value.Date< start.Date,
                    ">=" => songDateValue.Value.Date>= start.Date,
                    "<=" => songDateValue.Value.Date<= end.Date,
                    _ => songDateValue.Value.Date>= start.Date && songDateValue.Value.Date <= end.Date
                };
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