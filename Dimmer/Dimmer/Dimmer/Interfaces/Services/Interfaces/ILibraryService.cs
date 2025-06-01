using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services.Interfaces;

public class LoadSongsResult // Define this class (or similar) if needed
{
    public IReadOnlyList<SongModel> Songs { get; set; } = Array.Empty<SongModel>();
    public IReadOnlyList<ArtistModel> Artists { get; set; } = Array.Empty<ArtistModel>();
    public IReadOnlyList<AlbumModel> Albums { get; set; } = Array.Empty<AlbumModel>();
    public IReadOnlyList<GenreModel> Genres { get; set; } = Array.Empty<GenreModel>();
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public int NewSongsAdded { get; set; }
}

public interface ILibraryService
{
    Task<LoadSongsResult?> ScanAndImportSongsAsync(IEnumerable<string> folderPaths);
    Task InitializeMasterListsAsync(); // To load data on startup
    IObservable<IReadOnlyList<SongModel>> AllSongsObservable { get; } // If needed
    IObservable<double> ScanProgress { get; } // 0.0 to 1.0
    IObservable<string> CurrentScanStatusMessage { get; }
}