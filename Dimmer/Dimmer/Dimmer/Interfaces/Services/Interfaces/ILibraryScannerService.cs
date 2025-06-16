namespace Dimmer.Interfaces.Services.Interfaces;
public interface ILibraryScannerService
{
    void LoadInSongsAndEvents();
    Task ReScanLibraryAsync();
    Task<LoadSongsResult?> ScanLibraryAsync(List<string> folderPaths); // Full scan
    Task<LoadSongsResult?> ScanSpecificPathsAsync(List<string> pathsToScan, bool isIncremental = true); // Incremental for new files/folders
}
