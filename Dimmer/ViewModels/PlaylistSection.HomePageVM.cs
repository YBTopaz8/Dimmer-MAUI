


namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
{
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> displayedPlaylists;
    [ObservableProperty]
    string selectedPlaylistPageTitle;

    [ObservableProperty]
    ObservableCollection<SongModelView> displayedSongsFromPlaylist;
    [ObservableProperty]
    PlaylistModelView selectedPlaylistToOpenBtmSheet;
    [ObservableProperty]
    SongModelView selectedSongToOpenBtmSheet;
    public void RefreshPlaylists()
    {
        DisplayedPlaylists?.Clear();
        DisplayedPlaylists = PlayBackService.GetAllPlaylists();
    }
    [ObservableProperty]
    PlaylistModelView selectedPlaylist;

    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)//string playlistName)
    {

        SelectedPlaylist = DisplayedPlaylists.FirstOrDefault(x => x.Id == PlaylistID)!;
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

    
    public async Task UpdatePlayList(SongModelView song, PlaylistModelView playlistModel =null, bool IsAddSong = false, bool IsRemoveSong = false, bool IsDeletePlaylist = false)
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
            await toast.Show(cts.Token);
        }
        else if (IsRemoveSong)
        {
            DisplayedSongsFromPlaylist.Remove(song);
            PlayBackService.RemoveSongFromPlayListWithPlayListID(song, SelectedPlaylist.Id);
            var toast = Toast.Make(songDeletedFromPlaylistText, duration);
            await toast.Show(cts.Token);
        }
        else if (IsDeletePlaylist)
        {
            PlayBackService.DeletePlaylistThroughID(SelectedPlaylist.Id);
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(PlaylistDeletedText, duration);
            await toast.Show(cts.Token);
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
            var newPlaylist = new PlaylistModelView()
            {
                Name = PlaylistName,
            };
            await UpdatePlayList(SelectedSongToOpenBtmSheet, newPlaylist, IsAddSong: true);
            DisplayedPlaylists = PlayBackService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            await toast.Show(cts.Token);
        }
    }

    [RelayCommand]
    public async Task DeletePlaylist()
    {
        PlayBackService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.Id);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(PlaylistDeletedText, duration);
        await toast.Show(cts.Token);
    }

    [RelayCommand]
    void OpenPlaylistMenuBtmSheet(PlaylistModelView playlist)
    {
        SelectedPlaylistToOpenBtmSheet = playlist;
        
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