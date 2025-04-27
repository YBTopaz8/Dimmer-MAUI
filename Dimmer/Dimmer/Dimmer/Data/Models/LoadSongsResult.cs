namespace Dimmer.Data.Models;
public class LoadSongsResult
{
    public required List<ArtistModel> Artists { get; set; }
    public required List<AlbumModel> Albums { get; set; }
    public required List<AlbumArtistGenreSongLink> Links { get; set; }
    public required List<SongModel> Songs { get; set; }
    public required List<GenreModel> Genres { get; set; }
}