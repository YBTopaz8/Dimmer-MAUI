namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView> DisplayedPlaylists { get; set; } 
    [ObservableProperty]
    public partial string? SelectedPlaylistPageTitle { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongsFromPlaylist{get;set;}
    [ObservableProperty]
    public partial PlaylistModelView SelectedPlaylistToOpenBtmSheet {get;set;} = null;
    [ObservableProperty]
    public partial SongModelView? MySelectedSong {get;set;} = null;

    [ObservableProperty]
    public partial PlaylistModelView SelectedPlaylist {get;set;}
    public void RefreshPlaylists()
    {
        DisplayedPlaylists?.Clear();
        DisplayedPlaylists = PlayBackService.GetAllPlaylists();
    }
    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(string PlaylistID)//string playlistName)
    {

        SelectedPlaylist = DisplayedPlaylists.FirstOrDefault(x => x.LocalDeviceId == PlaylistID)!;
        DisplayedSongsFromPlaylist?.Clear();
        DisplayedSongsFromPlaylist= PlayBackService.GetSongsFromPlaylistID(PlaylistID).OrderBy(x => x.Title).ToObservableCollection();
        
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

    
    public void UpdatePlayList(SongModelView song, PlaylistModelView playlistModel, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
    {
        if (song is null)
        {
            return;
        }
        if (IsAddSong)
        {
            PlayBackService.AddSongToPlayListWithPlayListID(song, playlistModel);
            if (CurrentPage == PageEnum.PlaylistsPage)
            {
                DisplayedSongsFromPlaylist.Add(song);

            }
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            //await toast.Show(cts.Token);
        }
        else if (IsRemoveSong)
        {
            DisplayedSongsFromPlaylist.Remove(song);
            SelectedPlaylist = DisplayedPlaylists.FirstOrDefault(x => x.LocalDeviceId == playlistModel.LocalDeviceId!);
            if (SelectedPlaylist is not null)
            {
                PlayBackService.RemoveSongFromPlayListWithPlayListID(song, SelectedPlaylist.LocalDeviceId!);
                var toast = Toast.Make(songDeletedFromPlaylistText, duration);
                //await toast.Show(cts.Token);
            }
        }
        else if (IsDeletePlaylist)
        {
            PlayBackService.DeletePlaylistThroughID(SelectedPlaylist.LocalDeviceId!);
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(PlaylistDeletedText, duration);
            //await toast.Show(cts.Token);
        }
        song.IsFavorite = !song.IsFavorite;
    }
    public void LoadFirstPlaylist()
    {
        RefreshPlaylists();
        if (DisplayedPlaylists is not null && DisplayedPlaylists.Count > 0)
        {
            GeneralStaticUtilities.RunFireAndForget(OpenSpecificPlaylistPage(DisplayedPlaylists[0].LocalDeviceId!));
        }
    }
    [RelayCommand]
    public void CreatePlaylistAndAddSong(string PlaylistName)
    {
        if (!string.IsNullOrEmpty(PlaylistName))
        {
            var newPlaylist = new PlaylistModelView()
            {
                Name = PlaylistName,
            };
            UpdatePlayList(MySelectedSong, newPlaylist, IsAddSong: true);
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            //await toast.Show(cts.Token);
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

    [RelayCommand]
    async Task AddToPlaylist()
    {
        if(!EnableContextMenuItems) return;
        if (DisplayedPlaylists is null)
        {
            RefreshPlaylists();
        }
        MySelectedSong = PickedSong;
        var allPlaylistNames = DisplayedPlaylists?.Select(x => x.Name).ToList();
        _ = await Shell.Current.ShowPopupAsync(new SongToPlaylistPopup(this, allPlaylistNames!));
        
    }

   

}