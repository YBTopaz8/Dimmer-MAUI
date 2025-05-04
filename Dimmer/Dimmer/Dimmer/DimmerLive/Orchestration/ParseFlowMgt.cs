using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Orchestration;
public class ParseFlowMgt
{
    private readonly SubscriptionManager _subs;

    public ParseFlowMgt(
        SubscriptionManager subs)
    {

        _subs   = subs;
    }
}
