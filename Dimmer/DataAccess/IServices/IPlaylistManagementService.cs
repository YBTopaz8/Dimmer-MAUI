namespace Dimmer_MAUI.DataAccess.IServices;
public interface IPlaylistManagementService
{
    IList<PlaylistModelView> AllPlaylists { get; set; }
    List<PlaylistModelView> GetPlaylists();
    IList<string> GetSongsIDsFromPlaylistID(string? playlistID);
    //bool AddSongToPlayListWithPlayListID(SongsModelView song, PlaylistModel playlist, PlaylistSongLink playlistSongLink);
    bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink = null, bool IsAddSong=false, bool IsRemoveSong=false, bool IsDeletePlaylist=false);
   
    bool RenamePlaylist(string playlistID, string newPlaylistName);
    bool DeletePlaylist(string playlistID);
}
