using Android.Runtime;

using Application = Android.App.Application;
using Environment = System.Environment;
using Path = System.IO.Path;
namespace Dimmer;

#if DEBUG
// Enable debuggable attribute in debug configuration.
// Remove this if you don't want the app to be debuggable.
// For release builds, ensure Debuggable(false) is set or this line is removed.
 //[Application(Debuggable = true)]
#else
// Disable debuggable attribute in release configuration.
// [Application(Debuggable = false)]
#endif
[Application(Debuggable = true)]
public class MainApplication : Application, Application.IActivityLifecycleCallbacks
{

    public static IServiceProvider ServiceProvider { get; set; }


    public static Activity? CurrentActivity { get; private set; }

    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        Console.WriteLine("Dimmer Android :D");
        

        RegisterActivityLifecycleCallbacks(this);
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledExceptionRaiser;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }


    public override void OnCreate()
    {
        base.OnCreate();


        ServiceProvider = Bootstrapper.Init();

        AndroidContentScanner.Initialize();

    }


    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"UNOBSERVED TASK EXCEPTION (Android): {e.Exception}");

        var errorHandler = ServiceProvider?.GetService<IErrorHandler>();
        errorHandler?.HandleError(e.Exception);
        e.SetObserved();
    }

    private static void OnAndroidUnhandledExceptionRaiser(object? sender, RaiseThrowableEventArgs e)
    {
        Debug.WriteLine($"ANDROID UNHANDLED EXCEPTION: {e.Exception}");
        var errorHandler = ServiceProvider?.GetService<IErrorHandler>();
        errorHandler?.HandleError(e.Exception);
        e.Handled = true; // Prevent the application from crashing
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (ServiceProvider == null)
        {
            Console.WriteLine($"Cannot log to IErrorHandler: ServiceProvider is null." +
                $"{e.IsTerminating} | {e.ExceptionObject?.GetType()}" +
                $"|" );
            return;
        }
        Debug.WriteLine($"GLOBAL UNHANDLED EXCEPTION (Android): {e.ExceptionObject}");
        var errorHandler = ServiceProvider?.GetService<IErrorHandler>();
        errorHandler?.HandleError((Exception)e.ExceptionObject);
        // On Android, unhandled exceptions in the main thread might still cause a crash.
        // The AndroidEnvironment.UnhandledExceptionRaiser is generally more effective for preventing crashes.
        // However, we still log here for completeness.
    }


    private static readonly ExceptionFilterPolicy _filterPolicy = new ExceptionFilterPolicy();

    private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {

        try
        {
            // Apply exception filtering to reduce noise from expected exceptions
            if (!_filterPolicy.ShouldLog(e.Exception))
            {
                return; // Skip logging for filtered exceptions
            }

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
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private static readonly object _logLock = new();
    private const string LogDirectoryName = "DimmerCrashLogs";
    private const string LogFileName = "Droidcrashlog_{0:yyyy-MM-dd}.txt";

    /// <summary>
    /// Gets the log file path, creating the directory if necessary.
    /// </summary>
    private static string GetLogFilePath()
    {
        string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LogDirectoryName);
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        
        string fileName = string.Format(LogFileName, DateTime.Now);
        return Path.Combine(directoryPath, fileName);
    }

    public static void LogException(Exception ex)
    {
        // Apply exception filtering to avoid logging noisy exceptions
        if (!_filterPolicy.ShouldLog(ex))
        {
            return; // Ignore this exception based on our policy
        }

        try
        {
            string filePath = GetLogFilePath();
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

    internal static void HandleIntent(Intent? intent)
    {
        Debug.WriteLine($"HandleIntent invoked with Intent: {intent}"); // Add logging!
    }

    /// <summary>
    /// Logs memory-related events to the crash log file.
    /// </summary>
    private static void LogMemoryEvent(string message)
    {
        try
        {
            string filePath = GetLogFilePath();
            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n\n";
            
            File.AppendAllText(filePath, logContent);
        }
        catch (Exception logEx)
        {
            Debug.WriteLine($"Failed to log memory event: {logEx.Message}");
        }
    }

    /// <summary>
    /// Performs aggressive garbage collection to free up memory.
    /// </summary>
    private static void PerformAggressiveGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Called when the system is running low on memory, and actively running processes should trim their memory usage.
    /// Note: The [GeneratedEnum] attribute is not needed in user code when overriding Android framework methods.
    /// </summary>
    public override void OnTrimMemory(TrimMemory level)
    {
        base.OnTrimMemory(level);
        
        Debug.WriteLine($"[MEMORY PRESSURE] OnTrimMemory called with level: {level}");
        LogMemoryEvent($"MEMORY PRESSURE: {level}");
        PerformAggressiveGarbageCollection();
    }

    /// <summary>
    /// Called when the overall system is running low on memory.
    /// Legacy callback for older Android versions.
    /// </summary>
    public override void OnLowMemory()
    {
        base.OnLowMemory();
        
        Debug.WriteLine("[CRITICAL MEMORY] OnLowMemory called - system is critically low on memory");
        LogMemoryEvent("CRITICAL MEMORY PRESSURE - OnLowMemory called");
        PerformAggressiveGarbageCollection();
    }

    public void OnActivityCreated(Activity activity, Bundle? savedInstanceState)
    {

    }

    public void OnActivityDestroyed(Activity activity)
    {

    }

    public void OnActivityPaused(Activity activity)
    {
        if (CurrentActivity == activity) CurrentActivity = null;
    }

    public void OnActivityResumed(Activity activity)
    {
        CurrentActivity = activity;
    }

    public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
    {

    }

    public void OnActivityStarted(Activity activity)
    {

    }

    public void OnActivityStopped(Activity activity)
    {

    }
}
