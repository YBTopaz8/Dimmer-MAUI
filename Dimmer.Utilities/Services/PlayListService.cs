
using System.Collections.Specialized;

namespace Dimmer.Utilities.Services;
public partial class PlayListService : ObservableObject, IPlayListService
{
    [ObservableProperty]
    public ObservableCollection<PlaylistModelView> allPlaylists;

    public IPlaylistManagementService PlaylistManagementService { get; }
    public ISongsManagementService SongsManagementService { get; }

    [ObservableProperty]
    public ObservableCollection<SongsModelView> songsFromPlaylist;
    
    
    [ObservableProperty]    
    string selectedPlaylistName;


    public PlayListService(IPlaylistManagementService playlistManagementService, ISongsManagementService songsManagementService)
    {
        PlaylistManagementService = playlistManagementService;
        SongsManagementService = songsManagementService;
        AllPlaylists = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
    }


    public ObservableCollection<PlaylistModelView> GetAllPlaylists()
    {
        AllPlaylists = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
        
        return AllPlaylists;
    }
    public void GetPlaylistDetails(ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    ObjectId currentlyLoadedPlaylist;// = ObjectId.Empty;


    public void GetSongsFromPlaylistID(ObjectId playlistID)
    {
        var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.Id == playlistID);

        if (specificPlaylist != null)
        {
            SelectedPlaylistName = specificPlaylist.Name;
            IList<SongsModelView> songsInPlaylist = SongsManagementService.AllSongs
                .Where(s => specificPlaylist.SongsIDs.Contains(s.Id))
                .ToList();
            SongsFromPlaylist = new ObservableCollection<SongsModelView>(songsInPlaylist);            
        }

    }

    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        throw new NotImplementedException();
    }

    public void AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
        if (specificPlaylist is null)
        {
            return;
        }
        specificPlaylist?.SongsIDs.Add(song.Id);
        specificPlaylist.TotalSongsCount += 1;
    }

    public void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playlistName);
        if (specificPlaylist is null)
        {
            return;
        }
        specificPlaylist?.SongsIDs.Add(song.Id);
        
        specificPlaylist.TotalSongsCount += 1;
    }

    public void RemoveFromPlayListWithPlayListName(SongsModelView song, string playListName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playListName);
        if (specificPlaylist is null)
        {
            return;
        }
        specificPlaylist?.SongsIDs.Remove(song.Id);
        specificPlaylist.TotalSongsCount -= 1;
    }

    public void RemoveFromPlayListWithPlayListID(SongsModelView song, ObjectId playListID)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playListID);
        if (specificPlaylist is null)
        {
            return;
        }
        specificPlaylist?.SongsIDs.Remove(song.Id);
        specificPlaylist.TotalSongsCount -= 1;
    }

    
}
