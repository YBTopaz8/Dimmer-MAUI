using System.Collections.Specialized;

namespace Dimmer.Utilities.IServices;
public interface IPlayListService
{
    string SelectedPlaylistName { get; }
    IList<PlaylistModelView> AllPlaylists { get; }
    IList<SongsModelView> SongsFromPlaylist { get; }
    void AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName);

    void RemoveFromPlayListWithPlayListName(SongsModelView song, string playListName);
    void RemoveFromPlayListWithPlayListID(SongsModelView song, ObjectId playListID);
    void GetPlaylistDetails(ObjectId playlistID);
    void GetSongsFromPlaylistID(ObjectId playlistID);
    bool DeletePlaylistThroughID(ObjectId playlistID);

    event NotifyCollectionChangedEventHandler AllPlaylistsChanged;
}
