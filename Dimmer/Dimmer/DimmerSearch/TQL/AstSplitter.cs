namespace Dimmer.DimmerSearch.TQL;

public record SplitAst(IQueryNode DatabaseNode, IQueryNode InMemoryNode);

public static class AstSplitter
{
    public static SplitAst Split(IQueryNode root)
    {
        var normalized = ToNegationNormalForm(root);
        var dbNode = CloneAndFilter(normalized, IsDatabaseNode);
        var inMemoryNode = GenerateInMemoryTree(normalized);
        return new SplitAst(dbNode, inMemoryNode);
    }

    /// <summary>
    /// Pushes NotNode down to leaf nodes via De Morgan's Laws so that
    /// replacing non-DB leaves with TRUEPREDICATE is always a sound over-approximation.
    /// </summary>
    private static IQueryNode ToNegationNormalForm(IQueryNode node, bool negate = false)
    {
        return node switch
        {
            NotNode n => ToNegationNormalForm(n.NodeToNegate, !negate),

            LogicalNode n => new LogicalNode(
                ToNegationNormalForm(n.Left, negate),
                negate
                    ? (n.Operator == LogicalOperator.And ? LogicalOperator.Or : LogicalOperator.And)
                    : n.Operator,
                ToNegationNormalForm(n.Right, negate)),

            ClauseNode c when c.Operator == "matchall" => c,

            ClauseNode c => negate
                ? new ClauseNode(c.Field, c.Operator, c.Value, c.UpperValue, !c.IsNegated)
                : c,

            InNode i => negate
                ? new InNode(i.Field, i.Values.ToList(), !i.IsNegated)
                : i,

            _ => negate ? new NotNode(node) : node
        };
    }

    private static IQueryNode GenerateInMemoryTree(IQueryNode node)
    {
        return node switch
        {
            LogicalNode n => new LogicalNode(GenerateInMemoryTree(n.Left), n.Operator, GenerateInMemoryTree(n.Right)),
            NotNode n when IsDatabaseNode(n.NodeToNegate) => new ClauseNode("any", "matchall", ""),
            NotNode n => new NotNode(GenerateInMemoryTree(n.NodeToNegate)),
            RandomChanceNode or DaypartNode => node,
            _ when IsDatabaseNode(node) => new ClauseNode("any", "matchall", ""),
            _ => node
        };
    }

    private static IQueryNode CloneAndFilter(IQueryNode node, Func<IQueryNode, bool> filter)
    {
        return node switch
        {
            LogicalNode n => new LogicalNode(
                CloneAndFilter(n.Left, filter),
                n.Operator,
                CloneAndFilter(n.Right, filter)),

            // After NNF, NotNode only ever wraps a non-Clause/non-In leaf (Daypart, RandomChance, FuzzyDate).
            // Since those aren't DB nodes, return matchall so we never exclude valid records at the DB pass.
            NotNode => new ClauseNode("any", "matchall", ""),

            _ => filter(node) ? node : new ClauseNode("any", "matchall", "")
        };
    }

    private static bool IsDatabaseNode(IQueryNode node) => node switch
    {
        ClauseNode c when FieldRegistry.FieldsByAlias.TryGetValue(c.Field, out var def) && def.Type == FieldType.Date => false,
        ClauseNode => true,
        InNode i when FieldRegistry.FieldsByAlias.TryGetValue(i.Field, out var def) && def.Type != FieldType.Date => true,
        FuzzyDateNode => false,
        _ => false
    };
}