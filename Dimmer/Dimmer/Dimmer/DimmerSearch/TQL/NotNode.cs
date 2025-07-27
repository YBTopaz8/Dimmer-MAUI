namespace Dimmer.DimmerSearch.TQL;
public class NotNode : IQueryNode
{
    public IQueryNode NodeToNegate { get; }

    public NotNode(IQueryNode node)
    {
        NodeToNegate = node;
    }
}