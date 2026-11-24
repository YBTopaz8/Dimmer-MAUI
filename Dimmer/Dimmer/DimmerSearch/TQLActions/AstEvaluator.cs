using System.Globalization;
using Fastenshtein;

namespace Dimmer.DimmerSearch.TQLActions;

public class AstEvaluator
{
    public Func<SongModel, bool> CreatePredicate(IQueryNode rootNode)
    {
        return song => Evaluate(rootNode, song);
    }

    private bool Evaluate(IQueryNode node, SongModel song) => node switch
    {
        LogicalNode n => EvaluateLogical(n, song),
        NotNode n => !Evaluate(n.NodeToNegate, song),
        ClauseNode n => EvaluateClause(n, song),
        RandomChanceNode n => (Math.Abs(song.Id.GetHashCode()) % 100) < n.Percentage,
        FuzzyDateNode n => EvaluateFuzzyDate(n, song),
        DaypartNode n => EvaluateDaypart(n, song),
        InNode n => EvaluateInClause(n, song),
        _ => true
    };

    private bool EvaluateInClause(InNode node, SongModel song)
    {
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
            return false;

        string songValue = SemanticQueryHelpers.GetStringProp(song, fieldDef.PropertyName);
        bool result = node.Values.Any(val => songValue.Equals(val, StringComparison.OrdinalIgnoreCase));

        return node.IsNegated ? !result : result;
    }

    private bool EvaluateLogical(LogicalNode node, SongModel song)
    {
        bool leftResult = Evaluate(node.Left, song);
        if (node.Operator == LogicalOperator.And && !leftResult)
            return false;
        if (node.Operator == LogicalOperator.Or && leftResult)
            return true;
        return Evaluate(node.Right, song);
    }

    private bool EvaluateClause(ClauseNode node, SongModel song)
    {
        if (node.Operator == "matchall")
            return true;

        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
            fieldDef = FieldRegistry.FieldsByAlias["any"];

        bool result = false;
        switch (fieldDef.Type)
        {
            case FieldType.Text:
                string songValue = SemanticQueryHelpers.GetStringProp(song, fieldDef.PropertyName);
                string queryValue = node.Value.ToString() ?? "";

                if (queryValue.ToLowerInvariant() is "empty" or "none" or "null")
                {
                    result = string.IsNullOrWhiteSpace(songValue);
                    break;
                }

                result = node.Operator switch
                {
                    "=" => songValue.Equals(queryValue, StringComparison.OrdinalIgnoreCase),
                    "^" => songValue.StartsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                    "$" => songValue.EndsWith(queryValue, StringComparison.OrdinalIgnoreCase),
                    "~" => Math.Abs(songValue.Length - queryValue.Length) <= 2 &&
                           Levenshtein.Distance(songValue.ToLowerInvariant(), queryValue.ToLowerInvariant()) <= 2,
                    _ => songValue.Contains(queryValue, StringComparison.OrdinalIgnoreCase),
                };
                break;

            case FieldType.Numeric:
            case FieldType.Duration:
                double songNumericValue = SemanticQueryHelpers.GetNumericProp(song, fieldDef.PropertyName);
                if (!TryParseNumericOrDuration(fieldDef.Type, node.Value?.ToString(), out double queryNumericValue))
                {
                    result = false;
                    break;
                }

                switch (node.Operator)
                {
                    case ">": result = songNumericValue > queryNumericValue; break;
                    case "<": result = songNumericValue < queryNumericValue; break;
                    case ">=": result = songNumericValue >= queryNumericValue; break;
                    case "<=": result = songNumericValue <= queryNumericValue; break;
                    case "-":
                        result = TryParseNumericOrDuration(fieldDef.Type, node.UpperValue?.ToString(), out double upperValue) &&
                                 songNumericValue >= queryNumericValue && songNumericValue <= upperValue;
                        break;
                    default:
                        result = Math.Abs(songNumericValue - queryNumericValue) < 0.001;
                        break;
                }
                break;

            case FieldType.Boolean:
                bool songBoolValue = SemanticQueryHelpers.GetBoolProp(song, fieldDef.PropertyName);
                string boolText = node.Value.ToString()?.Trim().ToLowerInvariant() ?? "false";
                bool queryBoolValue = boolText is "true" or "yes" or "1" or "oui";
                result = songBoolValue == queryBoolValue;
                break;

            case FieldType.Date:
                var songDate = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);
                if (songDate == null)
                {
                    result = false;
                    break;
                }

                var (startRange, endRange) = ParseDateValue(node.Value.ToString());
                if (startRange == DateTimeOffset.MinValue && endRange == DateTimeOffset.MaxValue)
                {
                    result = false;
                    break;
                }

                switch (node.Operator)
                {
                    case ">": result = songDate.Value.Date > startRange.Date; break;
                    case "<": result = songDate.Value.Date < startRange.Date; break;
                    case ">=": result = songDate.Value.Date >= startRange.Date; break;
                    case "<=": result = songDate.Value.Date <= startRange.Date; break;
                    case "-":
                        var (_, upperEnd) = ParseDateValue(node.UpperValue?.ToString());
                        result = songDate.Value.Date >= startRange.Date && songDate.Value.Date <= upperEnd.Date;
                        break;
                    default:
                        result = songDate.Value >= startRange && songDate.Value <= endRange;
                        break;
                }
                break;
        }

