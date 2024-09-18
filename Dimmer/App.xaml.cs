namespace Dimmer_MAUI;

public partial class App : Application
{
    public App(INativeAudioService audioService)
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

    private void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"********** UNHANDLED EXCEPTION! Details: {e.Exception} | {e.Exception.InnerException?.Message} | {e.Exception.Source} " +
            $"| {e.Exception.StackTrace} | {e.Exception.TargetSite}");
#endif
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
        
        win.Title = "Dimmer v0.0.4";
        
        return win;
        
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

            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nMsg:{ex.Message}\n StackTrack:{ex.StackTrace}\n";

            File.AppendAllText(filePath, logContent);
        }
        catch (Exception loggingEx)
        {
            // Optionally, handle exceptions that occur during logging
            // For example, you might want to notify the user or log to an alternative location
            // However, avoid throwing exceptions from a logging method to prevent potential infinite loops
            
            Debug.WriteLine($"Failed to log exception: {loggingEx}");
        }
    }

    public override void CloseWindow(Window window)
    {
        base.CloseWindow(window);
    }


}
