// AstSplitter.cs - CORRECTED
namespace Dimmer.DimmerSearch.TQL;

public record SplitAst(IQueryNode DatabaseNode, IQueryNode InMemoryNode);

public static class AstSplitter
{
    public static SplitAst Split(IQueryNode root)
    {
        var dbNode = CloneAndFilter(root, IsDatabaseNode);

        // This is no longer needed as the MetaParser will now use the master AST
        // for the in-memory predicate. We can simplify this.
        var inMemoryNode = GenerateInMemoryTree(root);
        return new SplitAst(dbNode, inMemoryNode);
    }
    private static IQueryNode GenerateInMemoryTree(IQueryNode node)
    {
        switch (node)
        {
            case LogicalNode n:
                return new LogicalNode(GenerateInMemoryTree(n.Left), n.Operator, GenerateInMemoryTree(n.Right));

            case NotNode n:
                // If we are negating a DB node, the DB handled the exclusion. 
                // In memory, we treat this as "True" (Pass).
                if (IsDatabaseNode(n.NodeToNegate))
                    return new ClauseNode("any", "matchall", "");

                return new NotNode(GenerateInMemoryTree(n.NodeToNegate));

            case RandomChanceNode:
            case DaypartNode:
                // Keep these for memory execution
                return node;

            default:
                // If it's a DB node (Clause/FuzzyDate), it's already filtered. 
                // Return "True" so we don't double-check it.
                if (IsDatabaseNode(node))
                    return new ClauseNode("any", "matchall", "");

                return node;
        }
    }
    private static IQueryNode CloneAndFilter(IQueryNode node, Func<IQueryNode, bool> filter)
    {
        
        switch (node)
        {
        
            case LogicalNode n:
                return new LogicalNode(
                    CloneAndFilter(n.Left, filter),
                    n.Operator,
                    CloneAndFilter(n.Right, filter));

            case NotNode n:
                {
                    var filteredChild = CloneAndFilter(n.NodeToNegate, filter);

            
                    if (filteredChild is ClauseNode { Operator: "matchall" })
                    {
                        return filteredChild; // Return the matchall node, effectively removing the NOT clause.
                    }

                    
                    return new NotNode(filteredChild);
                }

            
            default:
                return filter(node) ? node : new ClauseNode("any", "matchall", "");
        }
    }

    private static bool IsDatabaseNode(IQueryNode node) => node switch
{
    ClauseNode => true,
    FuzzyDateNode => true,
   

    // For logical/container nodes, this isn't a leaf, so we traverse deeper.
    // Returning true here ensures we don't prematurely replace them.
    _ => false
};
}