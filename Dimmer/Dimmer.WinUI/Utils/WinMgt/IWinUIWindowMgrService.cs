﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Window = Microsoft.UI.Xaml.Window;

namespace Dimmer.WinUI.Utils.WinMgt;

public interface IWinUIWindowMgrService
{
    event EventHandler<WinUIWindowMgrService.WindowClosingEventArgs>? WindowClosing;
    event EventHandler<Microsoft.UI.Xaml.WindowActivatedEventArgs>? WindowActivated;
    event EventHandler<Window>? WindowClosed;
    event EventHandler<Microsoft.UI.Xaml.WindowSizeChangedEventArgs>? WindowSizeChanged;

    void BringToFront(Window window);
    void CloseAllWindows();
    void CloseWindow(Window window);
    void CloseWindow<T>() where T : Window;
    Window? CreateContentWindow(Type pageType, object? navigationParameter = null, string? title = null);
    T? CreateWindow<T>() where T : Window, new();
    T? CreateWindow<T>(object? parameter) where T : Window;
    IReadOnlyList<Window> GetOpenNativeWindows();
    Window? GetOrCreateUniqueContentWindow(Type pageType, string uniqueId, object? navigationParameter = null, string? title = null, Func<Window>? windowFactory = null);
    T? GetOrCreateUniqueWindow<T>(BaseViewModelWin? callerVM=null, Func<T>? windowFactory = null) where T : Window;
    T? GetWindow<T>() where T : Window;
    void TrackWindow(Window window);
    void UntrackWindow(Window window);
}