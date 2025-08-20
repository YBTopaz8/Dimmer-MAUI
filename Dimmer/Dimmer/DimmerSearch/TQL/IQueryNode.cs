namespace Dimmer.DimmerSearch.TQL;
public interface IQueryNode
{
}
public class NotNode : IQueryNode
{
    public IQueryNode NodeToNegate { get; }

    public NotNode(IQueryNode node)
    {
        NodeToNegate = node;
    }
}
public enum LogicalOperator { And, Or }

public class RandomChanceNode : IQueryNode
{
    public int Percentage { get; }
    public bool IsFrozen { get; }

    public RandomChanceNode(int percentage, bool isFrozen=false)
    {
        Percentage = Math.Clamp(percentage, 0, 100);
        IsFrozen = isFrozen;
    }
}

public class FuzzyDateNode : IQueryNode
{
    public enum Qualifier { Ago, Between, Never }
    public string DateField { get; }
    public Qualifier Type { get; }
    public string Operator { get; } 
    public TimeSpan? OlderThan { get; }
    public TimeSpan? NewerThan { get; }

    public FuzzyDateNode(string dateField, Qualifier type, string op, TimeSpan? olderThan = null, TimeSpan? newerThan = null)
    {
        DateField = dateField;
        Type = type;
        Operator = op; 
        OlderThan = olderThan;
        NewerThan = newerThan;
    }
}

public class DaypartNode : IQueryNode
{
    public string DateField { get; }
    public TimeSpan StartTime { get; }
    public TimeSpan EndTime { get; }
    public DaypartNode(string dateField, TimeSpan startTime, TimeSpan endTime)
    {
        DateField = dateField;
        StartTime = startTime;
        EndTime = endTime;
    }
}


public class LogicalNode : IQueryNode
{
    public IQueryNode Left { get; }
    public LogicalOperator Operator { get; }
    public IQueryNode Right { get; }

    public LogicalNode(IQueryNode left, LogicalOperator op, IQueryNode right)
    {
        Left = left;
        Operator = op;
        Right = right;
    }


}



public class SortNode : IQueryNode
{
    public string Field { get; }
    public bool Descending { get; }

    public SortNode(string field, bool descending)
    {
        Field = field;
        Descending = descending;
    }
}
public class GroupNode : IQueryNode
{
    public string Field { get; }
    public GroupNode(string field) => Field = field;
}
public class StatsCommandNode : IQueryNode
{
    public IQueryNode FilterNode { get; }
    public StatsCommandNode(IQueryNode filterNode)
    {
        FilterNode = filterNode;
    }
}
public class CommandNode : IQueryNode
{
    public string Command { get; }
    public Dictionary<string, object> Arguments { get; }
    public CommandNode(string command, Dictionary<string, object> arguments)
    {
        Command = command;
        Arguments = arguments;
    }
}


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