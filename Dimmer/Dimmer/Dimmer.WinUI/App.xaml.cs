// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace Dimmer.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        Debug.WriteLine("Dimmer WinUI :D");
        this.InitializeComponent();
        AppDomain.CurrentDomain.ProcessExit +=CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.FirstChanceException +=CurrentDomain_FirstChanceException;
    }

    private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        string errorDetails = $"********** UNHANDLED EXCEPTION! **********\n" +
                                 $"Exception Type: {e.Exception.GetType()}\n" +
                                 $"Message: {e.Exception.Message}\n" +
                                 $"Source: {e.Exception.Source}\n" +
                                 $"Stack Trace: {e.Exception.StackTrace}\n";

        if (e.Exception.InnerException != null)
        {
            errorDetails += "***** Inner Exception *****\n" +
                            $"Message: {e.Exception.InnerException.Message}\n" +
                            $"Stack Trace: {e.Exception.InnerException.StackTrace}\n";
        }

        // Print to Debug Console
        Debug.WriteLine(errorDetails);

        // Log to file
        LogException(e.Exception);

        // Print to Shell.Current
        if (Shell.Current != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"Unhandled Exception HERE, {errorDetails}");
            });
        }
    }
    private static readonly object _logLock = new();

    public static void LogException(Exception ex)
    {
        try
        {
            // Define the directory path.
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerCrashLogs");

            // Ensure the directory exists.
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Use a date-specific file name.
            string fileName = $"crashlog_{DateTime.Now:yyyy-MM-dd}.txt";
            string filePath = Path.Combine(directoryPath, fileName);

            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nMsg: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n";

            // Retry mechanism for file writing.
            bool success = false;
            int retries = 3;
            int delay = 500; // Delay between retries in milliseconds

            lock (_logLock)
            {
                while (retries-- > 0 && !success)
                {
                    try
                    {
#if RELEASE || DEBUG
                        File.AppendAllText(filePath, logContent);
                        success = true; // Write successful.
#endif
                    }
                    catch (IOException ioEx) when (retries > 0)
                    {
                        Debug.WriteLine($"Failed to log, retrying... ({ioEx.Message})");
                        Thread.Sleep(delay);
                    }
                }

                if (!success)
                {
                    Debug.WriteLine("Failed to log exception after multiple attempts.");
                }
            }
        }
        catch (Exception loggingEx)
        {
            Debug.WriteLine($"Failed to log exception: {loggingEx}");
        }
    }
    private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    protected override MauiApp CreateMauiApp()
    {
        var s = IPlatformApplication.Current;
        
        return MauiProgram.CreateMauiApp();
    }
    private TrayIconHelper? _trayIconHelper;
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr childWindow, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WndProcDelegate? _newWndProcDelegate;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private const int GWL_WNDPROC = -4;
    private const int WM_COMMAND = 0x0111;
    private const int WM_NOTIFY = 0x004E;
    private IntPtr _thumbnailPreviewWindowHandle = IntPtr.Zero; // Store the handle

    public void InitializeThumbnailHandling()
    {
        // 1. Find the Thumbnail Preview Window Handle
        _thumbnailPreviewWindowHandle = FindThumbnailPreviewWindow();

        if (_thumbnailPreviewWindowHandle != IntPtr.Zero)
        {
            // 2. Hook the Window Procedure
            _newWndProcDelegate = WndProc;
            _oldWndProc = SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, _newWndProcDelegate);

            if (_oldWndProc == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to hook window procedure. Error code: " + Marshal.GetLastWin32Error());
            }
        }
        else
        {
            Debug.WriteLine("Could not find thumbnail preview window.");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NOTIFY)
        {
            NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(lParam, typeof(NMHDR));

            if (nmhdr.code == TTN_COMMAND)
            {
                var commandId = nmhdr.idFrom;
                //int commandId = nmhdr.idCommand;

                if (commandId == 100)
                {
                    Debug.WriteLine("Play button clicked (Thumbnail).");
                    // Your Play button logic here
                }
                else if (commandId == 101)
                {
                    Debug.WriteLine("Pause button clicked (Thumbnail).");
                    // Your Pause button logic here
                }
                else if (commandId == 102)
                {
                    Debug.WriteLine("Resume button clicked (Thumbnail).");
                    // Your Resume button logic here
                }
                else if (commandId == 103)
                {
                    Debug.WriteLine("Previous button clicked (Thumbnail).");
                    // Your Previous button logic here
                }
                else if (commandId == 104)
                {
                    Debug.WriteLine("Next button clicked (Thumbnail).");
                    // Your Next button logic here
                }
            }
        }
        else if (msg == WM_COMMAND)
        {
            // Handle WM_COMMAND if needed (e.g., for other window messages)
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private IntPtr FindThumbnailPreviewWindow()
    {
        string className = "Shell_ThumbPreview"; // Example - likely incorrect
        string windowTitle = "Your Application Title"; // Example - likely incorrect

        IntPtr hwnd = FindWindow(className, windowTitle);

        if (hwnd == IntPtr.Zero)
        {
            // Try finding a child window if the direct search fails
            hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, windowTitle);
        }

        return hwnd;
    }

    public void UnhookThumbnailHandling()
    {
        if (_thumbnailPreviewWindowHandle != IntPtr.Zero)
        {
            SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, _oldWndProc);
            _thumbnailPreviewWindowHandle = IntPtr.Zero;
        }
    }


    // Structures needed for WM_NOTIFY handling
    [StructLayout(LayoutKind.Sequential)]
    public struct NMHDR
    {
        public IntPtr hwndFrom;
        public uint idFrom;
        public uint code;
    }

    public const int TTN_COMMAND = 0x0100;

    
}
public static class AppPlatform
{
    public static Window? DimmerWindow { get; set; } 
    public static Window CreatePlatformWindow(IActivationState? state)
    {
        DimmerWindow = new Window(new AppShell());
        DimmerWindow.Title = "Dimmer";
        return DimmerWindow;
    }

}