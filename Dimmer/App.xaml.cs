
using System.Net;

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
        
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        DimmerWindow = dimmerWindow;
#if DEBUG
        APIKeys.SetupLastFM();
#endif

    }

    private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        Debug.WriteLine($"********** UNHANDLED EXCEPTION! Details: {e.Exception} | {e.Exception.InnerException?.Message} | {e.Exception.Source} " +
            $"| {e.Exception.StackTrace} | {e.Exception.Message} || {e.Exception.Data.Values} {e.Exception.HelpLink}");

        //var home = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        LogException(e.Exception);
    }
    
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

        return DimmerWindow;
    }

    private static readonly object _logLock = new object();

    public DimmerWindow DimmerWindow { get; }

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
