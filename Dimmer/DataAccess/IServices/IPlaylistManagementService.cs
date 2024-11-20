namespace Dimmer_MAUI.DataAccess.IServices;
public interface IPlaylistManagementService
{
    IList<PlaylistModelView> AllPlaylists { get; set; }
    List<PlaylistModelView> GetPlaylists();
    IList<ObjectId> GetSongsIDsFromPlaylistID(ObjectId playlistID);
    //bool AddSongToPlayListWithPlayListID(SongsModelView song, PlaylistModel playlist, PlaylistSongLink playlistSongLink);
    bool UpdatePlayList(PlaylistModelView playlist, PlaylistSongLink? playlistSongLink = null, bool IsAddSong=false, bool IsRemoveSong=false, bool IsDeletePlaylist=false);
    bool RemoveSongFromPlayListWithPlayListID(SongModelView song, ObjectId playlistID);
    bool RemoveSongFromPlayListWithPlayListName(SongModelView song, string playlistName);
    bool RenamePlaylist(ObjectId playlistID, string newPlaylistName);
    bool DeletePlaylist(ObjectId playlistID);
}
