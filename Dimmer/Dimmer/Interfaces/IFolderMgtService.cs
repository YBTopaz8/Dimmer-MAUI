namespace Dimmer.Interfaces;

public interface IFolderMgtService : IDisposable
{
    Task AddFolderToWatchListAndScan(string path);

    Task AddManyFoldersToWatchListAndScan(List<string> paths);
    Task ClearAllWatchedFoldersAndRescanAsync();
    Task RemoveFolderFromWatchListAsync(string path);
    Task ReScanFolder(string folderPath);
    Task StartWatchingConfiguredFoldersAsync();
    Task UpdateFolderInWatchListAsync(string path);
}
