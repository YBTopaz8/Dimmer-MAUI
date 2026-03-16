namespace Dimmer.WinUI.Utils.StaticUtils;

public static partial class UiThreads
{
    
    public static DispatcherQueue WinUI { get; set; } = null!;



    private static DispatcherQueue? _winUI;

    public static void EnsureInitialized()
    {
        if (_winUI != null) return;

        var dispatcher = DispatcherQueue.GetForCurrentThread();

        if (dispatcher == null)
        {

            Debug.WriteLine("[CRITICAL] Could not capture DispatcherQueue. Ensure this is called from the Main UI Thread.");
            return;
        }

        _winUI = dispatcher;

        Utilities.Extensions.UiThreads.DispatchAction = action =>
        {
            try
            {
                if (_winUI.HasThreadAccess)
                {
                    action();
                }
                else
                {
                    bool enqueued = _winUI.TryEnqueue(() =>
                    {
                        try { action(); }
                        catch (Exception ex) { Debug.WriteLine($"[UI Exception] {ex}"); }
                    });

                    if (!enqueued)
                    {
                        // If this hits, the App is likely shutting down.
                        Debug.WriteLine($"[CRITICAL] Failed to enqueue on UI Thread.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FATAL] Dispatcher logic failed: {ex}");
            }
        };
    }
}
