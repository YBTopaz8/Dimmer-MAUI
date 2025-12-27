using Dimmer.WinUI.Views;

using UiThreads = Dimmer.WinUI.Utils.StaticUtils.UiThreads;
using Window = Microsoft.UI.Xaml.Window;
namespace Dimmer.WinUI.Utils.WinMgt;

public partial class WinUIWindowMgrService : IWinUIWindowMgrService
{
    private readonly IServiceProvider _mauiServiceProvider; // To potentially resolve MAUI services if needed
    private readonly List<Window> _openWindows = new(); // Simple tracking
    private readonly Dictionary<string, Window> _trackedUniqueContentWindows = new();
    private readonly Dictionary<Type, Window> _trackedUniqueTypedWindows = new();

    /// <summary>
    /// Fired just before a window is closed. Can be cancelled by subscribers.
    /// </summary>
    public event EventHandler<WindowClosingEventArgs>? WindowClosing;

    /// <summary>
    /// Fired after a window has been closed.
    /// </summary>
    public event EventHandler<Window>? WindowClosed; // CHANGED: Renamed from WindowClosing for clarity

    /// <summary>
    /// Fired when any tracked window is activated (brought to the foreground).
    /// </summary>

    public event EventHandler<WindowActivatedWithSourceEventArgs>? WindowActivated;
    //public event EventHandler<WindowSizeChangedEventArgs>? WindowSizeChanged;

    /// <summary>
    /// Fired when any tracked window's size is changed.
    /// </summary>
    public event EventHandler<WindowActivatedWithSourceEventArgs>? WindowSizeChanged;


