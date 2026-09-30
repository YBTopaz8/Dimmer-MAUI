using System;

namespace Dimmer.DimmerSearch.TQL;

public static class DxCriteriaGenerator
{
    public static string Generate(IQueryNode node) => node switch
    {
        LogicalNode n => HandleLogical(n),
        NotNode n => $"Not ({Generate(n.NodeToNegate)})",
        ClauseNode n => GenerateClause(n),
        _ => string.Empty
    };

    private static string HandleLogical(LogicalNode n)
    {
        string left = Generate(n.Left);
        string right = Generate(n.Right);

        if (string.IsNullOrEmpty(left)) return right;
        if (string.IsNullOrEmpty(right)) return left;

        string op = n.Operator == LogicalOperator.And ? "And" : "Or";
        return $"({left} {op} {right})";
    }

    private static string GenerateClause(ClauseNode node)
    {
        if (node.Operator == "matchall") return string.Empty;

        // Map TQL alias to SongModelView property name
        if (!FieldRegistry.FieldsByAlias.TryGetValue(node.Field, out var fieldDef))
        {
            // If "any", search Title, Artist, and Album in DevExpress syntax
            string val = EscapeString(node.Value.ToString() ?? "");
            return $"(Contains([Title], '{val}') Or Contains([OtherArtistsName], '{val}') Or Contains([AlbumName], '{val}'))";
        }

        string prop = fieldDef.PropertyName;
        string value = node.Value.ToString() ?? "";

        string clause = fieldDef.Type switch
        {
            FieldType.Numeric or FieldType.Duration => node.Operator switch
            {
                ">" => $"[{prop}] > {TqlUtilities.ParseDuration(value)}",
                "<" => $"[{prop}] < {TqlUtilities.ParseDuration(value)}",
                ">=" => $"[{prop}] >= {TqlUtilities.ParseDuration(value)}",
                "<=" => $"[{prop}] <= {TqlUtilities.ParseDuration(value)}",
                _ => $"[{prop}] = {TqlUtilities.ParseDuration(value)}"
            },
            FieldType.Boolean => $"[{prop}] = {(value.ToLower().StartsWith("t") ? "True" : "False")}",
            _ => node.Operator switch
            {
                "=" => $"[{prop}] = '{EscapeString(value)}'",
                "^" => $"StartsWith([{prop}], '{EscapeString(value)}')",
                "$" => $"EndsWith([{prop}], '{EscapeString(value)}')",
                _ => $"Contains([{prop}], '{EscapeString(value)}')" // Default contains
            }
        };

        return node.IsNegated ? $"Not ({clause})" : clause;
    }

    private static string EscapeString(string val) => val.Replace("'", "''");
}