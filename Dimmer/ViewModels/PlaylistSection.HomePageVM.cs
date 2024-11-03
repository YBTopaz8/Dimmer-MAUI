


namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> displayedPlaylists;
    [ObservableProperty]
    string selectedPlaylistPageTitle;

    [ObservableProperty]
    ObservableCollection<SongsModelView> displayedSongsFromPlaylist;
    [ObservableProperty]
    PlaylistModelView selectedPlaylistToOpenBtmSheet;
    [ObservableProperty]
    SongsModelView selectedSongToOpenBtmSheet;
    PlaylistMenuBtmSheet btmSheet { get; set; }


    


    public void RefreshPlaylists()
    {
        DisplayedPlaylists?.Clear();
        DisplayedPlaylists = PlayBackService.GetAllPlaylists();
    }
    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)//string playlistName)
    {
        DisplayedSongsFromPlaylist?.Clear();
        DisplayedSongsFromPlaylist= PlayBackService.GetSongsFromPlaylistID(PlaylistID).ToObservableCollection();
        //PlayListService.GetSongsFromPlaylistID
        
        SelectedPlaylistPageTitle = PlayBackService.SelectedPlaylistName;
        //PlayBackService.UpdateCurrentQueue(, 1);
        //PlayListService.
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

    [RelayCommand]
    public async Task AddSongToSpecifcPlaylist(string playlistName)
    {
        PlayBackService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, playlistName);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(songAddedToPlaylistText, duration);
        await toast.Show(cts.Token);
    }
    [RelayCommand]
    async Task UpdateSongInFavoritePlaylist(SongsModelView song)
    {
        if (song is null)
        {
            return;
        }
        PlayBackService.UpdateSongToFavoritesPlayList(song);
        if (song.IsFavorite)
        {
            PlayBackService.AddSongToPlayListWithPlayListName(song, "Favorites");
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            HapticFeedback.Perform(HapticFeedbackType.Click);
            await toast.Show(cts.Token);
        }
        else
        {
            PlayBackService.RemoveSongFromPlayListWithPlayListName(song, "Favorites");
        }
        song.IsFavorite = !song.IsFavorite;
    }
    public async void LoadFirstPlaylist()
    {
        RefreshPlaylists();
        if (DisplayedPlaylists is not null && DisplayedPlaylists.Count > 0)
        {
            await OpenSpecificPlaylistPage(DisplayedPlaylists[0].Id);
        }
    }
    [RelayCommand]
    public async Task CreatePlaylistAndAddSong(string PlaylistName)
    {
        if (!string.IsNullOrEmpty(PlaylistName))
        {
            PlayBackService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, PlaylistName);
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            await toast.Show(cts.Token);
        }
    }

    [RelayCommand]
    public async Task DeletePlaylist()
    {
        await btmSheet.DismissAsync();
        PlayBackService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.Id);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(PlaylistDeletedText, duration);
        await toast.Show(cts.Token);
    }

    [RelayCommand]
    async Task OpenPlaylistMenuBtmSheet(PlaylistModelView playlist)
    {
        SelectedPlaylistToOpenBtmSheet = playlist;
        btmSheet = new PlaylistMenuBtmSheet(this);
        await btmSheet.ShowAsync();
    }

    [RelayCommand]
    void RenamePlaylist(string newPlaylistName)
    {
        //PlaylistManagementService.RenamePlaylist(SelectedPlaylistToOpenBtmSheet.Id, newPlaylistName);
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
        SelectedSongToOpenBtmSheet = PickedSong;
        var allPlaylistNames = DisplayedPlaylists.Select(x => x.Name).ToList();
        _ = await Shell.Current.ShowPopupAsync(new SongToPlaylistPopup(this, allPlaylistNames));
        
    }

   

}