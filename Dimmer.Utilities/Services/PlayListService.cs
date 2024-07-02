
namespace Dimmer.Utilities.Services;
public class PlayListService : IPlayListService
{
    public IObservable<IList<PlaylistModelView>> AllPlaylists => _allPlaylistsSubject.AsObservable();
    BehaviorSubject<IList<PlaylistModelView>> _allPlaylistsSubject = new([]);

    public IPlaylistManagementService PlaylistManagementService { get; }
    public ISongsManagementService SongsManagementService { get; }
    public IObservable<IList<SongsModelView>> SongsFromPlaylist => _allSongsFromPlaylist.AsObservable();
    BehaviorSubject<IList<SongsModelView>> _allSongsFromPlaylist = new([]);


    public PlayListService(IPlaylistManagementService playlistManagementService, ISongsManagementService songsManagementService)
    {
        PlaylistManagementService = playlistManagementService;
        SongsManagementService = songsManagementService;
        _allPlaylistsSubject.OnNext(PlaylistManagementService.AllPlaylists);
    }
    public void AddSongToPlayListWithPlayListID(SongsModel song, ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    public void AddSongToPlayListWithPlayListName(SongsModel song, string playlistName)
    {
        throw new NotImplementedException();
    }

    public void GetPlaylistDetails(ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    ObjectId currentlyLoadedPlaylist;// = ObjectId.Empty;
    public void GetSongsFromPlaylistID(ObjectId playlistID)
    {
        if (playlistID == currentlyLoadedPlaylist) //will remove or change this because what if a user adds/removes a song from PL?
        {
            return;
        }
        var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.Id == playlistID);

        if (specificPlaylist != null)
        {
            
            List<SongsModelView> songsInPlaylist = SongsManagementService.AllSongs
                .Where(s => specificPlaylist.SongsIDs.Contains(s.Id))
                .ToList();
            _allSongsFromPlaylist.OnNext(songsInPlaylist);
            currentlyLoadedPlaylist = playlistID;

            foreach (var item in songsInPlaylist)
            {
                Debug.WriteLine(item.Title);
            }
        }

    }

    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        throw new NotImplementedException();
    }
}
