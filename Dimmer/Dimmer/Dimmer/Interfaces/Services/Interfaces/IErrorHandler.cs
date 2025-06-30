namespace Dimmer.Interfaces.Services.Interfaces;
public interface IErrorHandler
{
    void HandleError(Exception ex);
}


public class ErrorHandler : IErrorHandler
{
    private static readonly object _logLock = new();
    private const string LogDirectoryName = "DimmerCrashLogs";
    public void HandleError(Exception ex)
    {
        LogException(ex);
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Application.Current?.Windows[0] != null)
            {
                await Shell.Current.DisplayAlert("An Unexpected Error Occurred", "The application has encountered an error. Please try again. If the problem persists, please contact support.", "OK");
            }
            else
            {
                Debug.WriteLine($"Error: MainPage is null. Could not display alert. Exception: {ex}");
            }
        });
    }

    public static void LogException(Exception ex)
    {
        try
        {
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LogDirectoryName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string fileName = $"Loggercrashlog_{DateTime.Now:yyyy-MM-dd}.txt";
            string filePath = Path.Combine(directoryPath, fileName);
            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nException Type: {ex.GetType()}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}\n";
            if (ex.InnerException != null)
            {
                logContent += $"Inner Exception:\nMessage: {ex.InnerException.Message}\nStackTrace: {ex.InnerException.StackTrace}\n";
            }
            logContent += "\n";

            bool success = false;
            int retries = 3;
            int delay = 500;

            lock (_logLock)
            {
                while (retries-- > 0 && !success)
                {
                    try
                    {
#if RELEASE || DEBUG
                        File.AppendAllText(filePath, logContent);
                        success = true;
#endif
                    }
                    catch (IOException ioEx) when (retries > 0)
                    {
                        Debug.WriteLine(
                        $"Failed to log exception: {ioEx}");
                    }
                }
            }
        }
        catch (Exception loggingEx)
        {

            Debug.WriteLine($"Logging failed: {loggingEx}");
        }
    }
}
