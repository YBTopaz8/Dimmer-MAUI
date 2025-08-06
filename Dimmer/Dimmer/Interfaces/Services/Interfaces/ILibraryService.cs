namespace Dimmer.Interfaces.Services.Interfaces;

public class LoadSongsResult // Define this class (or similar) if needed
{
    public IReadOnlyList<SongModelView> Songs { get; set; } = Array.Empty<SongModelView>();
    public IReadOnlyList<ArtistModelView> Artists { get; set; } = Array.Empty<ArtistModelView>();
    public IReadOnlyList<AlbumModelView> Albums { get; set; } = Array.Empty<AlbumModelView>();
    public IReadOnlyList<GenreModelView> Genres { get; set; } = Array.Empty<GenreModelView>();
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int NewSongsAdded { get; set; }
}

//public interface ILibraryService
//{
//    Task<LoadSongsResult?> ScanAndImportSongsAsync(IEnumerable<string> folderPaths);
//    Task InitializeMasterListsAsync(); // To load data on startup
//    IObservable<IReadOnlyList<SongModel>> AllSongsObservable { get; } // If needed
//    IObservable<double> ScanProgress { get; } // 0.0 to 1.0
//    IObservable<string> CurrentScanStatusMessage { get; }
//}