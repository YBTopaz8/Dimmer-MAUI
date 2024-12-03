using Hqub.Lastfm;
using Hqub.Lastfm.Cache;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    #region HomePage MultiSelect's Logic
    public bool EnableContextMenuItems = true;
    [ObservableProperty]
    ObservableCollection<SongModelView> multiSelectSongs;
    [ObservableProperty]
    string multiSelectText;

   

    [RelayCommand]
    async Task MultiSelectUtilClicked(int SelectedBtn)
    {
        switch (SelectedBtn)
        {   
            case 0: 
                await DeleteFile(TemporarilyPickedSong);
                break;
            case 1:
                break;
            case 2:
                break;
            default:
                break;
        }
    }
    #endregion

    [RelayCommand]
    void DummyFunc()
    {
        GetAllAlbums();
    }

    [RelayCommand]
    void BringAppToFront()
    {
#if WINDOWS
        MiniPlayBackControlNotif.BringAppToFront();
#endif
    }
    

    [RelayCommand]
    async Task ShowSleepTimerPopup()
    {
        await Shell.Current.ShowPopupAsync(new SleepTimerSelectionPopup(this));
    }
    private CancellationTokenSource? _sleepTimerCancellationTokenSource;

    [RelayCommand]
    async Task StartSleepTimer(double value)
    {
        // Convert value to milliseconds (e.g., value in minutes * 60 * 1000)
        var valueInMilliseconds = value * 60 * 1000;

        // Cancel any existing timer
        _sleepTimerCancellationTokenSource?.Cancel();

        // Create a new CancellationTokenSource
        _sleepTimerCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _sleepTimerCancellationTokenSource.Token;

        try
        {
            string min = value < 2 ? "Minute" : "Minutes";
            var toast = Toast.Make($"Started ! Song will pause after {value}{min}}}");
            await toast.Show(cts.Token);

            await Task.Delay((int)valueInMilliseconds, cancellationToken);

            // If the delay completed without cancellation, pause the song
            if (!cancellationToken.IsCancellationRequested && IsPlaying)
            {
                await PauseSong();
            }
        }
        catch (TaskCanceledException ex)
        {
            // Handle the cancellation (if needed but i'm not sure I will ngl but still)
            Debug.WriteLine(ex.Message);
        }
    }

    public async Task ExitingApp()
    {
#if WINDOWS && NET9_0
        if (IsPlaying)
        {
            await this.PauseSong();
        }
#endif
        if (TemporarilyPickedSong is not null)
        {
            AppSettingsService.LastPlayedSongPositionPref.SetLastPosition(CurrentPositionPercentage);
            AppSettingsService.LastPlayedSongSettingPreference.SetLastPlayedSong(TemporarilyPickedSong.LocalDeviceId);
        }
    }
    [ObservableProperty]
    bool iIsMultiSelectOn;
    [RelayCommand]
    async Task DeleteFile(SongModelView? song)
    {

        switch (IsMultiSelectOn)
        {
            case true:
                bool result = await Shell.Current.DisplayAlert("Delete Song", $"Are you sure you want to Delete Selections " +
                   $"from your Device?", "Yes", "No");
                if (result)
                {
                    PlatSpecificUtils.MultiDeleteSongFiles(MultiSelectSongs);
                    
                    // Loop through all songs in MultiSelectSongs and remove them from DisplayedSongs if they exist
                    foreach (var selectedSong in MultiSelectSongs)
                    {
                        DisplayedSongs?.Remove(selectedSong);
                    }
                    if (MultiSelectSongs.Contains(TemporarilyPickedSong!))
                    {
                        await PlayNextSong();
                    }
                    await PlayBackService.MultiDeleteSongFromHomePage(MultiSelectSongs);

                    MultiSelectSongs.Clear();
                    MultiSelectText = $"{MultiSelectSongs.Count} Song{(MultiSelectSongs.Count > 1 ? "s" : "")} Selected";
                }
                break;

            case false:

                if (!EnableContextMenuItems)
                    break;
                song ??= SelectedSongToOpenBtmSheet;
                bool res = await Shell.Current.DisplayAlert("Delete File", $"Are you sure you want to Delete Song: {song.Title} " +
                    $"by {song.ArtistName}?", "Yes", "No");
                
                if(res)
                {
                    PlatSpecificUtils.DeleteSongFile(song);
                    if (song == TemporarilyPickedSong)
                    {
                        await PlayNextSong();
                    }
                    DisplayedSongs?.Remove(song);
                    PlayBackService.DeleteSongFromHomePage(song);
                }
                break;

        }
    }

    [RelayCommand]
    void OpenSongFolder() //SongModel SelectedSong)
    {
        if(!EnableContextMenuItems) return;
#if WINDOWS
        string filePath = string.Empty;
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            filePath = PickedSong.FilePath;
        }
        else
        {
            filePath = SelectedSongToOpenBtmSheet.FilePath;
        }
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
        {
            // Open File Explorer and select the file
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
#endif
    }

    [RelayCommand]
    async Task OpenViewSongDetailsPopup()
    {
        
        await Shell.Current.ShowPopupAsync(new ViewSongMetadataPopupView(this));
    }

    [RelayCommand]
    async Task OpenEditSongPopup(SongModelView? song)
    {
        await Shell.Current.ShowPopupAsync(new EditSongPopup(this));
    }


