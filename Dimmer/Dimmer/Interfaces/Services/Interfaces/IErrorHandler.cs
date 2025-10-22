using Dimmer.Utils;

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
        
    }

    private static readonly ExceptionFilterPolicy _filterPolicy = new ExceptionFilterPolicy();
    public static void LogException(Exception ex)
    {
        if (!_filterPolicy.ShouldLog(ex))
        {
            return; // Ignore this exception based on our policy.
        }
        try
        {
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), LogDirectoryName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            string fileName = $"MAUIILoggercrashlog_{DateTime.Now:yyyy-MM-dd}.txt";
            string filePath = Path.Combine(directoryPath, fileName);
            string logContent = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\nException Type: {ex.GetType()}\nMessage: {ex.Message}\nStackTraceLogger: {ex.StackTrace}\n";
            if (ex.InnerException != null)
            {
                logContent += $"Inner Exception:\nMessage: {ex.InnerException.Message}\nStackTraceLogger: {ex.InnerException.StackTrace}\n";
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
