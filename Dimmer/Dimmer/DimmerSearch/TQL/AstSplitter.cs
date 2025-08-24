using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL;

public record SplitAst(IQueryNode DatabaseNode, IQueryNode InMemoryNode);

public static class AstSplitter
{
    public static SplitAst Split(IQueryNode root)
    {
        var dbNode = CloneAndFilter(root, IsDatabaseNode);
        var inMemoryNode = CloneAndFilter(root, node => !IsDatabaseNode(node));
        return new SplitAst(dbNode, inMemoryNode);
    }

    // This is the key function. It always traverses the full tree structure.
    // The keep/replace decision is only made for leaf nodes.
    private static IQueryNode CloneAndFilter(IQueryNode node, Func<IQueryNode, bool> filter)
    {
        return node switch
        {
            // For container nodes, always recurse.
            LogicalNode n => new LogicalNode(
                CloneAndFilter(n.Left, filter),
                n.Operator,
                CloneAndFilter(n.Right, filter)),

            NotNode n => new NotNode(CloneAndFilter(n.NodeToNegate, filter)),

            // For leaf nodes, apply the filter.
            // If it passes, keep the node. If not, replace it.
            _ => filter(node) ? node : new ClauseNode("any", "matchall", "")
        };
    }

    // This now only needs to define the LEAF nodes that go to the database.
    private static bool IsDatabaseNode(IQueryNode node) => node switch
    {
        ClauseNode => true,
        FuzzyDateNode => true,
        // Everything else is considered an in-memory leaf node.
        RandomChanceNode => false,
        DaypartNode => false,

        _ => true // Default to true for any other unknown node type
    };
}