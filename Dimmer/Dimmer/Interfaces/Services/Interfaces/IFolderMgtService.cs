
namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    Task AddFolderToWatchListAndScan(string path);
    void AddManyFoldersToWatchListAndScan(List<string> paths);
    void ClearAllWatchedFoldersAndRescanAsync();
    void RemoveFolderFromWatchListAsync(string path);
    void StartWatchingConfiguredFolders();
}
