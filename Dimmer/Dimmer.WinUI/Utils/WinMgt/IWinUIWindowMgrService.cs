using Dimmer.WinUI.Views;

using Microsoft.UI.Xaml;

using Window = Microsoft.UI.Xaml.Window;

namespace Dimmer.WinUI.Utils.WinMgt;

public interface IWinUIWindowMgrService
{
    event EventHandler<WinUIWindowMgrService.WindowClosingEventArgs>? WindowClosing; public event EventHandler<WindowActivatedWithSourceEventArgs>? WindowActivated;

    event EventHandler<Window>? WindowClosed;
    event EventHandler<Microsoft.UI.Xaml.WindowSizeChangedEventArgs>? WindowSizeChanged;

    void ActivateWindow(Window window);
    void BringToFront(Window window);
    void CloseAllWindows();
    void CloseWindow(Window window);
    void CloseWindow<T>() where T : Window;
    Window? CreateContentWindow(Type pageType, object? navigationParameter = null, string? title = null);
    T? CreateWindow<T>() where T : Window, new();
    T? CreateWindow<T>(object? parameter) where T : Window;
    IReadOnlyList<Window> GetOpenNativeWindows();
    Window? GetOrCreateUniqueContentWindow(Type pageType, string uniqueId, object? navigationParameter = null, string? title = null, Func<Window>? windowFactory = null);
    T? GetOrCreateUniqueWindow<T>(BaseViewModelWin? callerVM = null, Func<T>? windowFactory = null) where T : Window;
    T? GetWindow<T>() where T : Window;
    void TrackWindow(Window window);
    void UntrackWindow(Window window);

}

public class WindowActivatedWithSourceEventArgs : EventArgs

{
    public Window Window { get; }
    public WindowActivationState ActivationState { get; }

    public WindowActivatedWithSourceEventArgs(Window window, WindowActivationState state)
    {
        Window = window;
        ActivationState = state;
    }
}