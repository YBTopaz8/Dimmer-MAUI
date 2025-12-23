namespace Dimmer.Interfaces;

public class LoadSongsResult // Define this class (or similar) if needed
{
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int NewSongsAddedCount { get; set; }
    public List<FileProcessingResult>? ProcessingResults { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    public bool IsError { get; internal set; }
    public int AlbumsCount { get; internal set; }
    public int ArtistsCount { get; internal set; }
    public int GenresCount { get; internal set; }
}

//public interface ILibraryService
//{
//    Task<LoadSongsResult?> ScanAndImportSongsAsync(IEnumerable<string> folderPaths);
//    Task InitializeMasterListsAsync(); // To load data on startup
//    IObservable<IReadOnlyList<SongModel>> AllSongsObservable { get; } // If needed
//    IObservable<double> ScanProgress { get; } // 0.0 to 1.0
//    IObservable<string> CurrentScanStatusMessage { get; }
//}