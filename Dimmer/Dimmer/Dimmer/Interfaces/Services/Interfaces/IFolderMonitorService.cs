namespace Dimmer.Interfaces.Services.Interfaces;
public interface IFolderMonitorService : IDisposable
{
    void Start(IEnumerable<string> paths);
    void Stop();
    event Action<string> OnChanged;
    event Action<FileSystemEventArgs>? OnCreated;
    event Action<FileSystemEventArgs>? OnDeleted;
    event Action<RenamedEventArgs>? OnRenamed;
}