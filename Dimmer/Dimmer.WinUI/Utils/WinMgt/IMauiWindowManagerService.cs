using MWindow = Microsoft.Maui.Controls.Window;

namespace Dimmer.WinUI.Utils.WinMgt;
public interface IMauiWindowManagerService
{
    // Create a window by its direct type
    T? CreateWindow<T>() where T : MWindow, new();
    T? CreateWindow<T>(object? parameter) where T : MWindow; // If constructor takes a parameter

    // Create a window that hosts a specific Page type
    MWindow? CreateContentWindow(Type pageType, object? navigationParameter = null, string? title = null);

    // Get or create a unique window by its type (e.g., only one Settings native window)
    T? GetOrCreateUniqueWindow<T>(Func<T>? windowFactory = null) where T : MWindow;
    MWindow? GetOrCreateUniqueContentWindow(Type pageType, string uniqueId, object? navigationParameter = null, string? title = null, Func<MWindow>? windowFactory = null);

    T? GetWindow<T>() where T : MWindow;
    MWindow? GetContentWindowByPageType(Type pageType);
    IReadOnlyList<MWindow> GetOpenNativeWindows(); // Note: This might be tricky to track perfectly without more involved OS-level enumeration
    void CloseWindow(MWindow window);
    void CloseWindow<T>() where T : MWindow;
    void BringToFront(MWindow window);
    void UntrackWindow(MWindow window);
    void TrackWindow(MWindow window);
    void CloseAllWindows();
    void ActivateWindow(MWindow window);
}