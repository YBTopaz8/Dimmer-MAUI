using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;
public interface IMusicMetadataService
{
    ArtistModelView GetOrCreateArtist(Track track, string name);
    AlbumModelView GetOrCreateAlbum(Track track, string name, string? initialCoverPath = null);
    GenreModelView GetOrCreateGenre(Track track, string name);
    void AddSong(SongModelView song); // For tracking processed songs, e.g., for duplicate checks

    IReadOnlyList<ArtistModelView> GetAllArtists();
    IReadOnlyList<AlbumModelView> GetAllAlbums();
    IReadOnlyList<GenreModelView> GetAllGenres();
    //IReadOnlyList<SongModelView> GetAllSongs();

    void LoadExistingData(
        IEnumerable<ArtistModelView> existingArtists,
        IEnumerable<AlbumModelView> existingAlbums,
        IEnumerable<GenreModelView> existingGenres,
        IEnumerable<SongModelView> existingSongs
        );
    bool DoesSongExist(string title, int durationInSeconds, out SongModelView? existingSong);
    void MarkAsUpdated(SongModelView song);
    bool DoesSongExist(string title, double durationInSeconds);
}
