using Microsoft.UI.Xaml;

namespace Dimmer.WinUI.Utils.WinMgt;

public class SettingsWindowManager : ISettingsWindowManager
{
    public SettingWindow InstanceWindow => _settingWindowInstance;
    private SettingWindow _settingWindowInstance;
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


    public void ShowSettingsWindow(BaseViewModel viewModel)
    {
        GetDispatcherQueue().TryEnqueue(() =>
        {
            if (_settingWindowInstance == null)
            {
                _settingWindowInstance = new SettingWindow(viewModel); // Pass your MyViewModel
                _settingWindowInstance.Closed += OnSettingWindowClosed;

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
                _settingWindowInstance.Activate();
            }
            else
            {
                _settingWindowInstance.Activate(); // Bring to front if already open
            }
        });
    }

    private void OnSettingWindowClosed(object sender, WindowEventArgs e)
    {
        // Ensure cleanup happens on the UI thread if it involves UI elements
        // though usually, just nulling out the reference is fine here.
        GetDispatcherQueue().TryEnqueue(() =>
        {
            if (_settingWindowInstance != null)
            {
                _settingWindowInstance.Closed -= OnSettingWindowClosed;
                _settingWindowInstance = null;
                System.Diagnostics.Debug.WriteLine("SettingsWindow closed and reference cleared.");
            }
        });
    }

    public void BringSettingsWindowToFront()
    {
        if (_settingWindowInstance != null)
        {
            GetDispatcherQueue().TryEnqueue(() =>
            {
                _settingWindowInstance?.Activate();
            });
        }
    }

    public void CloseSettingsWindow()
    {
        if (_settingWindowInstance != null)
        {
            GetDispatcherQueue().TryEnqueue(() =>
            {
                _settingWindowInstance?.Close();
                // The Closed event handler will set _settingWindowInstance to null
            });
        }
    }
}