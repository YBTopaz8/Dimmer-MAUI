using System.Collections.Concurrent;

using Dimmer.Utils;

using Microsoft.Windows.AppLifecycle;

using Windows.ApplicationModel.Activation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    private Microsoft.UI.Xaml.Window m_window;

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
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;


        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (ex.Message.Contains("Operation is not valid due to the current state of the object.")
            && ex.StackTrace?.Contains("WinRT.ExceptionHelpers") == true)
        {
            // This is the noisy exception we want to ignore.
            // Just return and don't log it.
            return;
        }
        var errorHandler = Services.GetService<IErrorHandler>();
        errorHandler?.HandleError((Exception)e.ExceptionObject);
        Exception exx = (Exception)e.ExceptionObject;

        string errorDetails = $"********** UNHANDLED EXCEPTION! Winui **********\n" +
                                 $"Exception Type: {exx.GetType()}\n" +
                                 $"Message: {exx.Message}\n" +
                                 $"Source: {exx.Source}\n" +
                                 $"Stack Trace: {exx.StackTrace}\n";

        // ... Log to file, etc.
        Debug.WriteLine(errorDetails);
        LogException(exx);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"UNOBSERVED TASK EXCEPTION: {e.Exception}");
        var errorHandler = Services.GetService<IErrorHandler>();
        errorHandler?.HandleError(e.Exception);
        e.SetObserved(); // Prevent app from crashing due to unobserved exception
    }

    // This event handler is for the MAIN INSTANCE when it's activated by a redirected instance
    private void MainInstance_Activated(object? sender, AppActivationArguments e)
    {
        try
        {
            if (e.Kind == ExtendedActivationKind.ToastNotification)
            {
                Debug.WriteLine("OK");
                return;
            }

            //await PlatUtils.EnsureWindowReadyAsync();
            //m_window = PlatUtils.GetNativeWindow();


            // This is guaranteed to run on the main instance.
            // We need to bring the activation to the UI thread to be safe.
            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                HandleActivation(e);
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Error", $"An error occurred during activation: {ex.Message}", "OK");
            });
        }
    }


    // A thread-safe collection to gather file paths from multiple, rapid activations.
    private readonly ConcurrentQueue<string> _activatedFilePaths = new();

    // A debouncer to process files in a single batch after a short delay.
    private readonly Debouncer _fileProcessingDebouncer = new(delayMilliseconds: 300);
    //protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    //{
    //    try
    //    {

    //        base.OnLaunched(args);

    //        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
    //        HandleActivation(activatedArgs);

    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine(ex.Message);
    //    }
    //}



    /// <summary>
    /// A unified handler for all app activations.
    /// It extracts file paths and queues them for batch processing.
    /// </summary>
    private void HandleActivation(AppActivationArguments args)
    {
        if (args.Kind == ExtendedActivationKind.File && args.Data is IFileActivatedEventArgs fileArgs)
        {
            // Extract valid paths from the activation arguments
            var validPaths = fileArgs.Files
                .Select(file => (file as StorageFile)?.Path)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToList(); // ToList to realize the query

            if (validPaths.Count != 0)
            {
                // Add the new paths to our central queue
                foreach (var path in validPaths)
                {
                    _activatedFilePaths.Enqueue(path!);
                }

                // Trigger the debouncer. It will wait 200ms for more files.
                // If another activation comes in within 200ms, it will reset the timer.
                // This ensures we only process the final batch of files once.
                _fileProcessingDebouncer.Debounce(ProcessFileBatch);
            }
        }
    }
    private void ProcessFileBatch()
    {
        m_window = PlatUtils.GetNativeWindowFromMAUIWindow();
        // Drain the queue to get all file paths collected so far
        var pathsToProcess = new List<string>();
        while (_activatedFilePaths.TryDequeue(out var path))
        {
            pathsToProcess.Add(path);
        }

        if (pathsToProcess.Count == 0)
        {
            return; // Nothing to do
        }

        // IMPORTANT: Ensure this runs on the UI thread, as it will likely
        // interact with a ViewModel that updates the UI.
        m_window.DispatcherQueue.TryEnqueue(() =>
        {
            // Resolve the specific ViewModel you need
            var homePageVM = IPlatformApplication.Current?.Services.GetService<BaseViewModel>();
            if (homePageVM != null)
            {
                // *** THE CORE OPTIMIZATION ***
                // Call a single method on your ViewModel to handle the entire batch.
                // This is vastly more performant than a loop.
                homePageVM.AddMusicFoldersByPassingToService(pathsToProcess);
            }
            else
            {
                Debug.WriteLine("Error: HomePageViewModel could not be resolved. Cannot process files.");
            }
        });
    }



    private static async void HandleFiles(string[] paths) // paths now comes as string?[]
    {
        if (paths == null || paths.Length == 0)
            return;

        // Filter out null or empty paths and get a List<string>
        var validPaths = paths.Where(p => !string.IsNullOrEmpty(p)).ToList<string>();

        if (validPaths.Count == 0)
            return;

        // It's generally safer to resolve services when needed,
        // especially if they might have a scoped lifetime or depend on UI thread.
        // Also, ensure HomePageVM is registered as a singleton or transient as appropriate.
        var homePageVM = IPlatformApplication.Current?.Services.GetService<BaseViewModel>(); // Assuming HomePageVM is your MyViewModel class
        if (homePageVM != null)
        {
            
                 homePageVM.AddMusicFoldersByPassingToService(validPaths);
            

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

    private static readonly ExceptionFilterPolicy _filterPolicy = new ExceptionFilterPolicy();
    public static void LogException(Exception ex)
    {
        if (!_filterPolicy.ShouldLog(ex))
        {
            return; // Ignore this exception based on our policy.
        }
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
            string fileName = $"WinUIcrashlog_{DateTime.Now:yyyy-MM-dd}.txt";
            string filePath = Path.Combine(directoryPath, fileName);

            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nMsg: {ex.Message}\nStackTraceWinUI: {ex.StackTrace}\n\n";

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


public class Debouncer
{
    private CancellationTokenSource? _cts;
    private readonly int _delayMilliseconds;

    public Debouncer(int delayMilliseconds = 250)
    {
        _delayMilliseconds = delayMilliseconds;
    }

    public void Debounce(Action action)
    {
        // Cancel any previously scheduled action
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        // Schedule the new action after the delay
        Task.Delay(_delayMilliseconds, _cts.Token)
            .ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    action();
                }
            }, TaskScheduler.Default); // Use default scheduler for the continuation
    }
}