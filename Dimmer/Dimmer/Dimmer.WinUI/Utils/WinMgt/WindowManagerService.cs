using Microsoft.UI.Xaml;

using Application = Microsoft.Maui.Controls.Application;
using Page = Microsoft.Maui.Controls.Page;
using Window = Microsoft.Maui.Controls.Window;


namespace Dimmer.WinUI.Utils.WinMgt;
internal class WindowManagerService : IWindowManagerService
{
    private readonly IServiceProvider _mauiServiceProvider; // To potentially resolve MAUI services if needed
    private readonly List<Window> _openWindows = new(); // Simple tracking
    private readonly Dictionary<string, Window> _trackedUniqueContentWindows = new();
    private readonly Dictionary<Type, Window> _trackedUniqueTypedWindows = new();


    // Constructor might take IServiceProvider if native pages need MAUI services
    public WindowManagerService(IServiceProvider mauiServiceProvider)
    {
        _mauiServiceProvider = mauiServiceProvider; // From MAUI's DI
    }

    /// <summary>
    /// Creates and activates a new native WinUI window of a specific type.
    /// </summary>
    public T? CreateWindow<T>() where T : Window, new()
    {
        try
        {
            var window = new T();
            TrackWindow(window);
            Application.Current!.ActivateWindow(window);
            Debug.WriteLine($"Native WinUI window created and activated: {typeof(T).FullName}");
            return window;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating native WinUI window {typeof(T).FullName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Creates and activates a new native WinUI window of a specific type, passing a parameter to its constructor.
    /// </summary>
    public T? CreateWindow<T>(object? parameter) where T : Window
    {
        try
        {
            // This assumes T has a constructor that accepts the parameter's type.
            // More robust would be to have specific factory methods or use a DI container that can handle parameterized resolution.
            var window = (T?)Activator.CreateInstance(typeof(T), parameter);
            if (window == null)
            {
                Debug.WriteLine($"Could not create instance of {typeof(T).FullName} with parameter.");
                return null;
            }
            TrackWindow(window);

            Application.Current!.ActivateWindow(window);
            Debug.WriteLine($"Native WinUI window created with parameter and activated: {typeof(T).FullName}");
            return window;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating native WinUI window {typeof(T).FullName} with parameter: {ex.Message}");
            return null;
        }
    }


    /// <summary>
    /// Creates a generic native WinUI Window that hosts a specific native WinUI Page.
    /// </summary>
    public Window? CreateContentWindow(Type pageType, object? navigationParameter = null, string? title = null)
    {
        if (!typeof(Page).IsAssignableFrom(pageType))
        {
            Debug.WriteLine($"Error: {pageType.FullName} is not a valid Microsoft.UI.Xaml.Controls.Page type.");
            return null;
        }

        try
        {
            var window = new Window(); // Generic host window
            var grid = new Page(); // Create a Frame to host the Page
            window.Page = grid;

            Page? pageInstance = null;
            try
            {
                // Attempt to resolve from MAUI's service provider if the native Page was registered there (less common but possible)
                pageInstance = _mauiServiceProvider.GetService(pageType) as Page;
                if (pageInstance != null)
                    Debug.WriteLine($"Resolved native Page {pageType.FullName} from MAUI DI.");
            }
            catch (Exception diEx)
            {
                Debug.WriteLine($"Could not resolve native Page {pageType.FullName} from MAUI DI: {diEx.Message}. Falling back.");
            }

            if (pageInstance == null)
            {
                pageInstance = Activator.CreateInstance(pageType) as Page;
                Debug.WriteLine($"Created native Page {pageType.FullName} via Activator.");
            }


            if (pageInstance == null)
            {
                Debug.WriteLine($"Error: Could not create instance of native Page {pageType.FullName}.");
                return null;
            }


            if (!string.IsNullOrEmpty(title))
            {
                window.Title = title;
            }



            TrackWindow(window);

            Application.Current!.ActivateWindow(window);
            Debug.WriteLine($"Native WinUI content window created for page: {pageType.FullName}, Title: {window.Title}");
            return window;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating native WinUI content window for page {pageType.FullName}: {ex.Message}");
            return null;
        }
    }

    public T? GetOrCreateUniqueWindow<T>(Func<T>? windowFactory = null) where T : Window
    {
        if (_trackedUniqueTypedWindows.TryGetValue(typeof(T), out var existingGenericWindow) && existingGenericWindow is T existingTypedWindow)
        {
            if (IsWindowOpen(existingTypedWindow))
            {
                BringToFront(existingTypedWindow);
                Debug.WriteLine($"Unique typed window {typeof(T).FullName} already exists. Bringing to front.");
                return existingTypedWindow;
            }
            else
            {
                _trackedUniqueTypedWindows.Remove(typeof(T));
                UntrackWindow(existingGenericWindow);
            }
        }

        if (windowFactory == null)
            throw new ArgumentNullException(nameof(windowFactory), $"No factory provided and no parameterless constructor for {typeof(T).Name}");

        T newWindow = windowFactory();
        TrackWindow(newWindow);
        _trackedUniqueTypedWindows[typeof(T)] = newWindow;

        Application.Current!.OpenWindow(newWindow);
        Debug.WriteLine($"Unique typed window created and activated: {typeof(T).FullName}");

        return newWindow;
    }


    public Window? GetOrCreateUniqueContentWindow(Type pageType, string uniqueId, object? navigationParameter = null, string? title = null, Func<Window>? windowFactory = null)
    {
        if (_trackedUniqueContentWindows.TryGetValue(uniqueId, out var existingWindow))
        {
            if (IsWindowOpen(existingWindow))
            {
                BringToFront(existingWindow);
                Debug.WriteLine($"Unique content window '{uniqueId}' for page {pageType.Name} already exists. Bringing to front.");
                return existingWindow;
            }
            else
            {
                _trackedUniqueContentWindows.Remove(uniqueId);
                UntrackWindow(existingWindow);
            }
        }

        Window? newWindow = windowFactory != null ? windowFactory() : CreateContentWindow(pageType, navigationParameter, title);
        if (newWindow != null)
        {
            // TrackWindow was already called by CreateContentWindow
            _trackedUniqueContentWindows[uniqueId] = newWindow;
            // Ensure base tracking also happens if windowFactory was used and didn't call TrackWindow
            if (windowFactory != null && !_openWindows.Contains(newWindow))
            {
                TrackWindow(newWindow);
            }
        }
        return newWindow;
    }


    private void TrackWindow(Window window)
    {
        if (!_openWindows.Contains(window))
        {
            _openWindows.Add(window);
            window.Destroying += Window_Destroying;
            ; // Subscribe to Closed event
        }
    }

    private void Window_Destroying(object? sender, EventArgs e)
    {
        if (sender is Window closedWindow)
        {
            UntrackWindow(closedWindow);
            Debug.WriteLine($"Native WinUI window closed and untracked: {closedWindow.Title}");
        }
    }

    private void UntrackWindow(Window window)
    {
        _openWindows.Remove(window);
        window.Destroying -= Window_Destroying; // Unsubscribe

        // Also remove from unique tracking if it was there
        var uniqueTypedKey = _trackedUniqueTypedWindows.FirstOrDefault(kvp => kvp.Value == window).Key;
        if (uniqueTypedKey != null)
            _trackedUniqueTypedWindows.Remove(uniqueTypedKey);

        var uniqueContentKey = _trackedUniqueContentWindows.FirstOrDefault(kvp => kvp.Value == window).Key;
        if (uniqueContentKey != null)
            _trackedUniqueContentWindows.Remove(uniqueContentKey);
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {

    }

    private bool IsWindowOpen(Window window)
    {
        // A simple check is if it's in our tracked list.
        // For a more robust check, you might need to see if its AppWindow is visible,
        // but that adds complexity with AppWindow.
        // For now, if it's in _openWindows, we assume it was opened by us and Closed event handles removal.
        // However, a window could be closed by user an not yet processed by Closed event if called rapidly.
        // A truly robust check might involve checking window.Content == null or window.Visible (if available directly)
        // or querying existing AppWindows (more advanced).
        // For now, rely on our _openWindows list which is maintained by Closed event.
        return _openWindows.Contains(window);
    }


    public T? GetWindow<T>() where T : Window
    {
        // If it was uniquely tracked by type
        if (_trackedUniqueTypedWindows.TryGetValue(typeof(T), out var uniqueWindow) && uniqueWindow is T typedUniqueWindow && IsWindowOpen(typedUniqueWindow))
        {
            return typedUniqueWindow;
        }
        // Fallback to general list
        return _openWindows.OfType<T>().FirstOrDefault(w => IsWindowOpen(w));
    }

    public Window? GetContentWindowByPageType(Type pageType)
    {
        return _openWindows.FirstOrDefault(w => w.Page is Page frame && frame.GetType() == pageType && IsWindowOpen(w));
    }


    public IReadOnlyList<Window> GetOpenNativeWindows()
    {
        // Returns a copy of the tracked windows. This list's accuracy depends on the Closed event.
        return _openWindows.ToList().AsReadOnly();
    }

    public void CloseWindow(Window window)
    {
        try
        {
            if (IsWindowOpen(window)) // Check if we are tracking it as open
            {

                Application.Current!.CloseWindow(window);
                Debug.WriteLine($"Native WinUI window close requested: {window.Title}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error closing native WinUI window {window.Title}: {ex.Message}");
        }
    }

    public void CloseWindow<T>() where T : Window
    {
        var window = GetWindow<T>();
        if (window != null)
        {
            CloseWindow(window);
        }
    }

    public void BringToFront(Window window)
    {
        if (IsWindowOpen(window))
        {
            // The native WinUI Window object itself is the one to activate

            Application.Current!.ActivateWindow(window);
            Debug.WriteLine($"Attempted to bring native WinUI window to front: {window.Title}");

            // For a more forceful bring to front:
            // var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            // NativeMethods.SetForegroundWindow(hwnd);
        }

    }
}



internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);
}


