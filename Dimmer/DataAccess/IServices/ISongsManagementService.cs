
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
    void AddPlayAndCompletionLink(PlayDateAndCompletionStateSongLink link);

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

    UserModelView CurrentOfflineUser { get; internal set; }
    ParseUser? CurrentUserOnline { get; internal set; }
    IList<PlayDateAndCompletionStateSongLinkView> AllPlayDataAndCompletionStateLinks { get; set; }

    
    Task<UserModelView?> GetUserAccountOnline();
    void LogOutUser();
    bool IsEmailVerified();
    Task<bool> LoginAndCheckEmailVerificationAsync(string username, string password);
    
    bool RequestPasswordResetAsync(string email);
    bool LogUserOnlineAsync(string email, string password);
    bool SignUpUserOnlineAsync(string email, string password);
    
    Task GetAllDataFromOnlineAsync();
    UserModelView? GetUserAccount(ParseUser? usr = null);
    Task SendAllDataToServerAsInitialSync();
    void UpdateUserLoginDetails(ParseUser usrr);
}
