namespace Dimmer.Interfaces.Services.Interfaces;
public interface ILibraryScannerService
{
    void LoadInSongsAndEvents();
    void RemoveDupesFromDB();
    Task<LoadSongsResult?>? ScanLibrary(List<string>? folderPaths); // Full scan
    Task<LoadSongsResult?>? ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true); // Incremental for new files/folders
}
