using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;

public static partial class UiThreads
{
    public static Action<Action> DispatchAction { get; set; }

}