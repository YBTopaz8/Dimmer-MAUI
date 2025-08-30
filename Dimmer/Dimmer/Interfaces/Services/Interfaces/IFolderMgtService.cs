
namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    void AddFolderToWatchListAndScan(string path);
    void AddManyFoldersToWatchListAndScan(List<string> paths);
    void ClearAllWatchedFoldersAndRescanAsync();
    void RemoveFolderFromWatchListAsync(string path);
    void StartWatchingConfiguredFolders();
}
