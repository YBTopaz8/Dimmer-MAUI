namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{

    #region HomePage MultiSelect's Logic
    public bool EnableContextMenuItems = true;
    [ObservableProperty]
    ObservableCollection<SongsModelView> multiSelectSongs;
    [ObservableProperty]
    string multiSelectText;

    [ObservableProperty]
    bool canMultiDeleteItems;
    public void HandleMultiSelect(CollectionView sender, SelectionChangedEventArgs? e=null)
    {
        MultiSelectSongs = sender.SelectedItems.Cast<SongsModelView>().ToObservableCollection();
        if (MultiSelectSongs.Count >= 1)
        {
            CanMultiDeleteItems = true;
        }
        else
        {
            CanMultiDeleteItems = false;
        }
        MultiSelectText = $"{MultiSelectSongs.Count} Song{(MultiSelectSongs.Count > 1 ? "s" : "")}/{DisplayedSongs.Count} Selected";
        Debug.WriteLine(MultiSelectSongs.Count);
    }

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
#if WINDOWS
        if (IsPlaying)
        {
            await this.PauseSong();
        }
#endif
        if (TemporarilyPickedSong is not null)
        {
            AppSettingsService.LastPlayedSongPositionPref.SetLastPosition(CurrentPositionPercentage);
            AppSettingsService.LastPlayedSongSettingPreference.SetLastPlayedSong(TemporarilyPickedSong.Id);
        }
    }

    [RelayCommand]
    void ToggleDiscordRPC(bool isChecked)
    {
        if (isChecked)
        {
            AppSettingsService.DiscordRPCPreference.ToggleDiscordRPC(isChecked);
            DiscordRPC.Initialize();
            if (IsPlaying)
            {
                DiscordRPC.UpdatePresence(TemporarilyPickedSong,
                    TimeSpan.FromSeconds(TemporarilyPickedSong.DurationInSeconds), TimeSpan.FromSeconds(CurrentPositionInSeconds));
            }
        }
    }

    

    [RelayCommand]
    async Task DeleteFile(SongsModelView? song)
    {

        switch (CanMultiDeleteItems)
        {
            case true:
                bool result = await Shell.Current.DisplayAlert("Delete Song", $"Are you sure you want to Delete Selections " +
                   $"from your Device?", "Yes", "No");
                if (result)
                {
                    await PlatSpecificUtils.MultiDeleteSongFiles(MultiSelectSongs);
                    
                    // Loop through all songs in MultiSelectSongs and remove them from DisplayedSongs if they exist
                    foreach (var selectedSong in MultiSelectSongs)
                    {
                        DisplayedSongs.Remove(selectedSong);
                    }
                    if (MultiSelectSongs.Contains(TemporarilyPickedSong))
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
                    DisplayedSongs.Remove(song);
                    await PlayBackService.DeleteSongFromHomePage(song.Id);
                }
                break;

        }
    }

    [RelayCommand]
    void OpenSongFolder() //SongsModel SelectedSong)
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
    async Task OpenEditSongPopup(SongsModelView? song)
    {
        await Shell.Current.ShowPopupAsync(new EditSongPopup(this));
    }


#if WINDOWS
    public AppWindowPresenter AppWinPresenter { get; set; }
#endif

    [RelayCommand]
    void ToggleStickToTop(bool isStickToTop)
    {
#if WINDOWS
        PlatSpecificUtils.ToggleWindowAlwaysOnTop(isStickToTop, AppWinPresenter);
#endif
    }

    #region Search Song On... ContextMenu Options

    [RelayCommand]
    async Task CntxtMenuSearch(int param)
    {
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
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
        if (SelectedSongToOpenBtmSheet != null)
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.ArtistName)}";
            await Launcher.OpenAsync(new Uri(searchUrl));
        }
    }

    async Task OpenSpotifyAlbumProfile()
    {
        if (SelectedSongToOpenBtmSheet != null && !string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.AlbumName))
        {
            string searchUrl = $"https://open.spotify.com/search/{Uri.EscapeDataString(SelectedSongToOpenBtmSheet.AlbumName)} {Uri.EscapeDataString(SelectedSongToOpenBtmSheet.ArtistName)}";
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

    [RelayCommand]
    void ReloadCoverImage()
    {
        DisplayedSongs= PlaybackUtilsService.CheckCoverImage(DisplayedSongs);
        SongsMgtService.AddSongBatchAsync(DisplayedSongs);
    }
}