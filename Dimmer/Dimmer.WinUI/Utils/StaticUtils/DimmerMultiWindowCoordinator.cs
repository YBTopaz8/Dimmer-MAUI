using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Window = Microsoft.UI.Xaml.Window;
using CommunityToolkit.Diagnostics;

namespace Dimmer.WinUI.Utils.StaticUtils;

public class DimmerMultiWindowCoordinator
{
    private readonly IWinUIWindowMgrService _mgr;
    private readonly ObservableCollection<WindowEntry> _windows = new();
    private Microsoft.UI.Xaml.Window? _homeWindow;

    public ReadOnlyObservableCollection<WindowEntry> Windows { get; }
    public BaseViewModelWin BaseVM { get; set; }
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
        _mgr.WindowSizeChanged += (_, e) =>
        {
            if (_homeWindow != null)
                WindowDockManager.SnapHomeWindow(_homeWindow, _mgr.GetOpenNativeWindows());
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

        var entry = new WindowEntry(win);
        _windows.Add(entry);

        var appWin = PlatUtils.GetAppWindow(win);

        appWin.Changed += (sender, args) =>
        {
            if (_homeWindow == null || win == _homeWindow) return; // prevent feedback loop

            // Trigger when user moves or resizes window
            if (args.DidPositionChange || args.DidSizeChange)
            {
                if (_homeWindow != null)
                    WindowDockManager.SnapHomeWindow(_homeWindow, _mgr.GetOpenNativeWindows());
            }
        };

        win.Closed += (_, _) =>
        {
            WindowDockManager.SaveWindowPosition(win);
            UntrackWindow(win);
        };
    }

    private void UntrackWindow(Window win)
    {
        var existing = _windows.FirstOrDefault(x => x.Window == win);
        if (existing != null)
            _windows.Remove(existing);
    }

    public void RestoreAll() => WindowDockManager.RestoreWindowPositions(_mgr);

    public void SaveAll()
    {
        foreach (var w in _mgr.GetOpenNativeWindows())
            WindowDockManager.SaveWindowPosition(w);
    }

    public void SnapAllToHome()
    {
        if (_homeWindow == null) return;
        WindowDockManager.SnapHomeWindow(_homeWindow, _mgr.GetOpenNativeWindows());
    }

    public void ShowControlPanel()
    {
        Guard.IsNull(BaseVM, "Base View Model");
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