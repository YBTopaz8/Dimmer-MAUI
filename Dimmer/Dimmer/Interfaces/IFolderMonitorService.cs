namespace Dimmer.Interfaces;
public interface IFolderMonitorService : IDisposable
{
    Task StartAsync(IEnumerable<string> paths);
    void Stop();
    event Action<string> OnChanged;
    event Action<FileSystemEventArgs>? OnCreated;
    event Action<FileSystemEventArgs>? OnDeleted;
    event Action<RenamedEventArgs>? OnRenamed;
}