#if WINDOWS
    public AppWindowPresenter AppWinPresenter { get; set; }
#endif
    [ObservableProperty]
    bool isStickToTop = false;
    [RelayCommand]
    void ToggleStickToTop()
    {       
        IsStickToTop = !IsStickToTop;

#if WINDOWS
        PlatSpecificUtils.ToggleWindowAlwaysOnTop(IsStickToTop, AppWinPresenter);
#endif
    }
    public bool IsSleek = false;
    [RelayCommand]
    void ToggleSleekMode(bool isSleek)
    {
        IsSleek = !IsSleek;
#if WINDOWS
        PlatSpecificUtils.ToggleFullScreenMode(IsSleek, AppWinPresenter);
#endif
    }

    #region Search Song On... ContextMenu Options

    [RelayCommand]
    async Task CntxtMenuSearch(int param)
    {
        if (SelectedSongToOpenBtmSheet is null)
        {
            return; // show error msg here or cue
        }
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong!;
        }
        switch (param)
        {
            case 0:
                await OpenGoogleSearch();
                break;
            case 1:
                await OpenYouTubeSearchResults();
                break;
            case 2:
                await OpenSpotifySearchResults();
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                    break;
            default:
                break;
        }
    }
    //I want to use SelectedSongToOpenBtmSheet, which has Title, ArtistName and AlbumName
    async Task OpenGoogleSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} - {SelectedSongToOpenBtmSheet.ArtistName}";
            //if (!string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
            //{
            //    query += $" {SelectedSongToOpenBtmSheet.AlbumName}";
            //}
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenGoogleLyricsSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} by {SelectedSongToOpenBtmSheet.ArtistName} lyrics";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenYouTubeSearchResults()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName} {SelectedSongToOpenBtmSheet.Title}";
            string searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    async Task OpenGoogleArtistSearch()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenGoogleAlbumSearch()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
        {
            string query = $"{SelectedSongToOpenBtmSheet.AlbumName} {SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    async Task OpenYouTubeArtistProfile()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifyArtistProfile()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.ArtistName))
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.ArtistName)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifyAlbumProfile()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.AlbumName)} {Uri.EscapeDataString(SelectedSongToOpenBtmSheet.AlbumName)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifySearchResults()
    {
        if (SelectedSongToOpenBtmSheet != null)
        {
            string query = $"{SelectedSongToOpenBtmSheet.Title} {SelectedSongToOpenBtmSheet.ArtistName}";
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(query)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }
    #endregion

    [ObservableProperty]
    bool isRetrievingCovers = true;
    [RelayCommand]
    async Task ReloadCoverImageAsync()
    {
        IsRetrievingCovers = true;
        // Run the work on a background thread
        await Task.Run(() =>
        {
            // Perform the operations on a background thread
            DisplayedSongs = PlaybackUtilsService.CheckCoverImage(DisplayedSongs);

            // Call the non-awaitable method directly
            SongsMgtService.AddSongBatchAsync(DisplayedSongs); // No await needed
        });

        // Return to the main thread to show the alert
        await Shell.Current.DisplayAlert("Download Complete!", "All Downloaded", "OK");
        IsRetrievingCovers = false;
    }



    [ObservableProperty]
    bool isTemporarySongNull = true;


    [ObservableProperty]
    bool isOnSearchMode = false;


    [RelayCommand]
    async Task DownloadAlbumImage(AlbumModelView album)
    {
      var firstSongOfSpectifAlbum = AllArtistsAlbumSongs.FirstOrDefault()!;
      SelectedAlbumOnArtistPage.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(firstSongOfSpectifAlbum.Title,firstSongOfSpectifAlbum.ArtistName!, firstSongOfSpectifAlbum.AlbumName!);
    }

    [ObservableProperty]
    string shareImgPath = string.Empty;
    
    public Color[] ShareColors { get; } = new Color[]{
            Color.FromArgb("#FF0000"),
            Color.FromArgb("#2365BD"),
            Color.FromArgb("#4C342F"),
            Color.FromArgb("#661D98"),
            };

    public FlyoutItem AppFlyout { get; set; }
    [RelayCommand]
    public void SwitchTab(int index)
    {
        switch (index)
        {
            case 0:
                AppFlyout.CurrentItem = AppFlyout.Items[0];
                break;
            case 1:
                AppFlyout.CurrentItem = AppFlyout.Items[1];
                break;
            case 2:
                AppFlyout.CurrentItem = AppFlyout.Items[2];
                break;
            case 3:
                AppFlyout.CurrentItem = AppFlyout.Items[3];
                break;
            case 4:
                AppFlyout.CurrentItem = AppFlyout.Items[4];
                break;
            default:
                break;
        }
    }

    public void ToggleFlyout(bool isOpenFlyout=false)
    {
        IsFlyOutPaneOpen = false;
        if (Shell.Current == null)
        {
            return;
        }
        if (isOpenFlyout)
        {
            if(Shell.Current.FlyoutBehavior != FlyoutBehavior.Locked)
            {
                IsFlyOutPaneOpen = true;
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Locked;
            }
        }
        else
        {
            if (Shell.Current.FlyoutBehavior != FlyoutBehavior.Flyout)
            {
                IsFlyOutPaneOpen = false;
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
                _ = Task.Delay(500);
                Shell.Current.FlyoutIsPresented = true;
            }
        }
    }

    [RelayCommand]
    void LogOut()
    {
        ParseClient.Instance.LogOut();
    }

    [ObservableProperty]
    bool isSyncingSongs = false;    

    [ObservableProperty]
    bool isLoggedIn = false;    
    public async Task FullSync()
    {
        SongsMgtService.CurrentUserOnline = this.CurrentUserOnline;
        IsSyncingSongs = true;
        await SongsMgtService.SendAllDataToServerAsInitialSync();
        await SongsMgtService.GetAllDataFromOnlineAsync();

        SyncRefresh();

        IsSyncingSongs = false;

        await Shell.Current.DisplayAlert("Success!", "Syncing Complete", "OK");
    }

    partial void OnCurrentUserOnlineChanged(ParseUser? oldValue, ParseUser newValue)
    {
        if (newValue is not null && newValue.IsAuthenticated)
        {
            SongsMgtService.UpdateUserLoginDetails(newValue);
            IsLoggedIn = true;
        }
        else
        {
            IsLoggedIn = false; //do more here 
        }
    }

   
}