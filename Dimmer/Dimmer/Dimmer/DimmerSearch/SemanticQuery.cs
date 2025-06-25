// --- File: Dimmer/DimmerSearch/SemanticQuery.cs ---
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dimmer.DimmerSearch
{
    // The root object representing the entire parsed query.
    public class SemanticQuery
    {
        public List<QueryClause> Clauses { get; } = new();

        public List<string> GeneralAndTerms { get; } = new();

        public List<string> GeneralOrTerms { get; } = new();

        public string Humanize()
        {
            if(!Clauses.Any() && !GeneralAndTerms.Any() && !GeneralOrTerms.Any())
                return "Searching for everything...";

            var sb = new StringBuilder("Find songs");
            var parts = Clauses.Select(c => c.Humanize()).ToList();

            if(GeneralOrTerms.Any())
                parts.Add($"containing any of the words: '{string.Join("', '", GeneralOrTerms)}'");
            if(GeneralAndTerms.Any())
                parts.Add($"containing all of the words: '{string.Join("', '", GeneralAndTerms)}'");

            sb.Append(" " + string.Join(", and ", parts));
            return sb.ToString();
        }
    }

    // Enums to define the types of operations.
    public enum TextOperator
    {
        Contains,
        StartsWith,
        EndsWith,
        Or,
        Levenshtein,
        Equals
    }

    public enum NumericOperator
    {
        Equals,
        GreaterThan,
        LessThan,
        Between
    }

    // Base class for all structured query parts.
    public abstract class QueryClause
    {
        public string FieldName { get; set; }

        public string Keyword { get; set; }

        public string RawValue { get; set; }

        public bool IsNegated { get; set; }

        public abstract Func<SongModelView, bool> AsPredicate();

        public abstract string Humanize();
    }

    // Represents a text-based clause, e.g., artist:drake|rihanna
    public class TextClause : QueryClause
    {
        public List<string> Values { get; set; } = new();

        public TextOperator Operator { get; set; }

        public override Func<SongModelView, bool> AsPredicate()
        {
            Func<SongModelView, bool> predicate = Operator switch
            {
                TextOperator.Or => song => Values.Any(
                    v => SemanticQueryHelpers.GetStringProp(song, FieldName)
                        .Contains(v, StringComparison.OrdinalIgnoreCase)),
                TextOperator.StartsWith => song => SemanticQueryHelpers.GetStringProp(song, FieldName)
                    .StartsWith(Values.First(), StringComparison.OrdinalIgnoreCase),
                TextOperator.EndsWith => song => SemanticQueryHelpers.GetStringProp(song, FieldName)
                    .EndsWith(Values.First(), StringComparison.OrdinalIgnoreCase),
                TextOperator.Levenshtein => song => SemanticQueryHelpers.LevenshteinDistance(
                        SemanticQueryHelpers.GetStringProp(song, FieldName),
                        Values.First()) <=
                    2,
                _ => song => SemanticQueryHelpers.GetStringProp(song, FieldName)
                    .Contains(Values.First(), StringComparison.OrdinalIgnoreCase)
            };
            return IsNegated ? (s => !predicate(s)) : predicate;
        }

        public override string Humanize()
        {
            string verb = Operator switch
            {
                TextOperator.Or => "is one of",
                TextOperator.StartsWith => "starts with",
                TextOperator.EndsWith => "ends with",
                TextOperator.Levenshtein => "sounds like",
                _ => "contains"
            };
            string valueStr = string.Join("' or '", Values);
            return $"where the {FieldName} {(IsNegated ? "does not" : "")} {verb} '{valueStr}'";
        }
    }

    // Represents a numeric clause, e.g., year:>2010
    public class NumericClause : QueryClause
    {
        public double? Value { get; set; }

        public double? UpperValue { get; set; } // For ranges

        public NumericOperator Operator { get; set; }

        public override Func<SongModelView, bool> AsPredicate()
        {
            Func<SongModelView, bool> predicate = s => false;
            if(Value.HasValue)
            {
                predicate = Operator switch
                {
                    NumericOperator.Between => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) >= Value &&
                        SemanticQueryHelpers.GetNumericProp(song, FieldName) <= UpperValue,
                    NumericOperator.GreaterThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) > Value,
                    NumericOperator.LessThan => song => SemanticQueryHelpers.GetNumericProp(song, FieldName) < Value,
                    _ => song => Math.Abs(SemanticQueryHelpers.GetNumericProp(song, FieldName) - Value.Value) < 0.001
                };
            }
            return IsNegated ? (s => !predicate(s)) : predicate;
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
            return $"where the {FieldName} {(IsNegated ? "is not" : "")} {opStr}";
        }
    }

    // Represents a boolean clause, e.g., explicit:true
    public class FlagClause : QueryClause
    {
        public bool TargetValue { get; set; }

        public override Func<SongModelView, bool> AsPredicate()
        {
            Func<SongModelView, bool> predicate = song => SemanticQueryHelpers.GetBoolProp(song, FieldName) ==
                TargetValue;
            return IsNegated ? (s => !predicate(s)) : predicate;
        }

        public override string Humanize() { return $"where {FieldName} is {(IsNegated ? !TargetValue : TargetValue)}"; }
    }
}