namespace Dimmer.Utilities.FileProcessorUtils;
public interface IMusicMetadataService
{
    ArtistModel GetOrCreateArtist(string name);
    AlbumModel GetOrCreateAlbum(string name, string? initialCoverPath = null);
    GenreModel GetOrCreateGenre(string name);
    void AddSong(SongModel song); // For tracking processed songs, e.g., for duplicate checks
    bool DoesSongExist(string title, int durationInSeconds); // Example duplicate check

    IReadOnlyList<ArtistModel> GetAllArtists();
    IReadOnlyList<AlbumModel> GetAllAlbums();
    IReadOnlyList<GenreModel> GetAllGenres();
    IReadOnlyList<SongModel> GetAllSongs();

    void LoadExistingData(
        IEnumerable<ArtistModel> existingArtists,
        IEnumerable<AlbumModel> existingAlbums,
        IEnumerable<GenreModel> existingGenres,
        IEnumerable<SongModel> existingSongs
        );
}
