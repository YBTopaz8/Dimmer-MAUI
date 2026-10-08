using Avalonia.Controls;
using DevWinUI;
using Dimmer.Utils;
using Dimmer.WinUI.DimmerAudioWin;
using Microsoft.Windows.AppLifecycle;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
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
        try
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

            Microsoft.UI.Xaml.Application.Current.UnhandledException += App_UnhandledException;


        }
        catch (Exception ex)
        {
            System.Diagnostics.Debugger.Break();
            throw;
        }
    }
    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true; // Try to prevent the app from closing immediately
        System.Diagnostics.Debug.WriteLine($"[XAML CRASH] {e.Message}");
        System.Diagnostics.Debug.WriteLine($"[XAML CRASH STACK] {e.Exception.StackTrace}");
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
        if (ex.Message.Contains("Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host..\r\n"))            
        {
            // This is the noisy exception we want to ignore.
            // Just return and don't log it.
            return;
        }

        if (ex.Message.Contains("Unable to read data from the transport connection: The I/O operation has been aborted because of either a thread exit or an application request.."))
        {
            return; // Ignore this specific exception
        }
        if (ex.Message.Contains("No such host is known."))            
        {
            // This is the noisy exception we want to ignore.
            // Just return and don't log it.
            return;
        }
        if (ex.Message.Contains("Exception has been thrown by the target of an invocation"))            
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

            // Use a date-specific file name

            string fileName;
#if DEBUG
            fileName = $"WinUIcrashlogDebug_{DateTime.Now:yyyy-MM-dd}.txt";
#elif RELEASE
            fileName = $"WinUIcrashlogRelease_{DateTime.Now:yyyy-MM-dd}.txt";
#endif
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

            // Remove m_window.DispatcherQueue entirely here. 
            // HandleActivation uses a thread-safe ConcurrentQueue and is safe to call from the RPC thread.
            HandleActivation(e);
        }
        catch (Exception ex)
        {
            RxSchedulers.UI.Schedule(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlertAsync("Error", $"An error occurred during activation: {ex.Message}", "OK");
            });
        }
    }

    public static SynchronizationContext MainSyncContext { get; private set; }
    // A thread-safe collection to gather file paths from multiple, rapid activations.
    private readonly ConcurrentQueue<string> _activatedFilePaths = new();

    // A debouncer to process files in a single batch after a short delay.
    private readonly Debouncer _fileProcessingDebouncer = new(delayMilliseconds: 300);
    
    
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        MainSyncContext = SynchronizationContext.Current!;

        this.DebugSettings.LayoutCycleTracingLevel = LayoutCycleTracingLevel.High;
        this.DebugSettings.LayoutCycleDebugBreakLevel = LayoutCycleDebugBreakLevel.High;


        Utils.StaticUtils.UiThreads.EnsureInitialized();
        //ContextMenuItem menu = new ContextMenuItem
        //{
        //    Title = "Open Dimmer Here",
        //    Param = @"""{path}""",
        //    AcceptFileFlag = (int)FileMatchFlagEnum.All,
        //    AcceptDirectoryFlag = (int)(DirectoryMatchFlagEnum.Directory | DirectoryMatchFlagEnum.Background | DirectoryMatchFlagEnum.Desktop),
        //    AcceptMultipleFilesFlag = (int)FilesMatchFlagEnum.Each,
        //    Index = 0,
        //    Enabled = true,
        //    Icon = ProcessInfoHelper.GetFileVersionInfo().FileName,
        //    Exe = "Dimmer.WinUI.exe"
        //};
        //var menuFolder = await ContextMenuService.CreateDefualtMenusFolderAsync();
        //ContextMenuService menuService = new ContextMenuService(menuFolder);
        //await menuService.SaveAsync(menu);
    }
 


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

        // Safety check: MAUI might not have created the window yet
        if (m_window == null)
        {
            Task.Delay(300).ContinueWith(_ => ProcessFileBatch());
            return;
        }

        var pathsToProcess = new List<string>();
        while (_activatedFilePaths.TryDequeue(out var path))
        {
            pathsToProcess.Add(path);
        }

        if (pathsToProcess.Count == 0) return;

        m_window.DispatcherQueue.TryEnqueue(() =>
        {
    
    var mainVM = IPlatformApplication.Current?.Services.GetService<BaseViewModel>();
            if (mainVM == null) return;

            // NEW LOGIC: Check if this was a direct media file activation
            var firstFile = pathsToProcess.First();
            var isSingleAudioFile = pathsToProcess.Count == 1 &&
                                    (firstFile.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                     firstFile.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
                                     firstFile.EndsWith(".opus", StringComparison.OrdinalIgnoreCase) ||
                                     firstFile.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                                     firstFile.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

            if (isSingleAudioFile)
            {
                // The user double-clicked a specific song from File Explorer!
                // 1. Pass to service to read metadata
                //var singleSongModel = await mainVM.ParseSingleFileToSongModel(firstFile);

                //// 2. Play immediately
                //if (singleSongModel != null)
                //{
                //    // Add to queue if you want, but force play immediately
                //    await mainVM.PlaySongAsync(singleSongModel);
                //}
            }
            else
            {
                // The user dragged a folder, or selected 50 songs and pressed "Open"
                // Use your existing bulk library logic
                mainVM.AddMusicFoldersByPassingToService(pathsToProcess);
            }

            // Bring the window to the front!
            var hwnd = PlatUtils.GetHWIdnInt(m_window);
            PlatUtils.ShowWindow(hwnd, PlatUtils.SW_RESTORE);
        });
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

        if (e.Exception.Message.Contains("Unable to read data from the transport connection: The I/O operation has been aborted because of either a thread exit or an application request.."))
        {
           return; // Ignore this specific exception
        }

        // Print to Debug Console
        Debug.WriteLine(errorDetails);

        // Log to file
        LogException(e.Exception);

    }
    private static readonly object _logLock = new();

    private static readonly ExceptionFilterPolicy _filterPolicy = new ExceptionFilterPolicy();
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
        //        bool result = await win!.Page!.DisplayAlertAsync(
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