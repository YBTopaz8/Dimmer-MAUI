namespace Dimmer.DataAccess.IServices;
public interface IPlaylistManagementService
{
    bool AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    bool AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName);
    bool RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    bool RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName);
}
