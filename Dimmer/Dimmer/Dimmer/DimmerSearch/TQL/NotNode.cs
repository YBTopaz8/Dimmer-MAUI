using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.TQL;
public class NotNode : IQueryNode
{
    public IQueryNode NodeToNegate { get; }

    public NotNode(IQueryNode node)
    {
        NodeToNegate = node;
    }
}