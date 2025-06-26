// --- File: Dimmer/DimmerSearch/SemanticModel.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dimmer.DimmerSearch;

#region --- Enums and Root Query ---
public enum TextOperator { Contains, StartsWith, EndsWith, Equals, Or, Levenshtein }
public enum NumericOperator { Equals, GreaterThan, LessThan, Between }
public enum SortDirection { Ascending, Descending }
public enum LimiterType { First, Last, Random }

public class SemanticQuery
{
    public List<QueryClause> Clauses { get; } = new();
    public List<SortClause> SortDirectives { get; } = new();
    public LimiterClause? LimiterDirective { get; set; }
    public List<string> GeneralAndTerms { get; } = new();
    public List<string> GeneralOrTerms { get; } = new();
    public string Humanize()
    {
        var parts = new List<string>();
        if (Clauses.Any())
            parts.AddRange(Clauses.Select(c => c.Humanize()));
        if (GeneralOrTerms.Any())
            parts.Add($"text contains any of: '{string.Join("', '", GeneralOrTerms)}'");
        if (GeneralAndTerms.Any())
            parts.Add($"text contains all of: '{string.Join("', '", GeneralAndTerms)}'");
        if (!parts.Any() && !SortDirectives.Any() && LimiterDirective == null)
            return "Searching for everything...";

        var sb = new StringBuilder("Find songs where " + string.Join(" AND ", parts));
        if (SortDirectives.Any())
        { /* ... */ } // Humanize logic for sort/limit
        return sb.ToString();
    }
}
#endregion

#region --- Clauses ---
public abstract class QueryClause
{
    public string FieldName { get; set; }
    public string Keyword { get; set; }
    public string RawValue { get; set; }
    public bool IsNegated { get; set; }
    public bool IsInclusion { get; set; }
    public abstract Func<SongModelView, bool> AsPredicate();
    public abstract string Humanize();
}

public class TextCondition { public string Value { get; set; } public TextOperator Operator { get; set; } = TextOperator.Contains; }

public class TextClause : QueryClause
{
    public List<TextCondition> Conditions { get; set; } = new();
    public override Func<SongModelView, bool> AsPredicate()
    {
        Func<SongModelView, bool> positivePredicate = song => Conditions.Any(cond => {
            var songValue = SemanticQueryHelpers.GetStringProp(song, FieldName);
            return cond.Operator switch
            {
                TextOperator.StartsWith => songValue.StartsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.EndsWith => songValue.EndsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.Equals => songValue.Equals(cond.Value, StringComparison.OrdinalIgnoreCase),
                _ => songValue.Contains(cond.Value, StringComparison.OrdinalIgnoreCase)
            };
        });
        return IsNegated ? (s => !positivePredicate(s)) : positivePredicate;
    }
    public override string Humanize() { /* ... */ return $"{FieldName} checks..."; }
}

public class NumericClause : QueryClause
{
    public double? Value { get; set; }
    public double? UpperValue { get; set; }
    public NumericOperator Operator { get; set; }
    public override Func<SongModelView, bool> AsPredicate()
    {
        if (!Value.HasValue)
            return s => true;
        Func<SongModelView, bool> positivePredicate = Operator switch
        {
            NumericOperator.Between => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) >= Value && SemanticQueryHelpers.GetNumericProp(song, FieldName) <= UpperValue,
            NumericOperator.GreaterThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) > Value,
            NumericOperator.LessThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) < Value,
            _ => song => Math.Abs(SemanticQueryHelpers.GetNumericProp(song, FieldName) - Value.Value) < 0.001
        };
        return IsNegated ? (s => !positivePredicate(s)) : positivePredicate;
    }
    public override string Humanize() { /* ... */ return $"{FieldName} checks..."; }
}

public class FlagClause : QueryClause
{
    public bool TargetValue { get; set; }
    public override Func<SongModelView, bool> AsPredicate()
    {
        Func<SongModelView, bool> positivePredicate = song => SemanticQueryHelpers.GetBoolProp(song, FieldName) == TargetValue;
        return IsNegated ? (s => !positivePredicate(s)) : positivePredicate;
    }
    public override string Humanize() => $"{FieldName} is {(IsNegated ? !TargetValue : TargetValue)}";
}
#endregion

#region --- Sorters and Limiters ---
public class SortClause { public string FieldName { get; set; } public SortDirection Direction { get; set; } }
public class LimiterClause { public LimiterType Type { get; set; } public int Count { get; set; } }
#endregion
