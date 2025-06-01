namespace Dimmer.Interfaces.Services.Interfaces;

public interface IFolderMgtService : IDisposable
{
    Task AddFolderToWatchListAndScanAsync(string path);
    Task ClearAllWatchedFoldersAndRescanAsync();
    Task RemoveFolderFromWatchListAsync(string path);
}
