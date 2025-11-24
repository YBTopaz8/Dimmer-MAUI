using Android.Runtime;

using Application = Android.App.Application;
using Environment = System.Environment;
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
public class MainApplication : Application
{

    public static IServiceProvider ServiceProvider { get; set; }

    

    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        Console.WriteLine("Dimmer Android :D");


        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledExceptionRaiser;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }


    public override void OnCreate()
    {
        base.OnCreate();


        ServiceProvider = Bootstrapper.Init();

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



    private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {

        try
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private static readonly object _logLock = new();

    public static void LogException(Exception ex)
    {
        try
        {
            // Define the directory path.
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DimmerCrashLogs");

            // Ensure the directory exists.
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Use a date-specific file name.
            string fileName = $"Droidcrashlog_{DateTime.Now:yyyy-MM-dd}.txt";
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

    internal static void HandleIntent(Intent? intent)
    {
        Debug.WriteLine($"HandleIntent invoked with Intent: {intent}"); // Add logging!
    }
}
