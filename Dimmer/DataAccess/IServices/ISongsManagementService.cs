namespace Dimmer_MAUI.DataAccess.IServices;
public interface ISongsManagementService
{
    IList<SongModelView> AllSongs { get; internal set; }
    IList<AlbumArtistGenreSongLinkView> AllLinks { get; internal set; }    
    
    bool AddSongBatchAsync(IEnumerable<SongModelView> song);
    bool AddArtistsBatch(IEnumerable<ArtistModelView> artists);
    Task<bool> LoadSongsFromFolderAsync(List<string> folderPath);//to load songs from folder
    void GetSongs();
    bool UpdateSongDetails(SongModelView songsModelView);
    bool UpdatePlayAndCompletionDetails(PlayDateAndCompletionStateSongLink link);

    void Dispose();

    IList<AlbumModelView> AllAlbums { get; internal set; }
    IList<ArtistModelView> AllArtists { get; internal set; }
    IList<GenreModelView> AllGenres { get; internal set; }
    void GetAlbums(); 
    void GetArtists(); 
    void GetGenres();
    void UpdateAlbum(AlbumModelView album);
    

    bool DeleteSongFromDB(SongModelView song);
    Task<bool> MultiDeleteSongFromDB(ObservableCollection<SongModelView> songs);

    UserModelView CurrentUser { get; internal set; }
    UserModelView? GetUserAccount();
    Task<UserModelView?> GetUserAccountOnline();
    
}
