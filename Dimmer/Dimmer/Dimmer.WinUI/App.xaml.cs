// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


using Dimmer.Utilities;

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
    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        if (!AppSettingsService.IsSticktoTopPreference.GetIsSticktoTopState())
        {
            return;
        }
        //e.Cancel = true;
        //var allWins = Application.Current!.Windows.ToList<Window>();

        //foreach (var win in allWins)
        //{
        //    if (win.Title != "MyWin")
        //    {
        //        bool result = await win!.Page!.DisplayAlert(
        //            "Confirm Action",
        //            "You sure want to close app?",
        //            "Yes",
        //            "Cancel");
        //        if (result)
        //        {

        //            Application.Current.CloseWindow(win);
        //            Application.Current.Quit();
        //            Environment.Exit(0); // Forcefully kill all threads

        //        }
        //    }
        //}
    }

    protected override MauiApp CreateMauiApp()
    {
        var s = IPlatformApplication.Current;
        
        return MauiProgram.CreateMauiApp();
    }


}


