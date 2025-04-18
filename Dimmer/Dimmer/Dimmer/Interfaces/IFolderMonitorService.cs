namespace Dimmer.Interfaces;
public interface IFolderMonitorService : IDisposable
{
    void Start(IEnumerable<string> paths);
    void Stop();
    event Action<string> OnChanged;
}