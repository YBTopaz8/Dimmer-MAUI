namespace Dimmer.WinUI.Utils.StaticUtils;

public static partial class UiThreads
{
    
    public static DispatcherQueue WinUI { get; set; } = null!;

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
