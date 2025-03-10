namespace Dimmer_MAUI.Platforms.Windows;

public class FilesAndDirectoryWatcher
{
    private readonly string _monitoredDirectory;
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly List<string> _allowedFileExtensions = [".mp3", ".flac", ".m4a", ".wav", ".txt", ".lrc"];
    private readonly BackgroundWorker _backgroundWorker = new BackgroundWorker(); // For UI updates
    private readonly BindingList<string> _fileChanges = [];  //UI binding

    //Event handler to raise UI updates (when file changes occur)
    public event EventHandler<FileChangeEventArgs>? FileChanged;

    //Event Args class to pass data
    public class FileChangeEventArgs : EventArgs
    {
        public string? ChangeType { get; set; } // "Created", "Deleted", "Renamed"
        public string? FilePath { get; set; }
    }

    public FilesAndDirectoryWatcher(string monitoredDirectory)
    {
        _monitoredDirectory = monitoredDirectory ?? Environment.SpecialFolder.MyMusic.ToString();

        if (!Directory.Exists(_monitoredDirectory))
        {
            throw new DirectoryNotFoundException($"The directory '{_monitoredDirectory}' does not exist.");
        }

        _fileSystemWatcher = new FileSystemWatcher(_monitoredDirectory)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = false
        };

        _fileSystemWatcher.Created += OnFileCreated;
        _fileSystemWatcher.Deleted += OnFileDeleted;
        _fileSystemWatcher.Renamed += OnFileRenamed;
        _fileSystemWatcher.Error += OnWatcherError;

        _backgroundWorker.WorkerSupportsCancellation = true;
        _backgroundWorker.DoWork += BackgroundWorker_DoWork; // Set work
        _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted; //Set completion

    }

    // BackgroundWorker handlers (for UI updates)
    private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        // This is where you would perform any long-running operations
        // that shouldn't block the UI thread. However, in this
        // specific case, since we are merely updating the UI, we
        // don't need to do any long-running operations here.
    }

    private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        // Handle completion (if any)
        // This is where you would perform any actions after the long-running
        // operation has completed. However, since we are updating the UI
        // (and that happens on a separate thread), we don't need to do
        // anything here.
    }

    public void StartMonitoring()
    {
        if (!_fileSystemWatcher.EnableRaisingEvents)
        {
            _fileSystemWatcher.EnableRaisingEvents = true;
        }
    }

    public void StopMonitoring()
    {
        if (_fileSystemWatcher.EnableRaisingEvents)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (IsValidFile(e.FullPath))
        {
            OnFileChanged("Created", e.FullPath);
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (IsValidFile(e.FullPath))
        {
            OnFileChanged("Deleted", e.FullPath);
        }
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Important: Renamed event gives both new and old paths.
        if (IsValidFile(e.FullPath) || IsValidFile(e.OldFullPath)) // Check either
        {
            OnFileChanged("Renamed", $"{e.OldFullPath} -> {e.FullPath}");
        }
    }

    private void OnWatcherError(object sender, System.IO.ErrorEventArgs e)
    {
        Debug.WriteLine($"FileSystemWatcher Error: {e.GetException().Message}");
        StopMonitoring(); // Or handle the error further
    }

    private bool IsValidFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            string? extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            if (extension == null)
                return false;

            return _allowedFileExtensions.Contains(extension);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error checking file extension for {filePath}: {ex.Message}");
            return false;
        }
    }

    private void OnFileChanged(string changeType, string filePath)
    {
        FileChanged?.Invoke(this, new FileChangeEventArgs { ChangeType = changeType, FilePath = filePath });
    }

}
