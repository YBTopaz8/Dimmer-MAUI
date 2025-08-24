using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL.RealmSection;
/// <summary>
/// A stateless utility to translate a TQL Abstract Syntax Tree (AST) into a Realm Query Language (RQL) string.
/// This class is the core of the database-first query model and replaces the AstEvaluator.
/// </summary>
public static class RqlGenerator
{
    private const string MatchAllPredicate = "TRUEPREDICATE";
    public static string Generate(IQueryNode node) => node switch
    {
        LogicalNode n => $"({Generate(n.Left)} {TranslateLogicalOperator(n.Operator)} {Generate(n.Right)})",
        NotNode n => $"NOT ({Generate(n.NodeToNegate)})",
        ClauseNode n => GenerateClause(n),
        FuzzyDateNode n => GenerateFuzzyDateClause(n),
        // DaypartNode and RandomChanceNode are not directly translatable to RQL.
        // They must be handled in-memory after the main query result is fetched.
        // We return TRUEPREDICATE so they don't block the rest of the RQL filter.
        DaypartNode => MatchAllPredicate,
        RandomChanceNode => MatchAllPredicate,
        _ => MatchAllPredicate // Default to matching everything
    };

    private static string TranslateLogicalOperator(LogicalOperator op) => op switch
    {
        LogicalOperator.And => "AND",
        LogicalOperator.Or => "OR",
        _ => "AND"
    };

    private static string GenerateClause(ClauseNode node)
    {
        if (node.Operator == "matchall")
            return MatchAllPredicate;

        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
        {
            fieldDef = FieldRegistry.FieldsByAlias["any"];
        }


        // Handle negation at the clause level by wrapping the result
        string clause = BuildClause(fieldDef, node.Operator, node.Value, node.UpperValue);
        return node.IsNegated ? $"NOT ({clause})" : clause;
    }


    private static string BuildClause(FieldDefinition fieldDef, string op, object value, object? upperValue)
    {
        // RQL operators are case-sensitive, so we use mapping. [c] denotes case-insensitivity.
        return fieldDef.Type switch
        {
                // --- ADD THIS NEW CASE BLOCK ---
                FieldType.Date => BuildDateClause(fieldDef, op, value.ToString()),

                FieldType.Boolean => $"{fieldDef.PropertyName} == {FormatValue(value, FieldType.Boolean)}",

            FieldType.Duration or FieldType.Numeric => op switch
            {
                ">" => $"{fieldDef.PropertyName} > {FormatValue(value, fieldDef.Type)}",
                "<" => $"{fieldDef.PropertyName} < {FormatValue(value, fieldDef.Type)}",
                ">=" => $"{fieldDef.PropertyName} >= {FormatValue(value, fieldDef.Type)}",
                "<=" => $"{fieldDef.PropertyName} <= {FormatValue(value, fieldDef.Type)}",
                "-" => $"{fieldDef.PropertyName} >= {FormatValue(value, fieldDef.Type)} AND {fieldDef.PropertyName} <= {FormatValue(upperValue, fieldDef.Type)}",
                _ => $"{fieldDef.PropertyName} == {FormatValue(value, fieldDef.Type)}", // Default to equality
            },

            // --- HANDLE TEXT TYPE LAST (AS IT'S THE MOST COMPLEX) ---
            FieldType.Text => op switch
            {
                "=" => $"{fieldDef.PropertyName} == {FormatValue(value)}",
                "^" => $"{fieldDef.PropertyName} BEGINSWITH[c] {FormatValue(value)}",
                "$" => $"{fieldDef.PropertyName} ENDSWITH[c] {FormatValue(value)}",
                "~" => $"{fieldDef.PropertyName} LIKE[c] '*{value}*'",
                _ => $"{fieldDef.PropertyName} CONTAINS[c] {FormatValue(value)}", // Default to contains
            },

            // Fallback for any unhandled types
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
                string agoOperator = node.Operator switch { ">" => ">", "<" => "<", _ => ">" };

                        return $"{fieldDef.PropertyName} {agoOperator} {FormatValue(boundaryDate, FieldType.Date)}";

            case FuzzyDateNode.Qualifier.Between when node.OlderThan.HasValue && node.NewerThan.HasValue:
                var olderBoundary = now.Subtract(node.OlderThan.Value);
                var newerBoundary = now.Subtract(node.NewerThan.Value);

                 return $"({fieldDef.PropertyName} >= {FormatValue(olderBoundary, FieldType.Date)} AND {fieldDef.PropertyName} <= {FormatValue(newerBoundary, FieldType.Date)})";

            default:
                return MatchAllPredicate;
        }
    }
    private static string FormatValue(object? value, FieldType type = FieldType.Text)
    {
        if (value is null)
            return "null";

        return type switch
        {
            FieldType.Text => $"'{value.ToString()?.Replace("'", "\\'")}'",
            FieldType.Numeric => Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            FieldType.Duration => ParseDuration(value.ToString()).ToString(CultureInfo.InvariantCulture),
            FieldType.Boolean => ("true|yes|1".Contains(value.ToString()?.ToLowerInvariant() ?? "")) ? "true" : "false",
            FieldType.Date when value is DateTimeOffset dto => $"TIMESTAMP({dto.ToString("o")})",
            _ => $"'{value.ToString()?.Replace("'", "\\'")}'" // Default to string
        };
    }

    private static (DateTimeOffset start, DateTimeOffset end) ParseDateKeyword(string? text)
    {
        var now = DateTimeOffset.UtcNow.Date;
        switch (text?.ToLowerInvariant())
        {
            case "today":
                return (now, now.AddDays(1).AddTicks(-1));
            case "yesterday":
                var yesterday = now.AddDays(-1);
                return (yesterday, yesterday.AddDays(1).AddTicks(-1));
                case "thisweek":
                    var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
                    return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
                case "lastweek":
                    var startOfLastWeek = now.AddDays(-(int)now.DayOfWeek - 7);
                    return (startOfLastWeek, startOfLastWeek.AddDays(7).AddTicks(-1));
                case "thismonth":
                    var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
                    return (startOfMonth, startOfMonth.AddMonths(1).AddTicks(-1));
                case "lastmonth":
                    var startOfLastMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-1);
                    return (startOfLastMonth, startOfLastMonth.AddMonths(1).AddTicks(-1));
                case "thisyear":
                    var startOfYear = new DateTimeOffset(now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    return (startOfYear, startOfYear.AddYears(1).AddTicks(-1));
                case "lastyear":
                    var startOfLastYear = new DateTimeOffset(now.Year - 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    return (startOfLastYear, startOfLastYear.AddYears(1).AddTicks(-1));
                //case "morning":
                //    return (now.AddHours(5), now.AddHours(12).AddTicks(-1));
                //case "afternoon":

            // Add other keywords like "thisweek", "lastmonth", etc.
            default:
                return (DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
        }
    }
    private static string BuildDateClause(FieldDefinition fieldDef, string op, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "TRUEPREDICATE";

        var (start, end) = ParseDateKeyword(value);

        // If the keyword is invalid, default to a non-matching query.
        if (start == DateTimeOffset.MinValue && end == DateTimeOffset.MaxValue)
        {
            return "FALSEPREDICATE";
        }

        return $"({fieldDef.PropertyName} >= {FormatValue(start, FieldType.Date)} AND {fieldDef.PropertyName} <= {FormatValue(end, FieldType.Date)})";
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
            if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                totalSeconds += val * multiplier;
                multiplier *= 60;
            }
        }
        return totalSeconds;
    }
}