        return node.IsNegated ? !result : result;
    }

    private static bool TryParseNumericOrDuration(FieldType type, string? text, out double value)
    {
        if (type == FieldType.Duration)
        {
            value = TqlUtilities.ParseDuration(text);
            return !string.IsNullOrWhiteSpace(text);
        }

        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static (DateTimeOffset start, DateTimeOffset end) ParseDateValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);

        var now = DateTimeOffset.UtcNow.Date;
        text = text.Trim().ToLowerInvariant();

        switch (text)
        {
            case "today":
                return (now, now.AddDays(1).AddTicks(-1));
            case "yesterday":
                var yesterday = now.AddDays(-1);
                return (yesterday, yesterday.AddDays(1).AddTicks(-1));
            case "thisweek":
            case "this week":
                var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
            case "lastweek":
            case "last week":
                var startOfLastWeek = now.AddDays(-(int)now.DayOfWeek - 7);
                return (startOfLastWeek, startOfLastWeek.AddDays(7).AddTicks(-1));
            case "last few days":
                var startOfFewDays = now.AddDays(-4);
                return (startOfFewDays, now.AddDays(1).AddTicks(-1));
            case "thismonth":
            case "this month":
                var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                return (startOfMonth, startOfMonth.AddMonths(1).AddTicks(-1));
            case "lastmonth":
            case "last month":
                var startOfLastMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-1);
                return (startOfLastMonth, startOfLastMonth.AddMonths(1).AddTicks(-1));
            case "thisyear":
            case "this year":
                var startOfYear = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
                return (startOfYear, startOfYear.AddYears(1).AddTicks(-1));
            case "lastyear":
            case "last year":
                var startOfLastYear = new DateTimeOffset(now.Year - 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
                return (startOfLastYear, startOfLastYear.AddYears(1).AddTicks(-1));
            default:
                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var absoluteDate))
                    return (absoluteDate.Date, absoluteDate.Date.AddDays(1).AddTicks(-1));
                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }
    }

    private bool EvaluateFuzzyDate(FuzzyDateNode node, SongModel song)
    {
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.DateField, out var fieldDef))
            return false;

        var songDate = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);

        if (node.Type == FuzzyDateNode.Qualifier.Never)
            return songDate == null;

        if (songDate == null)
            return false;

        var now = DateTimeOffset.UtcNow;
        switch (node.Type)
        {
            case FuzzyDateNode.Qualifier.Ago when node.OlderThan.HasValue:
                var boundaryDate = now.Subtract(node.OlderThan.Value);
                return node.Operator switch
                {
                    ">" => songDate.Value < boundaryDate,
                    "<" => songDate.Value > boundaryDate,
                    ">=" => songDate.Value <= boundaryDate,
                    "<=" => songDate.Value >= boundaryDate,
                    _ => songDate.Value > boundaryDate,
                };

            case FuzzyDateNode.Qualifier.Between when node.OlderThan.HasValue && node.NewerThan.HasValue:
                var olderBoundary = now.Subtract(node.OlderThan.Value);
                var newerBoundary = now.Subtract(node.NewerThan.Value);
                return songDate.Value >= olderBoundary && songDate.Value <= newerBoundary;

            default:
                return false;
        }
    }

    private bool EvaluateDaypart(DaypartNode node, SongModel song)
    {
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.DateField, out var fieldDef))
            return false;

        var songDate = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);
        if (songDate == null)
            return false;

        var timeOfDay = songDate.Value.TimeOfDay;
        if (node.StartTime > node.EndTime)
            return timeOfDay >= node.StartTime || timeOfDay < node.EndTime;

        return timeOfDay >= node.StartTime && timeOfDay < node.EndTime;
    }
}