    public WinUIWindowMgrService(IServiceProvider mauiServiceProvider)
    {
        _mauiServiceProvider = mauiServiceProvider;
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
            window.Activate();
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
            window.Activate();
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
            window.Content = grid;

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
            window.Activate();
            Debug.WriteLine($"Native WinUI content window created for page: {pageType.FullName}, Title: {window.Title}");
            return window;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating native WinUI content window for page {pageType.FullName}: {ex.Message}");
            return null;
        }
    }

    public T? GetOrCreateUniqueWindow<T>(BaseViewModelWin? callerVM, Func<T>? windowFactory = null) where T : Window
    {
        if (callerVM == null) return null;

        T targetWindow = null;

        if (_trackedUniqueTypedWindows.TryGetValue(typeof(T), out var existingGenericWindow) && existingGenericWindow is T existingTypedWindow)
        {
            if (IsWindowOpen(existingTypedWindow))
            {
                BringToFront(existingTypedWindow);
                targetWindow = existingTypedWindow;
            }
            else
            {
                _trackedUniqueTypedWindows.Remove(typeof(T));
                UntrackWindow(existingGenericWindow);
            }
        }
        if (targetWindow == null)
        {
            windowFactory ??= () => Activator.CreateInstance<T>();
           
        }
      
            T newWindow = windowFactory();

        void OnNewWindowClosed(object sender, WindowEventArgs args)
        {
            // Unsubscribe to prevent memory leaks
            newWindow.Closed -= OnNewWindowClosed;

            // Bring the original caller back to the front
            try
            {
                //Debug.WriteLine($"Unique window '{newWindow.Title}' closed. Activating caller.");
                //callerVM.ActivateMainWindow();
            }
            catch (Exception ex)
            {
                // This might happen if the caller window was also closed in the meantime.
                Debug.WriteLine($"Could not activate caller window. It might be closed. Error: {ex.Message}");
            }
        }

        newWindow.Closed += OnNewWindowClosed;

        TrackWindow(newWindow);
        _trackedUniqueTypedWindows[typeof(T)] = newWindow;
        newWindow.Activate();
        Debug.WriteLine($"Unique typed window created: {typeof(T).FullName}. It will activate its caller on close.");

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
        else
        {
            return null;
        }
            return newWindow;
    }


    public void TrackWindow(Window window)
    {
        if (_openWindows.Contains(window))
            return;

        _openWindows.Add(window);

        // hook activation
        window.Activated += (s, e) =>
        {
            WindowActivated?.Invoke(this, new WindowActivatedWithSourceEventArgs(window, e.WindowActivationState));
        };

        // hook size changed
        window.SizeChanged += (s, e) =>
        {
            WindowActivatedWithSourceEventArgs ee = new WindowActivatedWithSourceEventArgs(window, WindowActivationState.CodeActivated);
            WindowSizeChanged?.Invoke(this, ee);
        };

        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);


        var presenter = appWindow.Presenter as OverlappedPresenter;
        appWindow.Changed += (sender, args) =>
        {
            var didVisChange = args.DidVisibilityChange;

            WindowActivationState state;
            if (sender.IsVisible)
            {
                state = WindowActivationState.PointerActivated;
            }
            else
            {
                state = WindowActivationState.Deactivated;
            }
            if(didVisChange)
            {
                switch (state)
                {
                    case WindowActivationState.CodeActivated:

                        break;
                    case WindowActivationState.Deactivated:

                        break;
                    default:
                        break;
                }
            }

            var didSizeChange = args.DidSizeChange;
            if(didSizeChange)
            {
                var newSize = sender.Size;
                Debug.WriteLine($"AppWindow size changed. New size: {newSize.Width}x{newSize.Height}");
                // Raise event for size change

                if (window != null)
                {
                    WindowSizeChanged?.Invoke(this, new WindowActivatedWithSourceEventArgs(window, state));
                }
            }
        };
    

        window.Closed += OnWindowClosed;
        // Subscribe to Closed event

    }

    public void UntrackWindow(Window window)
    {
        _openWindows.Remove(window);
        window.Closed -= OnWindowClosed;

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
        if (sender is Window closedWindow)
        {
            WindowDockManager.SaveWindowPosition(closedWindow); // Save position on close
            var customArgs = new WindowClosingEventArgs(closedWindow);
            WindowClosing?.Invoke(this, customArgs); // Notify any subscribers
            UntrackWindow(closedWindow);
            Debug.WriteLine($"Native WinUI window closed and untracked: {closedWindow.Title}");
        }
    }
    // This is a custom EventArgs class you'll need to create for the cancellable event
    public class WindowClosingEventArgs : EventArgs
    {
        public bool Cancel { get; set; } = false;
        public Window Window { get; }

        public WindowClosingEventArgs(Window window)
        {
            Window = window;
        }
    }

    public void BringToFront(Window window)
    {
        EnsureWindowActive(window);
    }
    public void EnsureWindowActive(Window? window)
    {
        if (window == null) return;
        // 1 try to recover a live tracked one first
        if (!_openWindows.Contains(window) || !IsWindowOpen(window))
        {
            
            window.Activate();
            TrackWindow(window);
            return;
        }

        // 2️ check HWND validity
        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        if (hwnd == IntPtr.Zero)
        {
            window = new DimmerWin();
            TrackWindow(window);
            window.Activate();
            return;
        }

        // 3️ validate AppWindow state
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);
        if (appWindow == null)
        {
            window = new DimmerWin();
            TrackWindow(window);
            window.Activate();
            return;
        }

        // 4️ normal restore / bring-front
        if (appWindow.Presenter is OverlappedPresenter p && p.State == OverlappedPresenterState.Minimized)
            p.Restore();

        appWindow.MoveInZOrderAtTop();
        window.Activate();
        return;
    }

    public void ActivateWindow(Window window)
    {
        if (IsWindowOpen(window))
        {
            window.Activate();
            Debug.WriteLine($"Native WinUI window activated: {window.Title}");
        }
    }
    private static bool IsWindowOpen(Window window)
    {

        IntPtr hwnd = WindowNative.GetWindowHandle(window);
        WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appWindow = AppWindow.GetFromWindowId(id);

        
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            
            switch (presenter.State)
            {
                case OverlappedPresenterState.Maximized:
                    return true;

                case OverlappedPresenterState.Minimized:
                    return false;
                case OverlappedPresenterState.Restored:
                    return true;
                default:
                    break;
            }
        }
        return false;
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



    public IReadOnlyList<Window> GetOpenNativeWindows()
    {
        // Returns a copy of the tracked windows. This list's accuracy depends on the Closed event.
        return _openWindows.ToList().AsReadOnly();
    }

    public void CloseAllWindows(Window? callWindow)
    {
        if (callWindow is not null)
        {
            _openWindows.Remove(callWindow);
        }
        foreach (var window in _openWindows.ToList())
        {
            CloseWindow(window);

            _openWindows.Remove(window);
        }
    }

    public void CloseWindow(Window window)
    {
        try
        {
            if (!_openWindows.Contains(window)) return;
            if (IsWindowOpen(window)) // Check if we are tracking it as open
            {
                window.Close(); // This will trigger the Destroying event which will handle untracking
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

}


