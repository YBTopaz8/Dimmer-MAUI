using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.AbstractQueryTree;
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