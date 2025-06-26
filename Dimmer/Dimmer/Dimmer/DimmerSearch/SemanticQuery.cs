// --- File: Dimmer/DimmerSearch/SemanticModel.cs ---
using System.Text;

using DynamicData.Binding;

namespace Dimmer.DimmerSearch;

#region --- Enums ---
public enum TextOperator { Contains, StartsWith, EndsWith, Equals, Or, Levenshtein }
public enum NumericOperator { Equals, GreaterThan, LessThan, Between }
public enum SortDirection { Ascending, Descending }
public enum LimiterType { First, Last, Random }
#endregion

#region --- Root Query Object ---
public class SemanticQuery
{
    public List<QueryClause> Clauses { get; } = new();
    public List<SortClause> SortDirectives { get; } = new();
    public LimiterClause? LimiterDirective { get; set; }

    public List<string> GeneralAndTerms { get; } = new();
    public List<string> GeneralOrTerms { get; } = new();
    public string Humanize()
    {
        if (Clauses.Count==0 && SortDirectives.Count==0 && LimiterDirective == null)
            return "Searching for everything...";

        var sb = new StringBuilder("Find songs ");
        var parts = Clauses.Select(c => c.Humanize()).ToList();

        // Add human-readable parts for the general terms
        if (GeneralOrTerms.Count!=0)
            parts.Add($"containing any of the words: '{string.Join("', '", GeneralOrTerms)}'");
        if (GeneralAndTerms.Count!=0)
            parts.Add($"containing all of the words: '{string.Join("', '", GeneralAndTerms)}'");

        if (parts.Count!=0)
            sb.Append(" " + string.Join(", and ", parts));
        if (SortDirectives.Count!=0)
        {
            var sortStrings = SortDirectives.Select(s => $"{s.FieldName} ({s.Direction.ToString().ToLower()})");
            sb.Append($", sorted by {string.Join(", then ", sortStrings)}");
        }

        if (LimiterDirective != null)
            sb.Append($", taking the {LimiterDirective.Type.ToString().ToLower()} {LimiterDirective.Count}");

        return sb.ToString();
    }
}
#endregion

#region --- Clauses (Filters, Sorters, Limiters) ---
public abstract class QueryClause
{
    public string FieldName { get; set; }
    public string Keyword { get; set; }
    public string RawValue { get; set; }
    public bool IsInclusion { get; set; } // include= vs exclude=
    public abstract Func<SongModelView, bool> AsPredicate();
    public abstract string Humanize();
}
public class TextCondition
{
    public string Value { get; set; }
    public TextOperator Operator { get; set; } = TextOperator.Contains; // Default
}
public class TextClause : QueryClause
{
    public new  string FieldName { get; set; }
    public List<TextCondition> Conditions { get; set; } = new();

    public override Func<SongModelView, bool> AsPredicate()
    {
        // Build a predicate for each individual condition
        var predicates = Conditions.Select(cond => {
            Func<SongModelView, bool> p = cond.Operator switch
            {
                TextOperator.StartsWith => s => SemanticQueryHelpers.GetStringProp(s, FieldName).StartsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.EndsWith => s => SemanticQueryHelpers.GetStringProp(s, FieldName).EndsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.Equals => s => SemanticQueryHelpers.GetStringProp(s, FieldName).Equals(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.Levenshtein => s => SemanticQueryHelpers.LevenshteinDistance(SemanticQueryHelpers.GetStringProp(s, FieldName), cond.Value) <= 2,
                _ => s => SemanticQueryHelpers.GetStringProp(s, FieldName).Contains(cond.Value, StringComparison.OrdinalIgnoreCase)
            };
            return p;
        }).ToList();

        // The final predicate checks if the song passes ANY of the generated predicates
        Func<SongModelView, bool> finalPredicate = song => predicates.Any(p => p(song));

        return IsInclusion ? finalPredicate : (s => !finalPredicate(s));
    }

    public override string Humanize()
    {
        var descriptions = Conditions.Select(cond => {
            string verb = cond.Operator switch
            {
                TextOperator.StartsWith => "starts with",
                TextOperator.EndsWith => "ends with",
                TextOperator.Equals => "is exactly",
                TextOperator.Levenshtein => "sounds like",
                _ => "contains"
            };
            return $"{verb} '{cond.Value}'";
        });

        return $"where the {FieldName} {(IsInclusion ? " " : "does not match any of: ")}{string.Join(" OR ", descriptions)}";
    }
}

public class NumericClause : QueryClause
{
    public new string FieldName { get; set; }
    public double? Value { get; set; }
    public double? UpperValue { get; set; }
    public NumericOperator Operator { get; set; }
    public override Func<SongModelView, bool> AsPredicate()
    {
        Func<SongModelView, bool> predicate = s => false;
        if (Value.HasValue)
        {
            predicate = Operator switch
            {
                NumericOperator.Between => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) >= Value && SemanticQueryHelpers.GetNumericProp(song, FieldName) <= UpperValue,
                NumericOperator.GreaterThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) > Value,
                NumericOperator.LessThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) < Value,
                _ => song => Math.Abs(SemanticQueryHelpers.GetNumericProp(song, FieldName) - Value.Value) < 0.001
            };
        }
        return IsInclusion ? predicate : (s => !predicate(s));
    }

    public override string Humanize()
    {
        string opStr = Operator switch
        {
            NumericOperator.Between => $"is between {Value} and {UpperValue}",
            NumericOperator.GreaterThan => $"is greater than {Value}",
            NumericOperator.LessThan => $"is less than {Value}",
            _ => $"is {Value}"
        };
        return $"where the {FieldName} {(IsInclusion ? "is" : "is not")} {opStr}";
    }
}

public class FlagClause : QueryClause
{
    public new string FieldName { get; set; }
    public bool TargetValue { get; set; }
    public override Func<SongModelView, bool> AsPredicate()
    {
        Func<SongModelView, bool> predicate = song => SemanticQueryHelpers.GetBoolProp(song, FieldName) == TargetValue;
        return IsInclusion ? predicate : (s => !predicate(s));
    }
    public override string Humanize() => $"where {FieldName} is {(IsInclusion ? TargetValue : !TargetValue)}";
}

public class SortClause
{
    public string FieldName { get; set; }
    public SortDirection Direction { get; set; }
}

public class LimiterClause
{
    public LimiterType Type { get; set; }
    public int Count { get; set; }
}
#endregion