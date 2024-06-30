namespace Dimmer.Utilities.IServices;
public interface IPlayListService
{
    IObservable<IList<PlaylistModelView>> AllPlaylists { get; }
    IObservable<IList<SongsModelView>> SongsFromPlaylist { get; }
    void AddSongToPlayListWithPlayListID(SongsModel song, ObjectId playlistID);
    void AddSongToPlayListWithPlayListName(SongsModel song, string playlistName);
    void GetPlaylistDetails(ObjectId playlistID);
    void GetSongsFromPlaylistID(ObjectId playlistID);
    bool DeletePlaylistThroughID(ObjectId playlistID);
}
