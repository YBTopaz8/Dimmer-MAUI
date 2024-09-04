


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
    public int CurrentQueue=0;

    public void RefreshPlaylists()
    {
        DisplayedPlaylists.Clear();
        DisplayedPlaylists = PlayBackUtilsService.GetAllPlaylists();
    }
    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)//string playlistName)
    {
        PlayBackUtilsService.GetSongsFromPlaylistID(PlaylistID);
        //PlayListService.GetSongsFromPlaylistID
        
        SelectedPlaylistPageTitle = PlayBackUtilsService.SelectedPlaylistName;
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
        PlayBackUtilsService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, playlistName);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(songAddedToPlaylistText, duration);
        await toast.Show(cts.Token);
    }
    public async void LoadFirstPlaylist()
    {
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
            PlayBackUtilsService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, PlaylistName);
            DisplayedPlaylists = PlayBackUtilsService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            await toast.Show(cts.Token);
        }
    }

    [RelayCommand]
    public async Task DeletePlaylist()
    {
        await btmSheet.DismissAsync();
        PlayBackUtilsService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.Id);
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
        SelectedSongToOpenBtmSheet = PickedSong;
        var allPlaylistNames = DisplayedPlaylists.Select(x => x.Name).ToList();
        _ = await Shell.Current.ShowPopupAsync(new SongToPlaylistPopup(this, allPlaylistNames));
        
    }

   

}
