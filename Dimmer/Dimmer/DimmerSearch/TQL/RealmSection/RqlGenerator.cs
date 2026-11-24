using System.Globalization;

namespace Dimmer.DimmerSearch.TQL.RealmSection;

public static class RqlGenerator
{
    private const string MatchAllPredicate = "TRUEPREDICATE";

    public static string Generate(IQueryNode node) => node switch
    {
        LogicalNode n => HandleLogical(n),
        NotNode n => $"NOT ({Generate(n.NodeToNegate)})",
        ClauseNode n => GenerateClause(n),
        InNode n => GenerateInClause(n),
        FuzzyDateNode n => GenerateFuzzyDateClause(n),
        _ => MatchAllPredicate
    };

    private static string GenerateInClause(InNode node)
    {
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef) || node.Values.Count == 0)
            return MatchAllPredicate;

        string clause;
        if (fieldDef.Type == FieldType.Text)
        {
            // Realm RQL's IN operator is case-sensitive; expand to case-insensitive ORs for text
            var comparisons = node.Values.Select(v => $"{fieldDef.PropertyName} ==[c] {FormatValue(v, FieldType.Text)}");
            clause = $"({string.Join(" OR ", comparisons)})";
        }
        else
        {
            var formattedValues = node.Values.Select(v => FormatValue(v, fieldDef.Type));
            clause = $"{fieldDef.PropertyName} IN {{{string.Join(", ", formattedValues)}}}";
        }

        return node.IsNegated ? $"NOT ({clause})" : clause;
    }

    private static string HandleLogical(LogicalNode n)
    {
        string left = Generate(n.Left);
        string right = Generate(n.Right);

        if (n.Operator == LogicalOperator.And)
        {
            if (left == MatchAllPredicate) return right;
            if (right == MatchAllPredicate) return left;
            return $"({left} AND {right})";
        }

        if (n.Operator == LogicalOperator.Or)
        {
            if (left == MatchAllPredicate || right == MatchAllPredicate) return MatchAllPredicate;
            return $"({left} OR {right})";
        }

        return $"({left} AND {right})";
    }

    private static string GenerateClause(ClauseNode node)
    {
        if (node.Operator == "matchall")
            return MatchAllPredicate;

        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
            fieldDef = FieldRegistry.FieldsByAlias["any"];

        string clause = BuildClause(fieldDef, node.Operator, node.Value, node.UpperValue);
        return node.IsNegated ? $"NOT ({clause})" : clause;
    }

    private static string BuildClause(FieldDefinition fieldDef, string op, object value, object? upperValue)
    {
        return fieldDef.Type switch
        {
            FieldType.Date => BuildDateClause(fieldDef, op, value.ToString()),

            FieldType.Boolean => $"{fieldDef.PropertyName} == {FormatValue(value, FieldType.Boolean)}",

            FieldType.Duration or FieldType.Numeric => op switch
            {
                ">" => $"{fieldDef.PropertyName} > {FormatValue(value, fieldDef.Type)}",
                "<" => $"{fieldDef.PropertyName} < {FormatValue(value, fieldDef.Type)}",
                ">=" => $"{fieldDef.PropertyName} >= {FormatValue(value, fieldDef.Type)}",
                "<=" => $"{fieldDef.PropertyName} <= {FormatValue(value, fieldDef.Type)}",
                "-" => $"({fieldDef.PropertyName} >= {FormatValue(value, fieldDef.Type)} AND {fieldDef.PropertyName} <= {FormatValue(upperValue, fieldDef.Type)})",
                _ => $"{fieldDef.PropertyName} == {FormatValue(value, fieldDef.Type)}",
            },

            FieldType.Text => op switch
            {
                _ when value?.ToString()?.ToLowerInvariant() is "empty" or "none" or "null"
                    => $"({fieldDef.PropertyName} == null OR {fieldDef.PropertyName} == '')",

                "=" => $"{fieldDef.PropertyName} ==[c] {FormatValue(value)}",
                "^" => $"{fieldDef.PropertyName} BEGINSWITH[c] {FormatValue(value)}",
                "$" => $"{fieldDef.PropertyName} ENDSWITH[c] {FormatValue(value)}",
                "~" => $"{fieldDef.PropertyName} LIKE[c] {FormatValue($"*{value}*")}",
                _ => $"{fieldDef.PropertyName} CONTAINS[c] {FormatValue(value)}",
            },

            _ => $"{fieldDef.PropertyName} == {FormatValue(value)}"
        };
    }

    private static string GenerateFuzzyDateClause(FuzzyDateNode node)
    {
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.DateField, out var fieldDef))
            return MatchAllPredicate;

        var now = DateTimeOffset.UtcNow;
        switch (node.Type)
        {
            case FuzzyDateNode.Qualifier.Never:
                return $"{fieldDef.PropertyName} == null";

            case FuzzyDateNode.Qualifier.Ago when node.OlderThan.HasValue:
                var boundaryDate = now.Subtract(node.OlderThan.Value);
                string rqlOperator = node.Operator switch
                {
                    ">" => "<",
                    "<" => ">",
                    ">=" => "<=",
                    "<=" => ">=",
                    _ => ">"
                };
                return $"{fieldDef.PropertyName} {rqlOperator} {FormatValue(boundaryDate, FieldType.Date)}";

            case FuzzyDateNode.Qualifier.Between when node.OlderThan.HasValue && node.NewerThan.HasValue:
                var olderBoundary = now.Subtract(node.OlderThan.Value);
                var newerBoundary = now.Subtract(node.NewerThan.Value);
                return $"({fieldDef.PropertyName} >= {FormatValue(olderBoundary, FieldType.Date)} AND {fieldDef.PropertyName} <= {FormatValue(newerBoundary, FieldType.Date)})";

            default:
                return MatchAllPredicate;
        }
    }

    public static string FormatValue(object? value, FieldType type = FieldType.Text)
    {
        if (value is null)
            return "null";

        return type switch
        {
            FieldType.Text => $"'{value.ToString()?.Replace("\\", "\\\\").Replace("'", "\\'")}'",
            FieldType.Numeric => double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var num)
                ? num.ToString(CultureInfo.InvariantCulture)
                : "0",
            FieldType.Duration => TqlUtilities.ParseDuration(value.ToString()).ToString(CultureInfo.InvariantCulture),
            FieldType.Boolean => (value.ToString()?.Trim().ToLowerInvariant() is "true" or "yes" or "1" or "oui") ? "true" : "false",
            FieldType.Date when value is DateTimeOffset dto => dto.UtcDateTime.ToString("yyyy-MM-dd@HH:mm:ss", CultureInfo.InvariantCulture),
            _ => $"'{value.ToString()?.Replace("\\", "\\\\").Replace("'", "\\'")}'"
        };
    }

    private static (DateTimeOffset start, DateTimeOffset end) ParseDateKeyword(string? text)
    {
        var now = DateTimeOffset.UtcNow.Date;
        switch (text?.Trim().ToLowerInvariant())
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
                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }
    }

    private static string BuildDateClause(FieldDefinition fieldDef, string op, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return MatchAllPredicate;

        var parsedKeywordRange = ParseDateKeyword(value);

        if (op is ">" or "<" or ">=" or "<=")
        {
            if (parsedKeywordRange.start != DateTimeOffset.MinValue)
                return $"{fieldDef.PropertyName} {op} {FormatValue(parsedKeywordRange.start, FieldType.Date)}";

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var boundaryDate))
                return $"{fieldDef.PropertyName} {op} {FormatValue(boundaryDate, FieldType.Date)}";

            return "FALSEPREDICATE";
        }

        var start = parsedKeywordRange.start;
        var end = parsedKeywordRange.end;

        if (start == DateTimeOffset.MinValue && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var singleDate))
        {
            start = singleDate.Date;
            end = start.AddDays(1).AddTicks(-1);
        }

        if (start == DateTimeOffset.MinValue)
            return "FALSEPREDICATE";

        return $"({fieldDef.PropertyName} >= {FormatValue(start, FieldType.Date)} AND {fieldDef.PropertyName} <= {FormatValue(end, FieldType.Date)})";
    }
}