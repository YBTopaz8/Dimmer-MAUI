namespace Dimmer.DataAccess.IServices;
public interface IPlaylistManagementService
{
    bool AddSongToPlayListWithPlayListID(SongsModel song, ObjectId playlistID);
    bool AddSongToPlayListWithPlayListName(SongsModel song, string playlistName);

}
