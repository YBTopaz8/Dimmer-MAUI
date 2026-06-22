namespace Dimmer.Interfaces;
public interface IFolderMonitorService : IDisposable
{
    IObservable<FileSystemEventArgs> FileDeleted { get; }
    IObservable<FileSystemEventArgs> FileChanged { get; }
    IObservable<RenamedEventArgs> FileRenamed { get; }
    IObservable<FileSystemEventArgs> FileCreated { get; }

    Task StartAsync(IEnumerable<string> paths);
    void Stop();
    
}