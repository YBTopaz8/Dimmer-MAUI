// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using System.Diagnostics; // For Process
using System.Linq;      // For LINQ methods like Select, Where
using Microsoft.Extensions.DependencyInjection; // For GetService
using Microsoft.Maui.Controls.Hosting;
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

        var mainInstance = AppInstance.FindOrRegisterForKey("MainDimmer");
        if (!mainInstance.IsCurrent)
        {
            // This is a secondary instance. Redirect and exit.
            var currentInstance = AppInstance.GetCurrent();
            var args = currentInstance.GetActivatedEventArgs();
            // Asynchronously redirect and then exit.
            // No need to GetAwaiter().GetResult() here, fire and forget is okay for redirection.
            _ = mainInstance.RedirectActivationToAsync(args); // Use discard _ for fire-and-forget

            Process.GetCurrentProcess().Kill();
            return; // Essential to prevent further initialization of this instance
        }
        else
        {
            // This is the main instance. Subscribe to activated events.
            mainInstance.Activated += MainInstance_Activated;
        }


        this.InitializeComponent();
        AppDomain.CurrentDomain.ProcessExit +=CurrentDomain_ProcessExit;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    }
    // This event handler is for the MAIN INSTANCE when it's activated by a redirected instance
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        // Ensure execution on the UI thread if HandleActivated interacts with UI directly
        // or if HomePageVM expects to be called from UI thread.
        // For MAUI, MauiDispatcher is a good way if needed.
        // For now, assuming HandleActivated or subsequent calls handle threading.
        HandleActivated(e);
        // TODO : Optimize this. HandleActivated is called an unknown number of time and is slow with many songs
        // With correct logic, it should be called once per redirection.
    }
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // If you intend to use _thumbnailHandler:
        // _thumbnailHandler.InitializeThumbnailHandling();

        // For the initial launch, get the activation arguments
        // This could be a normal launch or a launch via file association
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        HandleActivated(activatedArgs); // Just call it once.
    }

    // No need for the 'paths' field at the class level if it's only used transiently
    // string[]? paths = Array.Empty<string>();

    private void HandleActivated(AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.File)
        {
            if (args.Data is IFileActivatedEventArgs fileArgs)
            {
                // Extract paths. These could be null if a file object isn't a StorageFile or Path is null
                string?[] rawPaths = fileArgs.Files.Select(file => (file as StorageFile)?.Path).ToArray();
                HandleFiles(rawPaths);
            }
        }
        // else if (args.Kind == ExtendedActivationKind.Launch)
        // {
        //    // Handle normal launch if needed, e.g., open main window without any files
        // }
        // Handle other activation kinds if necessary
    }

    private void HandleFiles(string?[] paths) // paths now comes as string?[]
    {
        if (paths == null || paths.Length == 0)
            return;

        // Filter out null or empty paths and get a List<string>
        var validPaths = paths.Where(p => !string.IsNullOrEmpty(p)).ToList<string>();

        if (!validPaths.Any())
            return;

        // It's generally safer to resolve services when needed,
        // especially if they might have a scoped lifetime or depend on UI thread.
        // Also, ensure HomePageVM is registered as a singleton or transient as appropriate.
        var homePageVM = IPlatformApplication.Current?.Services.GetService<BaseViewModel>(); // Assuming HomePageVM is your ViewModel class
        if (homePageVM != null)
        {
            // Consider if LoadLocalSongFromOutSideApp needs to be thread-safe
            // or dispatched to the UI thread if it updates UI-bound properties directly.
            // e.g., MainThread.BeginInvokeOnMainThread(() => homePageVM.LoadLocalSongFromOutSideApp(validPaths));
            //homePageVM.LoadLocalSongFromOutSideApp(validPaths);
        }
        else
        {
            // Log error: HomePageVM not found
            Debug.WriteLine("Error: HomePageVM could not be resolved.");
        }
    }


    private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        string errorDetails = $"********** UNHANDLED EXCEPTION! **********\n" +
                                 $"Exception Type: {e.Exception.GetType()}\n" +
                                 $"ChatMessage: {e.Exception.Message}\n" +
                                 $"Source: {e.Exception.Source}\n" +
                                 $"Stack Trace: {e.Exception.StackTrace}\n";

        if (e.Exception.InnerException != null)
        {
            errorDetails += "***** Inner Exception *****\n" +
                            $"ChatMessage: {e.Exception.InnerException.Message}\n" +
                            $"Stack Trace: {e.Exception.InnerException.StackTrace}\n";
        }

        // Print to Debug Console
        Debug.WriteLine(errorDetails);

        // Log to file
        LogException(e.Exception);

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


