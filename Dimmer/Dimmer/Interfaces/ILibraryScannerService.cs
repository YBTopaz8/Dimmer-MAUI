namespace Dimmer.Interfaces;
public interface ILibraryScannerService
{

    void RemoveDupesFromDB();
    Task<LoadSongsResult> ScanLibrary(List<string>? folderPaths); // Full scan
    Task<LoadSongsResult> ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true); // Incremental for new files/folders
}
