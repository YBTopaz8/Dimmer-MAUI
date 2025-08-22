// --- File: Dimmer/DimmerSearch/SemanticModel.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dimmer.DimmerSearch;

#region --- Enums and Root Query ---
public enum TextOperator { Contains, StartsWith, EndsWith, Equals, Or, Levenshtein, IsEmpty, IsNotEmpty }
public enum NumericOperator { Equals, GreaterThan, LessThan, Between, GreaterThanOrEqual, LessThanOrEqual }
//public enum SortDirection { Asc, Desc, Random }
//public enum LimiterType { First, Last, Random }

public class SemanticModel
{
    public List<QueryClause> MainClauses { get; } = new();
    public List<QueryClause> InclusionClauses { get; } = new();
    public List<QueryClause> ExclusionClauses { get; } = new();

    public List<QueryClause> Clauses { get; } = new();
    public List<SortClause> SortDirectives { get; } = new();
    public LimiterClause? LimiterDirective { get; set; }
    public List<string> GeneralAndTerms { get; } = new();
    public List<string> GeneralOrTerms { get; } = new();
    public string Humanize()
    {
        var parts = new List<string>();
        if (Clauses.Count!=0)
            parts.AddRange(Clauses.Select(c => c.Humanize()));
        if (GeneralOrTerms.Count!=0)
            parts.Add($"text contains any of: '{string.Join("', '", GeneralOrTerms)}'");
        if (GeneralAndTerms.Count!=0)
            parts.Add($"text contains all of: '{string.Join("', '", GeneralAndTerms)}'");
        if (parts.Count==0 && SortDirectives.Count==0 && LimiterDirective == null)
            return "Searching for everything...";

        var sb = new StringBuilder("Find songs where " + string.Join(" AND ", parts));
        if (SortDirectives.Count!=0)
        { /* ... */ } // Humanize logic for sort/limit
        return sb.ToString();
    }
}
#endregion

#region --- Clauses ---
public enum LogicalOperator { And, Or }

// NEW CLASS: Represents a logical group of other clauses, like (a AND b)
public class GroupClause : QueryClause
{
    public List<QueryClause> Clauses { get; } = new();
    public LogicalOperator Operator { get; set; } = LogicalOperator.And;

    public override Func<SongModelView, bool> AsPredicate()
    {
        var predicates = Clauses.Select(c => c.AsPredicate()).ToList();
        if (predicates.Count==0)
            return s => true;

        Func<SongModelView, bool> positivePredicate = Operator switch
        {
            LogicalOperator.Or => song => predicates.Any(p => p(song)),
            _ => song => predicates.All(p => p(song)) // Default to AND
        };

        // Note: IsNegated applied to a group is powerful (De Morgan's laws!)
        // e.g., !(a AND b) is the same as (!a OR !b)
        return IsNegated ? (s => !positivePredicate(s)) : positivePredicate;
    }

    public override string Humanize() => $"({string.Join($" {Operator.ToString().ToUpper()} ", Clauses.Select(c => c.Humanize()))})";
}
public abstract class QueryClause
{
    public string FieldName { get; set; }
    public string Keyword { get; set; }
    public string RawValue { get; set; }
    public bool IsNegated { get; set; }
    public abstract Func<SongModelView, bool> AsPredicate();
    public abstract string Humanize();
}

public class TextCondition { public string Value { get; set; } public TextOperator Operator { get; set; } = TextOperator.Contains; }

public class TextClause : QueryClause
{
    public List<TextCondition> Conditions { get; set; } = new();
    public override Func<SongModelView, bool> AsPredicate()
    {
        bool positivePredicate(SongModelView song) => Conditions.Any(cond =>
        {
            var songValue = SemanticQueryHelpers.GetStringProp(song, FieldName);
            return cond.Operator switch
            {
                TextOperator.StartsWith => songValue.StartsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.EndsWith => songValue.EndsWith(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.Equals => songValue.Equals(cond.Value, StringComparison.OrdinalIgnoreCase),
                TextOperator.IsEmpty => string.IsNullOrEmpty(songValue), // NEW
                TextOperator.IsNotEmpty => !string.IsNullOrEmpty(songValue), // NEW
                _ => songValue.Contains(cond.Value, StringComparison.OrdinalIgnoreCase) // Contains is default
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

//#region --- Sorters and Limiters ---
public class SortClause
{
    public string FieldName { get; set; } = null!;
    public SortDirection Direction { get; set; }
}
//public class LimiterClause { public LimiterType Type { get; set; } public int Count { get; set; } }
//#endregion
