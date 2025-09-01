
namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    Task AddFolderToWatchListAndScan(string path);
    void AddManyFoldersToWatchListAndScan(List<string> paths);
    Task ClearAllWatchedFoldersAndRescanAsync();
    Task RemoveFolderFromWatchListAsync(string path);
    Task StartWatchingConfiguredFoldersAsync();
}
