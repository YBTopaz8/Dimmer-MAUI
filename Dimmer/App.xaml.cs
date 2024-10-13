namespace Dimmer_MAUI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Handle unhandled exceptions
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
#if WINDOWS
        MainPage = new AppShell();
        
#elif ANDROID
        MainPage = new AppShellMobile();
#endif
        
    }


    private async void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        Debug.WriteLine($"********** UNHANDLED EXCEPTION! Details: {e.Exception} | {e.Exception.InnerException?.Message} | {e.Exception.Source} " +
            $"| {e.Exception.StackTrace} | {e.Exception.TargetSite}");

        var home = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        await home.ExitingApp();
        LogException(e.Exception);
    }
    public Window win;
    protected override Window CreateWindow(IActivationState activationState)
    {
        win = base.CreateWindow(activationState);
        win.MinimumHeight = 800;
        win.MinimumWidth = 1200;
        win.Height = 900;
        win.Width = 1200;
#if DEBUG

        win.Title = "Dimmer v0.0.5-debug";
#endif

#if RELEASE
        win.Title = "Dimmer v0.0.5-release";
#endif
        return win;
    }

    private static readonly object _logLock = new object();
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
                        File.AppendAllText(filePath, logContent);
                        success = true; // Write successful
                    }
                    catch (IOException ioEx) when (retries > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to log, retrying... ({ioEx.Message})");
                        System.Threading.Thread.Sleep(delay); // Wait and retry
                    }
                }

                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to log exception after multiple attempts.");
                }
            }
        }
        catch (Exception loggingEx)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log exception: {loggingEx}");
        }
    }

    public override void CloseWindow(Window window)
    {
        base.CloseWindow(window);
    }

    protected override void OnStart()
    {
        base.OnStart();

    }
}
