namespace Dimmer.Utils;

public static partial class UiThreads
{
    public static Handler AndUI { get; set; } = null!;

    // Expose the main-thread Dispatcher equivalent
    public static void InitializeMainHandler()
    {
        if(Looper.MainLooper == null)
            return;
        AndUI ??= new Handler(Looper.MainLooper);

        Dimmer.Utilities.Extensions.UiThreads.DispatchAction = action =>
        {
            if (Looper.MyLooper() == Looper.MainLooper)
            {
                // Already on main thread
                action();
            }
            else
            {
                // Post to main thread
                AndUI.Post(action);
            }
        };
    }
}