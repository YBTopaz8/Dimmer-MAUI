namespace Dimmer.DimmerSearch.TQL;
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