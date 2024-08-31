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
        System.Diagnostics.Debug.WriteLine($"********** UNHANDLED EXCEPTION! Details: {e.Exception}");
#endif
        LogException(e.Exception);
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        window.MinimumHeight = 800;
        window.MinimumWidth = 1200;
        window.Height = 900;
        window.Width = 1200;
        
        window.Title = "Dimmer";
        
        return window;
        
    }

    private void LogException(Exception ex)
    {
        try
        {
            // Log to a file
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\DimmerDD", "crashlog.txt");
            string logContent = $"[{DateTime.Now}]\n{ex}\n\n";
            if (File.Exists(filePath))
            {
                File.AppendAllText(filePath, logContent);
            }            
        }
        catch
        {
            // If logging fails, there's not much we can do
        }
    }

    
}
