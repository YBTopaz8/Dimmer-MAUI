namespace Dimmer.Interfaces;

public interface IArtistActions
{
    void QuickViewArtist(SongModelView song, string artistName);
    void PlaySongsByArtistInCurrentAlbum(SongModelView song, string artistName);
    void PlayAllSongsByArtist(SongModelView song, string artistName);
    void QueueAllSongsByArtist(SongModelView song, string artistName);
    void NavigateToArtistPage(SongModelView song, string artistName);
    bool IsArtistFavorite(SongModelView song, string artistName);
    void ToggleFavoriteArtist(SongModelView song, string artistName, bool isFavorite);
    // Optional stats
    int GetArtistPlayCount(SongModelView song,string artistName);        // return 0 if unknown
    bool IsArtistFollowed(SongModelView song, string artistName);          // if you have the concept
    void QuickViewArtistSortByAlbum();
    void QuickViewArtistSortByGenres( );
}
