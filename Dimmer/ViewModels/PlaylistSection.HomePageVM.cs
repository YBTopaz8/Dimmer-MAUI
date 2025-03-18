using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView> DisplayedPlaylists { get; set; } 
    [ObservableProperty]
    public partial string? SelectedPlaylistPageTitle { get; set; }

    [ObservableProperty]
    public partial PlaylistModelView SelectedPlaylistToOpenBtmSheet {get;set;} = null;
    

    [ObservableProperty]
    public partial PlaylistModelView SelectedPlaylist {get;set;}
    public void RefreshPlaylists()
    {
        DisplayedPlaylists?.Clear();
        PlaylistManagementService.GetPlaylists();
        DisplayedPlaylists = PlaylistManagementService.AllPlaylists;
    }
    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(string PlaylistID)//string playlistName)
    {
        if (PlayBackService.AllPlaylists is null)
        {
            return;
        }
        SelectedPlaylist = PlayBackService.AllPlaylists.FirstOrDefault(x => x.LocalDeviceId == PlaylistID)!;
        SelectedPlaylist.DisplayedSongsFromPlaylist?.Clear();
        SelectedPlaylist.DisplayedSongsFromPlaylist= PlayBackService.GetSongsFromPlaylistID(PlaylistID).OrderBy(x => x.Title).ToObservableCollection();
        
        SelectedPlaylistPageTitle = PlayBackService.SelectedPlaylistName;
        
#if ANDROID
        await Shell.Current.GoToAsync(nameof(SinglePlaylistPageM), true);
#endif
    }

    CancellationTokenSource cts = new();
    const string songAddedToPlaylistText = "Song Added to Playlist";
    const string songDeletedFromPlaylistText = "Song Removed from Playlist";
    const string PlaylistCreatedText = "Playlist Created Successfully!";
    const string PlaylistDeletedText = "Playlist Deleted Successfully!";
    const ToastDuration duration = ToastDuration.Short;

    
    public void UpSertPlayList(PlaylistModelView playlistModel, List<string>? songIDs = null, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        if (songIDs is null)
        {
            return;
        }
        SelectedPlaylist = PlayBackService.AllPlaylists!.First(x => x.LocalDeviceId == playlistModel.LocalDeviceId!);
        if (IsAddSong)
        {

            AddToPlaylist(playlistModel, songIDs);              
            //await toast.Show(cts.Token);
        }
        else if (IsRemoveSong)
        {
            RemoveFromPlaylist(playlistModel, songIDs);
            
        }
        else if (IsDeletePlaylist)
        {
            PlayBackService.AllPlaylists?.Remove(SelectedPlaylist);
            PlayBackService.DeletePlaylistThroughID(SelectedPlaylist.LocalDeviceId!);
            
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(PlaylistDeletedText, duration);
            //await toast.Show(cts.Token);
        }
    }
    public async Task LoadFirstPlaylist()
    {
        RefreshPlaylists();
        if (DisplayedPlaylists is not null && DisplayedPlaylists.Count > 0)
        {
            await OpenSpecificPlaylistPage(DisplayedPlaylists[0].LocalDeviceId!);
        }
    }
   

    [RelayCommand]
    public void DeletePlaylist()
    {
        PlayBackService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.LocalDeviceId);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(PlaylistDeletedText, duration);
        //await toast.Show(cts.Token);
    }

    [RelayCommand]
    void OpenPlaylistMenuBtmSheet(PlaylistModelView playlist)
    {
        SelectedPlaylistToOpenBtmSheet = playlist;        
    }

    [RelayCommand]
    void RenamePlaylist(string newPlaylistName)
    {
        //PlaylistManagementService.RenamePlaylist(SelectedPlaylistToOpenBtmSheet.LocalDeviceId, newPlaylistName);
        //HiDisplayedPlaylists = PlayListService.GetAllPlaylists();
    }

    
    public void AddToPlaylist(PlaylistModelView playlist, List<string?> songIDs)
    {
        songIDs = songIDs.ToList();

        if (!EnableContextMenuItems) return;
        
        foreach (var id in songIDs)
        {
            var songg = DisplayedSongs.First(x => x.LocalDeviceId == id);
            if (SelectedPlaylist.DisplayedSongsFromPlaylist.Contains(songg))
            {
                songIDs.Remove(id);
                continue;
            }
            SelectedPlaylist.DisplayedSongsFromPlaylist?.Add(songg);
        }
        PlayBackService.AddSongsToPlaylist(songIDs, playlist);
        
            RefreshPlaylists();
        
        GeneralStaticUtilities.ShowNotificationInternally($"Added {songIDs.Count} to Playlist: {playlist.Name}");
    }
    
    public void RemoveFromPlaylist(PlaylistModelView playlist, List<string>? songIDs=null)
    {
        songIDs ??= [MySelectedSong!.LocalDeviceId!];
        if (!EnableContextMenuItems) return;
        if (DisplayedPlaylists is null)
        {
            RefreshPlaylists();
        }
        foreach (var id in songIDs)
        {
            var songg = DisplayedSongs.First(x => x.LocalDeviceId == id);
            
            SelectedPlaylist.DisplayedSongsFromPlaylist?.Remove(songg);
        }
        PlayBackService.RemoveSongFromPlayListWithPlayListID(songIDs, playlist);
        
        GeneralStaticUtilities.ShowNotificationInternally($"Removed {songIDs.Count} to Playlist: {playlist.Name}");
    }

   

}