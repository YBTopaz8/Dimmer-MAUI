
namespace Dimmer_MAUI.DataAccess.IServices;
public interface ISongsManagementService
{
    
    bool AddSongBatchAsync(IEnumerable<SongModelView> song);
    bool AddArtistsBatch(IEnumerable<ArtistModelView> artists);
    Task<bool> LoadSongsFromFolderAsync(List<string> folderPath);//to load songs from folder
    void GetSongs();

    /// <summary>
    /// Adds/Updates a single song to the database.
    /// </summary>
    /// <param name="SongModelView"></param>
    /// <returns></returns>
    bool UpdateSongDetails(SongModelView songModelView);
    Task AddPlayAndCompletionLinkAsync(PlayDateAndCompletionStateSongLink link, bool SyncSave=false);

    void Dispose();

    List<SongModelView> AllSongs { get; internal set; }
    
    List<AlbumModelView> AllAlbums { get; internal set; }
    List<ArtistModelView> AllArtists { get; internal set; }
    List<GenreModelView> AllGenres { get; internal set; }
    List<AlbumArtistGenreSongLinkView> AllLinks { get; internal set; }
    void GetAlbums(); 
    void GetArtists(); 
    void GetGenres();
    void UpdateAlbum(AlbumModelView album);
    

    bool DeleteSongFromDB(SongModelView song);
    Task<bool> MultiDeleteSongFromDB(ObservableCollection<SongModelView> songs);

    UserModelView CurrentOfflineUser { get; internal set; }
    ParseUser? CurrentUserOnline { get; internal set; }
    

    Task<UserModelView?> GetUserAccountOnline();
    void LogOutUser();
    Task<bool> IsEmailVerified();
    Task<bool> LoginAndCheckEmailVerificationAsync(string username, string password);
    
    bool RequestPasswordResetAsync(string email);
    Task<bool> LogUserOnlineAsync(string email, string password);
    
    Task GetAllDataFromOnlineAsync();
    UserModelView? GetUserAccount(ParseUser? usr = null);
    Task SendAllDataToServerAsInitialSync();
    void UpdateUserLoginDetails(ParseUser usrr);
    void InitApp(HomePageVM vm);
    Task<bool> SyncPlayDataAndCompletionData();

    public void AddPDaCStateLink(PlayDateAndCompletionStateSongLink model);
    public void RestoreAllOnlineData(List<PlayDateAndCompletionStateSongLink> playDataLinks, List<SongModel> songs,
        List<AlbumModel> albums, List<GenreModel> allGenres,
        List<PlaylistModel> allPlaylists, List<AlbumArtistGenreSongLink> otherLinks);

    public List<PlayDataLink> AllPlayDataLinks { get; internal set; }

}
