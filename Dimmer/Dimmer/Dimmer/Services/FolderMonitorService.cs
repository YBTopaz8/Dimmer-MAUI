namespace Dimmer.Services;
public class FolderMonitorService : IFolderMonitorService
{
    
    private readonly List<FileSystemWatcher> _watchers = [];
    private bool _disposed;

    public event Action<string>? OnChanged;
    public event Action<FileSystemEventArgs>? OnCreated;
    public event Action<FileSystemEventArgs>? OnDeleted;
    public event Action<RenamedEventArgs>? OnRenamed;

    public void Start(IEnumerable<string> paths)
    {
        Stop();
        if (paths == null || !paths.Any())
            return;
        foreach (var p in paths)
        {
            if (!Directory.Exists(p))
                continue;
            var w = new FileSystemWatcher(p)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            w.Changed += (_, e) => OnChanged?.Invoke(e.FullPath);
            w.Created += (_, e) => OnCreated?.Invoke(e);
            w.Renamed += (_, e) => OnRenamed?.Invoke(e);
            w.Deleted += (_, e) => OnDeleted?.Invoke(e);
            _watchers.Add(w);
        }
    }

    public void Stop()
    {
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
