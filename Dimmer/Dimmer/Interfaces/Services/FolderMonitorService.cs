namespace Dimmer.Interfaces.Services;

public class FolderMonitorService : IFolderMonitorService
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private bool _disposed;

    // Rx Subjects instead of standard C# events
    private readonly Subject<FileSystemEventArgs> _onCreated = new();
    private readonly Subject<FileSystemEventArgs> _onDeleted = new();
    private readonly Subject<FileSystemEventArgs> _onChanged = new();
    private readonly Subject<RenamedEventArgs> _onRenamed = new();

    // Expose as Observables
    public IObservable<FileSystemEventArgs> FileCreated => _onCreated.AsObservable();
    public IObservable<FileSystemEventArgs> FileDeleted => _onDeleted.AsObservable();
    public IObservable<FileSystemEventArgs> FileChanged => _onChanged.AsObservable();
    public IObservable<RenamedEventArgs> FileRenamed => _onRenamed.AsObservable();

    public Task StartAsync(IEnumerable<string> paths)
    {
        return Task.Run(() =>
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
                    // Massively increase buffer size to prevent skipped events during heavy file operations (e.g. dragging 1000 songs)
                    InternalBufferSize = 65536,
                    // Only listen to what we care about to reduce noise
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                w.Created += (_, e) => _onCreated.OnNext(e);
                w.Deleted += (_, e) => _onDeleted.OnNext(e);
                w.Changed += (_, e) => _onChanged.OnNext(e);
                w.Renamed += (_, e) => _onRenamed.OnNext(e);

                // Optional: catch internal buffer overflows
                w.Error += (_, e) => System.Diagnostics.Debug.WriteLine($"FSW Error: {e.GetException().Message}");

                w.EnableRaisingEvents = true;
                _watchers.Add(w);
            }
        });
    }

    public void Stop()
    {
        lock (_watchers)
        {
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();
                _onCreated.Dispose();
                _onDeleted.Dispose();
                _onChanged.Dispose();
                _onRenamed.Dispose();
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