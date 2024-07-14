
using System.Collections.Specialized;

namespace Dimmer.Utilities.Services;
public partial class PlayListService : ObservableObject, IPlayListService
{
    public IList<PlaylistModelView> AllPlaylists => AllPlaylistsSubject;
    
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> _allPlaylistsSubject;

    public IPlaylistManagementService PlaylistManagementService { get; }
    public ISongsManagementService SongsManagementService { get; }

    public IList<SongsModelView> SongsFromPlaylist => AllSongsFromPlaylist;
    [ObservableProperty]
    ObservableCollection<SongsModelView> _allSongsFromPlaylist;
    
    [ObservableProperty]
    string observableSelectedPlaylistName;
    public string SelectedPlaylistName => ObservableSelectedPlaylistName;

    public event NotifyCollectionChangedEventHandler AllPlaylistsChanged
    {
        add { _allPlaylistsSubject.CollectionChanged += value; }
        remove { _allPlaylistsSubject.CollectionChanged -= value; }
    }

    public PlayListService(IPlaylistManagementService playlistManagementService, ISongsManagementService songsManagementService)
    {
        PlaylistManagementService = playlistManagementService;
        SongsManagementService = songsManagementService;
        AllPlaylistsSubject = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);

    }


    public ObservableCollection<PlaylistModelView> UpdatePlaylistSongs()
    {
        AllPlaylistsSubject = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
        
        return AllPlaylistsSubject;
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
            ObservableSelectedPlaylistName = specificPlaylist.Name;
            IList<SongsModelView> songsInPlaylist = SongsManagementService.AllSongs
                .Where(s => specificPlaylist.SongsIDs.Contains(s.Id))
                .ToList();
            AllSongsFromPlaylist = new ObservableCollection<SongsModelView>(songsInPlaylist);
            currentlyLoadedPlaylist = playlistID;
        }

    }

    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    public void AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
        specificPlaylist?.SongsIDs.Add(song.Id);
    }

    public void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playlistName);
        specificPlaylist?.SongsIDs.Add(song.Id);
    }

    public void RemoveFromPlayListWithPlayListName(SongsModelView song, string playListName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playListName);
        specificPlaylist?.SongsIDs.Remove(song.Id);
        
    }

    public void RemoveFromPlayListWithPlayListID(SongsModelView song, ObjectId playListID)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playListID);
        specificPlaylist?.SongsIDs.Remove(song.Id);
    }
}
