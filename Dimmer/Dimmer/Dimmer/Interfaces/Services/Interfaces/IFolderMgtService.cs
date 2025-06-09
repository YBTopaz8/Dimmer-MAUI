namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    void AddFolderToWatchListAndScanAsync(string path);
    void ClearAllWatchedFoldersAndRescanAsync();
    void RemoveFolderFromWatchListAsync(string path);
}
