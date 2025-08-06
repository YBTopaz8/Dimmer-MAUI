namespace Dimmer.WinUI.Utils.WinMgt;
public interface IWindowManagerService
{
    // Create a window by its direct type
    T? CreateWindow<T>() where T : Window, new();
    T? CreateWindow<T>(object? parameter) where T : Window; // If constructor takes a parameter

    // Create a window that hosts a specific Page type
    Window? CreateContentWindow(Type pageType, object? navigationParameter = null, string? title = null);

    // Get or create a unique window by its type (e.g., only one Settings native window)
    T? GetOrCreateUniqueWindow<T>(Func<T>? windowFactory = null) where T : Window;
    Window? GetOrCreateUniqueContentWindow(Type pageType, string uniqueId, object? navigationParameter = null, string? title = null, Func<Window>? windowFactory = null);

    T? GetWindow<T>() where T : Window;
    Window? GetContentWindowByPageType(Type pageType);
    IReadOnlyList<Window> GetOpenNativeWindows(); // Note: This might be tricky to track perfectly without more involved OS-level enumeration
    void CloseWindow(Window window);
    void CloseWindow<T>() where T : Window;
    void BringToFront(Window window);
}