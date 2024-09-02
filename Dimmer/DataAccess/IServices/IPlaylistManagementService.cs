namespace Dimmer_MAUI.DataAccess.IServices;
public interface IPlaylistManagementService
{
    IList<PlaylistModelView> AllPlaylists { get; set; }
    void GetPlaylists();
    IList<ObjectId> GetSongsIDsFromPlaylistID(ObjectId playlistID);
    bool AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    bool AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName);

    bool RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    bool RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName);
    bool RenamePlaylist(ObjectId playlistID, string newPlaylistName);
    bool DeletePlaylist(ObjectId playlistID);
}
