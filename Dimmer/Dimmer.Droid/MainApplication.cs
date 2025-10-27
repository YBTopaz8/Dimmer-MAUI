using System.Reactive;

using Android.App;
using Android.Runtime;

using AndroidX.DrawerLayout.Widget;

using ReactiveUI;

using Debug = System.Diagnostics.Debug;

namespace Dimmer;
#if DEBUG
// Enable debuggable attribute in debug configuration.
// Remove this if you don't want the app to be debuggable.
// For release builds, ensure Debuggable(false) is set or this line is removed.
// [Application(Debuggable = true)]
#else
// Disable debuggable attribute in release configuration.
// [Application(Debuggable = false)]
#endif
[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(nint handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        Console.WriteLine("Dimmer Android :D");

        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledExceptionRaiser;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }
    //    RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
    //    {
    //        Debug.WriteLine($"REACTIVEUI DEFAULT EXCEPTION HANDLER (Android): {ex}");
    //        var errorHandler = IPlatformApplication.Current!.Services.GetService<IErrorHandler>();
    //        errorHandler?.HandleError(ex);
    //    });
    //}

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Debug.WriteLine($"UNOBSERVED TASK EXCEPTION (Android): {e.Exception}");
        var errorHandler = IPlatformApplication.Current!.Services.GetService<IErrorHandler>();
        errorHandler?.HandleError(e.Exception);
        e.SetObserved();
    }

    private static void OnAndroidUnhandledExceptionRaiser(object? sender, RaiseThrowableEventArgs e)
    {
        Debug.WriteLine($"ANDROID UNHANDLED EXCEPTION: {e.Exception}");
        var errorHandler = IPlatformApplication.Current.Services.GetService<IErrorHandler>();
        errorHandler?.HandleError(e.Exception);
        e.Handled = true; // Prevent the application from crashing
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"GLOBAL UNHANDLED EXCEPTION (Android): {e.ExceptionObject}");
        var errorHandler = IPlatformApplication.Current.Services.GetService<IErrorHandler>();
        errorHandler?.HandleError((Exception)e.ExceptionObject);
        // On Android, unhandled exceptions in the main thread might still cause a crash.
        // The AndroidEnvironment.UnhandledExceptionRaiser is generally more effective for preventing crashes.
        // However, we still log here for completeness.
    }

    public static void HandleAppAction(AppAction appAction)
    {
        Debug.WriteLine($"HandleAppAction invoked with ID: {appAction.Id}"); // Add logging!
                                                                             // Ensure you dispatch to the main thread for UI work
        
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
    sealed partial class MyDrawerListener : DrawerLayout.SimpleDrawerListener
    {
        private readonly MyShellRenderer _renderer;
        private readonly Android.Views.View _contentViewToAnimate; // The main content view

        public MyDrawerListener(MyShellRenderer renderer, Android.Views.View contentView)
        {
            _renderer = renderer;
            _contentViewToAnimate = contentView;
        }

        public override void OnDrawerSlide(Android.Views.View drawerView, float slideOffset)
        {
            base.OnDrawerSlide(drawerView, slideOffset);

            // `drawerView` is the flyout menu view itself.
            // `_contentViewToAnimate` is the page content area.

            if (_contentViewToAnimate != null)
            {
                // 1. Parallax effect for content
                // float contentTranslationX = drawerView.Width * slideOffset * 0.3f; // Adjust 0.3f for intensity
                //_contentViewToAnimate.TranslationX = contentTranslationX;

                // 2. Scale down content (subtle)
                float scale = 1.0f - (slideOffset * 0.1f); // Scale down by 10% when fully open
                _contentViewToAnimate.ScaleX = scale;
                _contentViewToAnimate.ScaleY = scale;

                // 3. Corner Radius for content (requires a CardView or custom background drawable)
                // If _contentViewToAnimate is a CardView or has a GradientDrawable background,
                // you can animate its corner radius.
                // This is more complex as you'd need to ensure _contentViewToAnimate has this capability.
                // For example, if _contentViewToAnimate is a FrameLayout, you could wrap its first child in a CardView.

                // 4. Fade out content slightly
                _contentViewToAnimate.Alpha = 1.0f - (slideOffset * 0.2f); // Fade out by 20%

                // 5. Rotate drawer icon (hamburger to arrow) - This is usually handled by DrawerLayout itself
                // if the Toolbar is correctly set up with it. But you could do custom things here.
            }

            // Animate flyout items (staggered reveal) - best done in MyShellFlyoutRenderer with RecyclerView
            // but you *could* try to access children of `drawerView` here (more fragile)
        }

        public override void OnDrawerOpened(Android.Views.View drawerView)
        {
            base.OnDrawerOpened(drawerView);
            // E.g., Announce for accessibility
            drawerView.AnnounceForAccessibility("Navigation menu opened");
        }

        public override void OnDrawerClosed(Android.Views.View drawerView)
        {
            base.OnDrawerClosed(drawerView);
            // Reset any transformations if not fully reset by slideOffset = 0
            if (_contentViewToAnimate != null)
            {
                _contentViewToAnimate.TranslationX = 0;
                _contentViewToAnimate.ScaleX = 1f;
                _contentViewToAnimate.ScaleY = 1f;
                _contentViewToAnimate.Alpha = 1f;
            }
            drawerView.AnnounceForAccessibility("Navigation menu closed");
        }

        public override void OnDrawerStateChanged(int newState)
        {
            base.OnDrawerStateChanged(newState);
            // newState can be DrawerLayout.StateIdle, StateDragging, StateSettling
        }
    }
    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }

    internal static void HandleIntent(Intent? intent)
    {
        Debug.WriteLine($"HandleIntent invoked with Intent: {intent}"); // Add logging!
    }
}
