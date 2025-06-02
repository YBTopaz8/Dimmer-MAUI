using Microsoft.UI.Xaml;

using Application = Microsoft.Maui.Controls.Application;

namespace Dimmer.WinUI.Utils.WinMgt;

public class SettingsWindowManager : ISettingsWindowManager
{
    public SettingsWindow InstanceWindow => _settingWindowInstance;
    private SettingsWindow _settingWindowInstance;
    private DispatcherQueue _dispatcherQueue; // To run UI operations on the correct thread

    public bool IsSettingsWindowOpen => _settingWindowInstance != null;

    // Helper to ensure we have the DispatcherQueue from the main MAUI window
    private DispatcherQueue GetDispatcherQueue()
    {
        if (_dispatcherQueue == null)
        {
            // Get the main MAUI window's underlying WinUI window
            var mauiApplication = Microsoft.Maui.Controls.Application.Current;
            if (mauiApplication != null && mauiApplication.Windows.Count > 0)
            {
                var mainMauiWindow = mauiApplication.Windows[0]; // Assuming first window is main
                if (mainMauiWindow.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    _dispatcherQueue = nativeWindow.DispatcherQueue;
                }
            }

        }
        return _dispatcherQueue;
    }


    public void ShowSettingsWindow(BaseViewModelWin viewModel)
    {
        GetDispatcherQueue().TryEnqueue(() =>
        {
            if (_settingWindowInstance == null)
            {
                _settingWindowInstance = new SettingsWindow(viewModel); // Pass your MyViewModel
                _settingWindowInstance.Destroying +=_settingWindowInstance_Destroying;
                ;

                // This requires getting the HWND of the main MAUI window.
                var mauiApplication = Microsoft.Maui.Controls.Application.Current;
                if (mauiApplication != null && mauiApplication.Windows.Count > 0)
                {
                    var mainMauiWindow = mauiApplication.Windows[0];
                    if (mainMauiWindow.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeMainWindow)
                    {

                        IntPtr nativeWindowHandle = WindowNative.GetWindowHandle(nativeMainWindow);
                        WindowId win32WindowsId = Win32Interop.GetWindowIdFromWindow(nativeWindowHandle);
                        AppWindow winuiAppWindow = AppWindow.GetFromWindowId(win32WindowsId);
                        //winuiAppWindow.?
                    }
                }
                Application.Current.OpenWindow(_settingWindowInstance);
            }
            else
            {
                Application.Current.ActivateWindow(_settingWindowInstance);
            }
        });
    }

    private void _settingWindowInstance_Destroying(object? sender, EventArgs e)
    {
        GetDispatcherQueue().TryEnqueue(() =>
        {
            if (_settingWindowInstance != null)
            {
                _settingWindowInstance = null;
                System.Diagnostics.Debug.WriteLine("SettingsWindow closed and reference cleared.");
            }
        });
    }

    private void OnSettingWindowClosed(object sender, WindowEventArgs e)
    {
        // Ensure cleanup happens on the UI thread if it involves UI elements
        // though usually, just nulling out the reference is fine here.
       
    }

    public void BringSettingsWindowToFront()
    {
        if (_settingWindowInstance != null)
        {
            GetDispatcherQueue().TryEnqueue(() =>
            {
                Application.Current.ActivateWindow( _settingWindowInstance);
            });
        }
    }

    public void CloseSettingsWindow()
    {
        if (_settingWindowInstance != null)
        {
            GetDispatcherQueue().TryEnqueue(() =>
            {
                Application.Current.CloseWindow(_settingWindowInstance);
                // The Closed event handler will set _settingWindowInstance to null
            });
        }
    }
}