
using ErrorEventArgs = System.IO.ErrorEventArgs;

// Renamed to reflect it monitors a single folder
using System.Collections.Immutable;


namespace Dimmer.Utilities;

public class SingleFolderMonitor : IDisposable
{

    // Shared settings can remain static if they apply to ALL monitors
    private static readonly ImmutableHashSet<string> TargetExtensions = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase, ".lrc", ".m4a", ".flac", ".mp3");

    // Instance fields - each instance monitors one path
    private FileSystemWatcher? _internalWatcher;
    private FileSystemWatcher? _parentWatcher;
    private string? _monitoredFolderName; // Store the name for parent watcher events

    public string? MonitoredPath { get; private set; }
    public bool IsMonitoring { get; private set; }

    // Events for consumers to subscribe to
    public event EventHandler<FileSystemEventArgs>? FileSystemChanged; // Created, Changed, Deleted (relevant files/dirs)
    public event EventHandler<RenamedEventArgs>? FileSystemRenamed; // Renamed (relevant files/dirs)
    public event EventHandler<string>? MonitoredFolderRenamed; // The top-level monitored folder itself was renamed (passes new path)
    public event EventHandler? MonitoredFolderDeleted; // The top-level monitored folder itself was deleted
    public event EventHandler<ErrorEventArgs>? WatcherError;

    // Constructor or an Initialize method
    public SingleFolderMonitor(string pathToMonitor)
    {
        MonitoredPath = pathToMonitor ?? throw new ArgumentNullException(nameof(pathToMonitor));
    }

    public bool StartMonitoring()
    {
        // Prevent starting multiple times
        if (IsMonitoring || _internalWatcher != null)
        {
            Debug.WriteLine($"Monitoring already active or not cleaned up for {MonitoredPath}");
            return IsMonitoring;
        }

        // Reset state in case of restart attempt after error/stop
        CleanupWatchersInternal();

        if (string.IsNullOrWhiteSpace(MonitoredPath) || !Directory.Exists(MonitoredPath))
        {
            Debug.WriteLine($"Cannot monitor non-existent path: {MonitoredPath}");
            MonitoredPath = null; // Indicate failure state
            return false;
        }

        string? parentPath = Path.GetDirectoryName(MonitoredPath);
        _monitoredFolderName = Path.GetFileName(MonitoredPath); // Store for checks

        try
        {
            _internalWatcher = new FileSystemWatcher(MonitoredPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName, // Removed LastAccess (often noisy)
                IncludeSubdirectories = true,
                EnableRaisingEvents = false // Enable after setup
            };

            _internalWatcher.Changed += OnInternalFileSystemChanged;
            _internalWatcher.Created += OnInternalFileSystemChanged;
            _internalWatcher.Deleted += OnInternalFileSystemChanged;
            _internalWatcher.Renamed += OnInternalFileSystemRenamed;
            _internalWatcher.Error += OnWatcherError;
            _internalWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error setting up internal watcher for {MonitoredPath}: {ex.Message}");
            CleanupWatchersInternal(); // Cleanup partial setup
            return false;
        }

        // Setup parent watcher only if parent exists
        if (!string.IsNullOrEmpty(parentPath) && Directory.Exists(parentPath) && !string.IsNullOrEmpty(_monitoredFolderName))
        {
            try
            {
                _parentWatcher = new FileSystemWatcher(parentPath)
                {
                    NotifyFilter = NotifyFilters.DirectoryName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = false // Enable after setup
                };

                // Use instance methods directly or lambdas capturing 'this' context
                _parentWatcher.Renamed += OnParentFolderRenamed;
                _parentWatcher.Deleted += OnParentFolderDeleted;
                _parentWatcher.Error += OnWatcherError;
                _parentWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                // Log the error, but maybe continue with just internal monitoring?
                // Or decide to fail completely. Here, we log and dispose parent.
                Debug.WriteLine($"Warning: Error setting up parent watcher for {MonitoredPath}: {ex.Message}. Monitoring rename/delete of the folder itself might fail.");
                _parentWatcher?.Dispose();
                _parentWatcher = null;
            }
        }

        IsMonitoring = true;
        Debug.WriteLine($"Started monitoring: {MonitoredPath}");
        return true;
    }

    public static bool IsTargetFileExtension(string? filePath) // Made static public helper
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        string extension = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(extension) && TargetExtensions.Contains(extension);
    }

    // --- Event Handlers ---

    private void OnInternalFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        // Determine if it's a directory or a file we care about
        bool isDirectory = Directory.Exists(e.FullPath) || (!File.Exists(e.FullPath) && !Path.HasExtension(e.FullPath) && e.ChangeType != WatcherChangeTypes.Deleted);
        // Refine directory check for delete
        if (e.ChangeType == WatcherChangeTypes.Deleted && !Path.HasExtension(e.FullPath) && !File.Exists(e.FullPath) /*Ensure it's not a file delete*/)
        {
            // Heuristic: If it doesn't have an extension and doesn't exist anymore, assume it was a directory.
            // This might have edge cases (files without extensions).
            // A more robust way might involve caching directory structure, but that's complex.
            isDirectory = true;
        }


        if (isDirectory || IsTargetFileExtension(e.FullPath))
        {
            Debug.WriteLine($"[{MonitoredPath}] Event: {e.ChangeType} - {(isDirectory ? "Dir" : "File")}: {e.Name}");
            FileSystemChanged?.Invoke(this, e); // Raise the instance event
        }
        else
        {
            Debug.WriteLine($"[{MonitoredPath}] Event Ignored (non-target): {e.ChangeType} - {e.Name}");
        }
    }

    private void OnInternalFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        // Check if either old or new path involves a directory or a target file
        bool oldPathIsDir = !Path.HasExtension(e.OldFullPath); // Heuristic
        bool newPathIsDir = Directory.Exists(e.FullPath); // Check existence for new path
        bool wasTarget = IsTargetFileExtension(e.OldFullPath);
        bool isTarget = IsTargetFileExtension(e.FullPath);
        bool isDirectoryRename = oldPathIsDir || newPathIsDir; // If either looks like a dir

        if (isDirectoryRename || wasTarget || isTarget)
        {
            Debug.WriteLine($"[{MonitoredPath}] Event: Renamed - {(isDirectoryRename ? "Dir" : "File")}: {e.OldName} -> {e.Name}");
            FileSystemRenamed?.Invoke(this, e); // Raise the instance event
        }
        else
        {
            Debug.WriteLine($"[{MonitoredPath}] Event Ignored (non-target rename): {e.OldName} -> {e.Name}");
        }
    }

    private void OnParentFolderRenamed(object sender, RenamedEventArgs e)
    {
        // Check if the renamed item *was* our monitored folder
        if (!string.IsNullOrEmpty(_monitoredFolderName) &&
            string.Equals(e.OldName, _monitoredFolderName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine($"Monitored folder {e.OldFullPath} RENAMED to {e.FullPath}");

            // Stop current watchers BEFORE updating path
            CleanupWatchersInternal(isStoppingPermanently: false); // Don't null MonitoredPath yet

            // Update the path this instance is responsible for
            MonitoredPath = e.FullPath;

            // Raise event BEFORE restarting watchers, in case consumer needs the new path
            MonitoredFolderRenamed?.Invoke(this, MonitoredPath);

            // Attempt to restart monitoring on the new path
            if (!StartMonitoring())
            {
                Debug.WriteLine($"Failed to restart monitoring after rename for new path: {MonitoredPath}");
                // Consider raising an error event here
                WatcherError?.Invoke(this, new ErrorEventArgs(new Exception($"Failed to restart monitoring after rename to {MonitoredPath}")));
                IsMonitoring = false; // Ensure state is consistent
                MonitoredPath = null; // Mark as failed/stopped
            }
        }
    }

    private void OnParentFolderDeleted(object sender, FileSystemEventArgs e)
    {
        // Check if the deleted item *was* our monitored folder
        if (!string.IsNullOrEmpty(_monitoredFolderName) &&
            string.Equals(e.Name, _monitoredFolderName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.WriteLine($"Monitored folder {e.FullPath} DELETED");
            // The folder is gone, cleanup everything
            CleanupWatchersInternal(isStoppingPermanently: true); // Full cleanup
            MonitoredFolderDeleted?.Invoke(this, EventArgs.Empty); // Notify
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Debug.WriteLine($"Watcher error for {MonitoredPath ?? "Unknown Path"}: {e.GetException()?.Message}");
        // Optionally attempt recovery, or just notify consumer
        WatcherError?.Invoke(this, e);

        // Decide on recovery strategy. A common error is buffer overflow.
        // Sometimes, stopping and restarting monitoring can help.
        // For now, we just notify. Consider adding auto-restart logic if needed.
        // A simple approach could be to call Cleanup and then try StartMonitoring again after a delay.
        // Or just rely on the consumer to handle the error event.

    }


    // --- Cleanup / Dispose ---

    // Internal cleanup used by Dispose and potentially by recovery logic
    private void CleanupWatchersInternal(bool isStoppingPermanently = true)
    {
        IsMonitoring = false; // Mark as not monitoring first

        _internalWatcher?.Dispose(); // Dispose handles disabling events and unsubscribing
        _internalWatcher = null;

        _parentWatcher?.Dispose();
        _parentWatcher = null;

        if (isStoppingPermanently)
        {
            // Clear events only on permanent stop/dispose to avoid issues during rename restart
            FileSystemChanged = null;
            FileSystemRenamed = null;
            MonitoredFolderRenamed = null;
            MonitoredFolderDeleted = null;
            WatcherError = null;
            MonitoredPath = null; // Indicate it's fully stopped
            _monitoredFolderName = null;
        }
        Debug.WriteLine($"Watchers cleaned up for path (or former path)");
    }

    public void StopMonitoring()
    {
        Debug.WriteLine($"Stopping monitoring for: {MonitoredPath}");
        CleanupWatchersInternal(isStoppingPermanently: true);
    }

    public void Dispose()
    {
        // Dispose pattern
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Dispose managed resources
            CleanupWatchersInternal(isStoppingPermanently: true);
        }
        // No unmanaged resources to free in this example
    }

}