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
