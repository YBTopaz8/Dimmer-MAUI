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
        var dbNode = CloneFor(root, node => IsDatabaseNode(node));
        var memNode = CloneFor(root, node => !IsDatabaseNode(node));
        return new SplitAst(dbNode, memNode);
    }

    // This is the key function. It recursively clones the AST.
    // If a node doesn't match the filter, it's replaced with a "pass-through" node.
    private static IQueryNode CloneFor(IQueryNode node, Func<IQueryNode, bool> filter)
    {
        if (filter(node))
        {
            // This node belongs in this tree. If it's a logical node, we need to
            // recurse into its children.
            return node switch
            {
                LogicalNode n => new LogicalNode(CloneFor(n.Left, filter), n.Operator, CloneFor(n.Right, filter)),
                NotNode n => new NotNode(CloneFor(n.NodeToNegate, filter)),
                _ => node // Leaf nodes (Clause, FuzzyDate) are copied as-is.
            };
        }

        // This node does NOT belong in this tree. Replace it with a "match all".
        return new ClauseNode("any", "matchall", "");
    }

    // Defines which nodes can be translated to RQL.
    private static bool IsDatabaseNode(IQueryNode node) => node switch
    {
        ClauseNode => true,
        FuzzyDateNode => true,
        LogicalNode => true, // Logical nodes are containers; their children determine their final state.
        NotNode => true,
        RandomChanceNode => false, // In-memory only
        DaypartNode => false, // In-memory only
        _ => true
    };
}