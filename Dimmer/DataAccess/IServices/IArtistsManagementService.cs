namespace Dimmer_MAUI.DataAccess.IServices;

public interface IArtistsManagementService
{
    IList<ArtistModelView> AllArtists { get; set; }
    IList<AlbumArtistSongLink> AlbumsArtistsSongLink { get; set; }
    void GetArtists();

    IList<ObjectId> GetSongsIDsFromArtistID(ObjectId artistID);
    bool AddSongToArtistWithArtistIDAndAlbumAndGenre(List<ArtistModelView> artistModel, List<AlbumModelView> albumModel,
        List<AlbumArtistSongLink> links, List<SongsModel> songs,
        List<GenreModelView> genreModels, List<AlbumArtistGenreSongLink> genreLinks);
    bool UpdateArtist(ArtistModelView artistModel);    
    bool DeleteArtist(ObjectId artistID);
}
