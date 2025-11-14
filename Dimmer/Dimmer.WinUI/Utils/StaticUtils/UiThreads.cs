using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.StaticUtils;

public static partial class UiThreads
{
    
    public static DispatcherQueue WinUI { get; set; }
    
    public static void InitializeWinUIDispatcher(DispatcherQueue dispatcher)
    {
        WinUI = dispatcher;

        Dimmer.Utilities.Extensions.UiThreads.DispatchAction = action =>
        {
            if (WinUI.HasThreadAccess)
                action();
            else
                WinUI.TryEnqueue(()=>action());
        };
    }
}
