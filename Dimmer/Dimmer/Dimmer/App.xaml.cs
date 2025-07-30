
//using Dimmer.DimmerLive.Models;
//using Dimmer.DimmerLive.Orchestration;

using Dimmer.DimmerLive.Orchestration;
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer;

public partial class App : Application
{

    public App()
    {
        InitializeComponent();
        //AddPlatformResources();           // ← call your platform hook

        // Handle unhandled exceptions
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
        //if (ParseSetup.InitializeParseClient())
        //{
        //    ParseClient.Instance.RegisterSubclass(typeof(UserDeviceSession));
        //    ParseClient.Instance.RegisterSubclass(typeof(ChatConversation));
        //    ParseClient.Instance.RegisterSubclass(typeof(ChatMessage));
        //    ParseClient.Instance.RegisterSubclass(typeof(DimmerSharedSong));
        //    ParseClient.Instance.RegisterSubclass(typeof(UserModelOnline));
        //    ParseClient.Instance.RegisterSubclass(typeof(DeviceState));
        //    ParseClient.Instance.RegisterSubclass(typeof(UserModelOnline));
        //    ParseClient.Instance.RegisterSubclass(typeof(FriendRequest));

        //}
    }
    //public partial void AddPlatformResources()
    // {
    //     // Provide a platform-specific implementation here.
    //     // For example, you can add platform-specific resources or configurations.
    //     // If no specific implementation is needed, leave this method empty.
    // }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        IAppUtil appUtil = IPlatformApplication.Current!.Services.GetRequiredService<IAppUtil>();
        return appUtil.LoadWindow();
    }

    private static readonly object _logLock = new();
    private static void CurrentDomain_FirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        var ex = e.Exception;
        if (ex.Message.Contains("Operation is not valid due to the current state of the object.")
            && ex.StackTrace?.Contains("WinRT.ExceptionHelpers") == true)
        {
            // This is the noisy exception we want to ignore.
            // Just return and don't log it.
            return;
        }
        if (ex is System.IO.FileNotFoundException
     && ex.Message.StartsWith("Could not load file or assembly 'ReactiveUI."))
        {
            // This is ReactiveUI probing for platform-specific assemblies (WPF, Blazor, etc.).
            // It's expected behavior and safe to ignore.
            return;
        }
        string errorDetails = $"********** UNHANDLED EXCEPTION! **********\n" +
                              $"Exception Type: {e.Exception.GetType()}\n" +
                              $"Message: {e.Exception.Message}\n" +
                              $"Source: {e.Exception.Source}\n" +
                              $"Stack Trace: {e.Exception.StackTrace}\n";

        if (e.Exception.InnerException != null)
        {
            errorDetails += "***** Inner Exception *****\n" +
                            $"innerMessage: {e.Exception.InnerException.Message}\n" +
                            $"Stack Trace: {e.Exception.InnerException.StackTrace}\n";
        }

        // Print to Debug Console
        Debug.WriteLine(errorDetails);

        // Log to file
      LogException(e.Exception);


    }

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
            string fileName = $"MAUIcrashlog_{DateTime.Now:yyyy-MM-dd}.txt";
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

}
