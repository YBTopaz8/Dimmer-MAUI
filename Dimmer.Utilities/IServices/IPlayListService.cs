namespace Dimmer.Utilities.IServices;
public interface IPlayListService
{
    void AddSongToPlayListWithPlayListID(SongsModel song, ObjectId playlistID);
    void AddSongToPlayListWithPlayListName(SongsModel song, string playlistName);
    
}
