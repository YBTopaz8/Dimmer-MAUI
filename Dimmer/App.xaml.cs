using System.Runtime.InteropServices;
#if WINDOWS
using static Dimmer_MAUI.Platforms.Windows.SHSTOCKICONINFO;
#endif
namespace Dimmer_MAUI;

public partial class App : Application
{
    public App(DimmerWindow dimmerWindow)
    {
        
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        AppDomain.CurrentDomain.ProcessExit += (s, e) => GeneralStaticUtilities.ClearUp();
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        DimmerWindow = dimmerWindow;

        //APIKeys.SetupClientInitializations();

        

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

    private void LogException(Exception ex)
    {

        try
        {
            // Define the directory path
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerCrashLogs");

            // Ensure the directory exists; if not, create it
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string filePath = Path.Combine(directoryPath, "crashlog.txt");
            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nMsg:{ex.Message}\nStackTrace:{ex.StackTrace}\n\n";

            // Retry mechanism for file writing
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
                        success = true; // Write successful
#endif
                    }
                    catch (IOException ioEx) when (retries > 0)
                    {
                        Debug.WriteLine($"Failed to log, retrying... ({ioEx.Message})");
                        Thread.Sleep(delay); // Wait and retry
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

    //private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    //{

    //    Debug.WriteLine($"********** UNHANDLED EXCEPTION! Details: {e.Exception} | {e.Exception.InnerException?.Message} | {e.Exception.Source} " +
    //        $"| {e.Exception.StackTrace} | {e.Exception.Message} || {e.Exception.Data.Values} {e.Exception.HelpLink}");

    //    //var home = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
    //    LogException(e.Exception);
    //}


    protected override Window CreateWindow(IActivationState? activationState)
    {

        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        //DimmerWindow.Page.
#if WINDOWS
        DimmerWindow.Page = new AppShell(vm);

#elif ANDROID
        
        DimmerWindow.Page = new AppShellMobile();
#endif

        //win = base.CreateWindow(activationState);
        //this.MinimumHeight = 800;
        //this.MinimumWidth = 1200;
        //this.Height = 900;
        //this.Width = 1200;

#if WINDOWS

        // Set the thumbnail icon to "music.png".

        InitializeThumbnailButtons();
#endif


        return DimmerWindow;
    }


#if WINDOWS
    // Call this method after your window is created.
    private void InitializeThumbnailButtons()
    {
        IntPtr hwnd = PlatSpecificUtils.DimmerHandle;

            // Hook the native window procedure to handle WM_COMMAND.
            _newWndProcDelegate = new WndProcDelegate(WndProc);
            _oldWndProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, _newWndProcDelegate);

            // Add thumbnail toolbar buttons.
            TaskbarThumbnailHelper.AddThumbnailButtons(hwnd);
     
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_COMMAND)
        {
            int commandId = wParam.ToInt32() & 0xffff;
            switch (commandId)
            {
                case 100:
                    System.Diagnostics.Debug.WriteLine("Play clicked.");
                    break;
                case 101:
                    System.Diagnostics.Debug.WriteLine("Pause clicked.");
                    break;
                case 102:
                    System.Diagnostics.Debug.WriteLine("Resume clicked.");
                    break;
                case 103:
                    System.Diagnostics.Debug.WriteLine("Previous clicked.");
                    break;
                case 104:
                    System.Diagnostics.Debug.WriteLine("Next clicked.");
                    break;
            }
            return IntPtr.Zero;
        }
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }
#endif


#if WINDOWS
    private const int WM_COMMAND = 0x0111;
    private const int GWL_WNDPROC = -4;
    private WndProcDelegate _newWndProcDelegate;
    private IntPtr _oldWndProc = IntPtr.Zero;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
#endif



    private static readonly Lock _logLock = new();

    public DimmerWindow DimmerWindow { get; }

    public override void CloseWindow(Window window)
    {
        base.CloseWindow(window);
    }


}

public enum AppState
{
    OnForeGround,
    OnBackGround
}
