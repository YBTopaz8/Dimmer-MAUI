// AstSplitter.cs - CORRECTED
using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Google.Cloud.AIPlatform.V1.RagRetrievalConfig.Types;

namespace Dimmer.DimmerSearch.TQL;

public record SplitAst(IQueryNode DatabaseNode, IQueryNode InMemoryNode);

public static class AstSplitter
{
    public static SplitAst Split(IQueryNode root)
    {
        var dbNode = CloneAndFilter(root, IsDatabaseNode);

        // This is no longer needed as the MetaParser will now use the master AST
        // for the in-memory predicate. We can simplify this.
        var inMemoryNode = new ClauseNode("any", "matchall", ""); // Placeholder

        return new SplitAst(dbNode, inMemoryNode);
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
    // Everything else is considered an in-memory-only leaf node.
    RandomChanceNode => false,
    DaypartNode => false,

    // For logical/container nodes, this isn't a leaf, so we traverse deeper.
    // Returning true here ensures we don't prematurely replace them.
    _ => true
};
}