using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;

/// <summary>
/// Represents a leaf node in the tree—a specific, concrete condition.
/// Examples: artist:tool, year:>2000, fav:true
/// </summary>
public class ClauseNode : IQueryNode
{
    public string Field { get; }
    public string Operator { get; }
    public object Value { get; }
    // An optional second value for range operations (e.g., year:2000-2010)
    public object? UpperValue { get; }
    public bool IsNegated { get; }
    public ClauseNode(string field, string op, object value, object? upperValue = null, bool isNegated = false)
    {
        Field = field;
        Operator = op;
        Value = value;
        UpperValue = upperValue;
        IsNegated = isNegated;
    }
}