namespace Dimmer.Interfaces;
public interface ILibraryScannerService
{

    void RemoveDupesFromDB();
    Task<LoadSongsResult> ScanLibrary(List<string>? folderPaths, bool isIncremental = false); // Full scan
    Task ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true); // Incremental for new files/folders
}
