namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    void AddFolderToWatchListAndScan(string path);
    void ClearAllWatchedFoldersAndRescanAsync();
    void RemoveFolderFromWatchListAsync(string path);
}
