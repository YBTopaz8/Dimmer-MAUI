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
    public RandomChanceNode(int percentage)
    {
        Percentage = Math.Clamp(percentage, 0, 100);
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
