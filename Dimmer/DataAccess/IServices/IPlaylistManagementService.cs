
namespace Dimmer_MAUI.DataAccess.IServices;
public interface IPlaylistManagementService
{
    ObservableCollection<PlaylistModelView> AllPlaylists { get; set; }
    ObservableCollection<PlaylistModelView> GetPlaylists();
    IList<string>? GetSongsIDsFromPlaylistID(string? playlistID);
    //bool AddSongsToPlaylist(SongsModelView song, PlaylistModel playlist, PlaylistSongLink playlistSongLink);
    bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink = null, bool IsAddSong=false, bool IsRemoveSong=false, bool IsDeletePlaylist=false);
   
    bool RenamePlaylist(string playlistID, string newPlaylistName);
    bool DeletePlaylist(string playlistID);
    bool AddSongsToPlaylist(string playlistID, List<string> songIDs);
}
