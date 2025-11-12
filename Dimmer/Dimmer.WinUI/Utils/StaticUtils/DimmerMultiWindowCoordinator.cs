using Windows.Graphics;
using CommunityToolkit.Diagnostics;
using Windows.Foundation;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using AppWindowChangedEventArgs = Microsoft.UI.Windowing.AppWindowChangedEventArgs;

namespace Dimmer.WinUI.Utils.StaticUtils;

public class DimmerMultiWindowCoordinator
{
    private readonly IWinUIWindowMgrService _mgr;
    private readonly ObservableCollection<WindowEntry> _windows = new();
    private Microsoft.UI.Xaml.Window? _homeWindow;
    public bool ReturnToHomeOnClose { get; set; } = true;

    public ReadOnlyObservableCollection<WindowEntry> Windows { get; }
    public BaseViewModelWin? BaseVM { get; set; }
    public DimmerMultiWindowCoordinator(IWinUIWindowMgrService mgr)
    {
        _mgr = mgr;
        
        Windows = new ReadOnlyObservableCollection<WindowEntry>(_windows);

        foreach (var win in _mgr.GetOpenNativeWindows())
            TrackWindow(win);
        _mgr.WindowActivated += (_, e) =>
        {
            var win = e.Window; // this will be the Window that just got focus/opened
            if (win != null && !_windows.Any(x => x.Window == win))
                TrackWindow(win);
        };
        _mgr.WindowSizeChanged += async (_, e) =>
        {
            if (_homeWindow != null)
              await  WindowDockManager.SnapHomeWindowAsync(_homeWindow, _mgr.GetOpenNativeWindows());
        };

        _mgr.WindowClosed += (_, w) => UntrackWindow(w);
    }

    public void SetHomeWindow(Window home)
    {
        _homeWindow = home;
        Debug.WriteLine($"[Coordinator] Home window set: {home.Title}");
    }

    public void TrackWindow(Window win)
    {
        if (_windows.Any(x => x.Window == win)) return;
        if (_homeWindow is null) return;
        _homeWindow.DispatcherQueue.TryEnqueue(() =>
        {

            var entry = new WindowEntry(win);
            _windows.Add(entry);
        });
        var appWin = PlatUtils.GetAppWindow(win);

        TypedEventHandler<AppWindow, AppWindowChangedEventArgs>? handler = null;
        handler = async (sender, args) =>
        {
            if (_homeWindow == null || win == _homeWindow) return; // prevent feedback loop

            // Trigger when user moves or resizes window
            if (args.DidPositionChange || args.DidSizeChange)
            {
                if (_homeWindow != null)
                   await WindowDockManager.SnapHomeWindowAsync(_homeWindow, _mgr.GetOpenNativeWindows());
            }
        };

        win.Closed += (_, _) =>
        {
            appWin.Changed -= handler;
            WindowDockManager.SaveWindowPosition(win);
            UntrackWindow(win);
        };
    }

    private void UntrackWindow(Window win)
    {
        var existing = _windows.FirstOrDefault(x => x.Window == win);
        if (existing != null)
            _homeWindow?.DispatcherQueue.TryEnqueue(() => _windows.Remove(existing));

        // ✅ if a non-home window closed, and toggle enabled, show home
        if (ReturnToHomeOnClose && _homeWindow != null && win != _homeWindow)
        {
            try
            {
                _homeWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    _mgr.BringToFront(_homeWindow);
                    PlatUtils.GetAppWindow(_homeWindow).Show(true);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Coordinator] Auto-return failed: {ex.Message}");
            }
        }
        
    }
    public void BringAllToFront()
    {
        foreach (var w in _mgr.GetOpenNativeWindows())
            _mgr.BringToFront(w);
    }

    public void RestoreAll() => WindowDockManager.RestoreWindowPositions(_mgr);

    public void SaveAll()
    {
        foreach (var w in _mgr.GetOpenNativeWindows())
            WindowDockManager.SaveWindowPosition(w);
    }

    public async Task SnapAllToHome()
    {
        if (_homeWindow == null) return;
       await WindowDockManager.SnapHomeWindowAsync(_homeWindow, _mgr.GetOpenNativeWindows());
    }

    public void ShowControlPanel()
    {
        Guard.IsNotNull(BaseVM, "Base View Model");
        //var panel = new ControlPanelWindow(BaseVM);
        //_mgr.GetOrCreateUniqueWindow(windowFactory:() => panel);
    }

    public void CloseAll()
    {
        SaveAll();
        _mgr.CloseAllWindows();
    }

    public void BringToFront(Window window)
    {
        _mgr.BringToFront(window);
    }

    
}
 public record WindowEntry(Window Window)
    {
        public string Title => Window.Title ?? "Untitled";
        public string Handle => WindowNative.GetWindowHandle(Window).ToString("X");
        public RectInt32 Bounds
        {
            get
            {
                var appWin = PlatUtils.GetAppWindow(Window);
                return new RectInt32(appWin.Position.X, appWin.Position.Y, appWin.Size.Width, appWin.Size.Height);
            }
        }
    };