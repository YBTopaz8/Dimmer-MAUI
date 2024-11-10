using DevExpress.Maui.Controls;
using Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Models;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
    
    [ObservableProperty]
    SongsModelView pickedSong; // I use this a lot with the collection view, mostly to scroll

    [ObservableProperty]
    SongsModelView temporarilyPickedSong;

    [ObservableProperty]
    double currentPositionPercentage;
    [ObservableProperty]
    double currentPositionInSeconds = 0;

    [ObservableProperty]
    ObservableCollection<SongsModelView> displayedSongs;

    [ObservableProperty]
    ObservableCollection<SongsModelView> prevCurrNextSongsCollection;

    SortingEnum CurrentSortingOption;
    [ObservableProperty]
    int totalNumberOfSongs;
    [ObservableProperty]
    string totalSongsSize;
    [ObservableProperty]
    string totalSongsDuration;

    [ObservableProperty]
    ObservableCollection<LyricPhraseModel>? synchronizedLyrics;
    [ObservableProperty]
    LyricPhraseModel? currentLyricPhrase;

    [ObservableProperty]
    int loadingSongsProgress;

    [ObservableProperty]
    double volumeSliderValue = 1;

    [ObservableProperty]
    bool isShuffleOn;
    [ObservableProperty]
    int currentRepeatMode;
    [ObservableProperty]
    bool isDRPCEnabled;
    IFolderPicker folderPicker { get; }
    IFileSaver FileSaver { get; }
    IPlaybackUtilsService PlayBackService { get; }
    ILyricsService LyricsManagerService { get; }
    public ISongsManagementService SongsMgtService { get; }
    public IArtistsManagementService ArtistMgtService { get; }

    public List<AlbumArtistSongLink> AllLinks { get; }

    [ObservableProperty]
    string unSyncedLyrics;
    [ObservableProperty]
    string localFilePath;

    public AppState CurrentAppState = AppState.OnForeGround;

    [ObservableProperty]
    int currentQueue = 0;
    public HomePageVM(IPlaybackUtilsService PlaybackManagerService, IFolderPicker folderPickerService, IFileSaver fileSaver,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService, IArtistsManagementService artistMgtService
                      )
    {
        this.folderPicker = folderPickerService;
        FileSaver = fileSaver;
        PlayBackService = PlaybackManagerService;
        LyricsManagerService = lyricsService;
        SongsMgtService = songsMgtService;
        ArtistMgtService = artistMgtService;
        CurrentSortingOption = AppSettingsService.SortingModePreference.GetSortingPref();

        SubscribeToPlayerStateChanges();
        SubscribetoDisplayedSongsChanges();
        
        SubscribeToCurrentSongPosition();
        SubscribeToPlaylistChanges();
        SubscribeToBackEndQChanges();

        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        LoadSongCoverImage();
        IsPlaying = false;
        //DisplayedPlaylists = PlayBackService.AllPlaylists;
        TotalSongsDuration = PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;
        IsPlaying = false;
        ToggleShuffleState();
        ToggleRepeatMode();
        FolderPaths = AppSettingsService.MusicFoldersPreference.GetMusicFolders().ToObservableCollection();
        IsDRPCEnabled = AppSettingsService.DiscordRPCPreference.IsDiscordRPCEnabled;
        //AppSettingsService.MusicFoldersPreference.ClearListOfFolders();
        GetAllArtists();
        GetAllAlbums();
        RefreshPlaylists();

        AllLinks = SongsMgtService.AllLinks.ToList();
    }

    void DoRefreshDependingOnPage()
    {
        LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();
        switch (CurrentPage)
        {
            case PageEnum.MainPage:
                
                break;
            case PageEnum.NowPlayingPage:
                switch (CurrentViewIndex)
                {
                    case 0:
                        break;
                        
                    case 1:
                        IsFetching = false;
                        break;
                        
                    case 2:
                        RefreshStatView();
                        break;
                    default:
                        break;
                }
                break;
            case PageEnum.PlaylistsPage:
                break;
            case PageEnum.FullStatsPage:
                ShowGeneralTopXSongs();
                break;
            case PageEnum.AllAlbumsPage:
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
    }
    [ObservableProperty]
    bool isMultiSelectOn;
    CollectionView PageCV { get; set; }
    public void AssignCV(CollectionView cv)
    {
        PageCV = cv;
    }
    void SubscribeToPlayerStateChanges()
    {
        if (_playerStateSubscription != null)
            return; // Already subscribed

        _playerStateSubscription = PlayBackService.PlayerState
            .DistinctUntilChanged()
            .Subscribe(async state =>
            {
                
                if (TemporarilyPickedSong is not null)
                {
                    TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

                    switch (state)
                    {
                        case MediaPlayerState.Playing:

                            if (PlayBackService.CurrentlyPlayingSong is null)
                                break;

                            TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
                            
                            if (PickedSong is not null)
                                PickedSong.IsPlaying = false;

                            PickedSong = TemporarilyPickedSong;
                            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;

                            TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                            PickedSong.IsCurrentPlayingHighlight = true;
                            
                            AllSyncLyrics = null;
                            splittedLyricsLines = null;
                            TemporarilyPickedSong.IsPlaying = true;
                            PickedSong.IsPlaying = true;
                            IsPlaying = true;

                            PlayPauseIcon = MaterialRounded.Pause;
                            CurrentLyricPhrase = new LyricPhraseModel() { Text = "" };
                            DoRefreshDependingOnPage();
                            
                            CurrentRepeatCount = PlayBackService.CurrentRepeatCount;
                            
                            await FetchSongCoverImage();

                            break;
                        case MediaPlayerState.Paused:
                            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
                            PickedSong ??= TemporarilyPickedSong;
                            
                            IsPlaying = false;
                            PlayPauseIcon = MaterialRounded.Play_arrow;
                            break;
                        case MediaPlayerState.Stopped:
                            //PickedSong = "Stopped";
                            break;
                        case MediaPlayerState.LoadingSongs:
                            LoadingSongsProgress = PlayBackService.LoadingSongsProgressPercentage;
                            break;
                        case MediaPlayerState.ShowPlayBtn:
                            PlayPauseIcon = MaterialRounded.Play_arrow;
                            IsPlaying = false;
                            break;
                        case MediaPlayerState.ShowPauseBtn:
                            IsPlaying = true;
                            PlayPauseIcon = MaterialRounded.Pause;
                            break;
                        default:
                            break;
                    }
                }
            });

    }

    public async void LoadLocalSongFromOutSideApp(List<string> filePath)
    {
        CurrentQueue = 2;
        await PlayBackService.PlaySelectedSongsOutsideAppAsync(filePath);
    }

    [ObservableProperty]
    bool isShellLoadingPage = false;
    [ObservableProperty]
    bool isViewingDifferentSong = false;
    public async Task NavToNowPlayingPage()
    {
        IsShellLoadingPage = true;
        CurrentViewIndex = 0;
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(SingleSongShellD));
#elif ANDROID
        var currentPage = Shell.Current.CurrentPage;

        if (currentPage.GetType() != typeof(SingleSongShell))
        {
            await Shell.Current.GoToAsync(nameof(SingleSongShell), true);
        }
#endif
        if (TemporarilyPickedSong is not null)
        {


            if (SelectedSongToOpenBtmSheet != TemporarilyPickedSong)
            {
                IsViewingDifferentSong = true;
                SynchronizedLyrics?.Clear();
                SynchronizedLyrics = LyricsManagerService.GetSpecificSongLyrics(SelectedSongToOpenBtmSheet).ToObservableCollection();
            }
            SongPickedForStats ??= new()
                {
                    Song = SelectedSongToOpenBtmSheet
                };
        }
    }

    public async Task<string> AfterSingleSongShellAppeared()
    {
        if (SelectedSongToOpenBtmSheet is null)
        {
            return string.Empty;
        }
        IsShellLoadingPage = false;
        (SelectedSongToOpenBtmSheet.HasSyncedLyrics, SelectedSongToOpenBtmSheet.SyncLyrics) = LyricsService.HasLyrics(SelectedSongToOpenBtmSheet);
        CurrentPage = PageEnum.NowPlayingPage;
        if (!string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.CoverImagePath) && !File.Exists(SelectedSongToOpenBtmSheet.CoverImagePath))
        {
            var coverImg = await LyricsManagerService
                .FetchAndDownloadCoverImage(SelectedSongToOpenBtmSheet.Title, SelectedSongToOpenBtmSheet.ArtistName, SelectedSongToOpenBtmSheet.AlbumName, SelectedSongToOpenBtmSheet);
            DisplayedSongs
                .FirstOrDefault(x => x.Id == SelectedSongToOpenBtmSheet.Id).CoverImagePath = coverImg;
            return coverImg;
        }
        return string.Empty;
    }

    #region Loadings Region

    [ObservableProperty]
    bool isLoadingSongs;

    [ObservableProperty]
    ObservableCollection<string> folderPaths;
    [RelayCommand]
    async Task SelectSongFromFolder()
    {
        //bool res = await Shell.Current.DisplayAlert("Select Song", "Sure?", "Yes", "No");
        //if (!res)
        //{
        //    return;
        //}

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;


#if WINDOWS || ANDROID
        var res = await FolderPicker.Default.PickAsync(token);

        if (res.Folder is null)
        {
            return;
        }
        var folder = res.Folder?.Path;
#elif IOS || MACCATALYST
        string folder = null;
#endif

        //var FolderName = Path.GetFileName(folder);
        FolderPaths.Add(folder);

        FullFolderPaths.Add(folder);

        AppSettingsService.MusicFoldersPreference.AddMusicFolder(FullFolderPaths);
        //await LoadSongsFromFolders();//FullFolderPaths);
    }

    List<string> FullFolderPaths = new();

    [RelayCommand]
    private async Task LoadSongsFromFolders()
    {
        try
        {

            DeviceDisplay.Current.KeepScreenOn = true;
            IsLoadingSongs = true;
            if (FolderPaths is null)
            {
                await Shell.Current.DisplayAlert("Error !", "No Paths to load", "OK");
                IsLoadingSongs = false;
                return;
            }
            bool loadSongsResult = await PlayBackService.LoadSongsFromFolderAsync(FolderPaths.ToList());
            if (loadSongsResult)
            {
                DisplayedSongs?.Clear();
                DisplayedSongs = SongsMgtService.AllSongs.ToObservableCollection();
                Debug.WriteLine("Songs Loaded Successfully");
            }
            else
            {
                Debug.WriteLine("No Songs Found");
            }
            IsLoadingSongs = false;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error During Scanning",ex.Message, "Ok");
        }
        finally
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
    }

    public PageEnum CurrentPage;
    #endregion

    #region Playback Control Region

    public List<SongsModelView> filteredSongs = new();
    [RelayCommand]
    //void PlaySong(SongsModelView? SelectedSong = null)
    public async Task PlaySong(SongsModelView? SelectedSong = null)
    {
        TemporarilyPickedSong ??= SelectedSong!;
        TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        SelectedSong.IsCurrentPlayingHighlight = false;
        if (SelectedSong is not null)
        {
            SelectedSongToOpenBtmSheet = SelectedSong;

            CurrentQueue = 0;
            if (CurrentPage == PageEnum.PlaylistsPage)
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: DisplayedSongsFromPlaylist, repeatMode: CurrentRepeatMode, repeatMaxCount: CurrentRepeatMaxCount);
                return;
            }
            if (CurrentPage == PageEnum.FullStatsPage)
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: TopTenPlayedSongs.Select(x => x.Song).ToObservableCollection());
                //ShowGeneralTopXSongs();
                return;
            }
            if (CurrentPage == PageEnum.SpecificAlbumPage || CurrentPage == PageEnum.AllAlbumsPage && SelectedSong != null)
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: AllArtistsAlbumSongs);
                return;
            }

            if (CurrentQueue == 1)
            {
                await PlayBackService.PlaySongAsync(SelectedSong!, CurrentQueue: CurrentQueue, SecQueueSongs: AllArtistsAlbumSongs);
                return;
            }
            if (IsOnSearchMode)
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue, SecQueueSongs: filteredSongs.ToObservableCollection());
            }
            else
            {
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue);
            }
        }
        else
        {
            await PlayBackService.PlaySongAsync(PickedSong, CurrentQueue, repeatMaxCount: CurrentRepeatMaxCount, repeatMode: CurrentRepeatMode);
            return;
        }

    }

    [RelayCommand]
    public async Task PauseSong()
    {
        await PlayBackService.PauseResumeSongAsync(CurrentPositionInSeconds, true);
    }
    
    [RelayCommand]
    public async Task ResumeSong()
    {
        await PlayBackService.PauseResumeSongAsync(CurrentPositionInSeconds);
    }
    

    [RelayCommand]
    async Task StopSong()
    {
        await PlayBackService.StopSongAsync();
    }

    [RelayCommand]
    async Task PlayNextSong()
    {
        TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        await PlayBackService.PlayNextSongAsync();
    }

    [RelayCommand]
    async Task PlayPreviousSong()
    {
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        await PlayBackService.PlayPreviousSongAsync();
    }

    [RelayCommand]
    void DecreaseVolume()
    {
        PlayBackService.DecreaseVolume();
        VolumeSliderValue -= 0.2;
    }
    [RelayCommand]
    void IncreaseVolume()
    {
        PlayBackService.IncreaseVolume();
        VolumeSliderValue += 0.2;
    }


    public void SeekSongPosition(LyricPhraseModel? lryPhrase = null)
    {
        if (lryPhrase is not null)
        {

            CurrentPositionInSeconds = lryPhrase.TimeStampMs * 0.001;
            PlayBackService.SetSongPosition(CurrentPositionInSeconds);
            return;
        }
        if (TemporarilyPickedSong is null)
            return;
        CurrentPositionInSeconds = CurrentPositionPercentage * TemporarilyPickedSong.DurationInSeconds;
        PlayBackService.SetSongPosition(CurrentPositionInSeconds);
    }

    [RelayCommand]
    void ChangeVolume()
    {
        PlayBackService.ChangeVolume(VolumeSliderValue);
    }

    [ObservableProperty]
    string shuffleOnOffImage = MaterialRounded.Shuffle;

    [ObservableProperty]
    string repeatModeImage = MaterialRounded.Repeat;
    [RelayCommand]
    void ToggleRepeatMode(bool IsCalledByUI = false)
    {
        CurrentRepeatMode = PlayBackService.CurrentRepeatMode;
        if (IsCalledByUI)
        {
            CurrentRepeatMode = PlayBackService.ToggleRepeatMode();
        }

        switch (CurrentRepeatMode)
        {
            case 1:
                RepeatModeImage = MaterialRounded.Repeat_on;
                break;
            case 2:
            case 4:
                RepeatModeImage = MaterialRounded.Repeat_one_on;
                break;
            case 0:
                RepeatModeImage = MaterialRounded.Repeat;
                break;
            default:
                break;
        }

    }

    [RelayCommand]
    void ToggleShuffleState(bool IsCalledByUI = false)
    {
        IsShuffleOn = PlayBackService.IsShuffleOn;
        if (IsCalledByUI)
        {
            IsShuffleOn = !IsShuffleOn;
            PlayBackService.ToggleShuffle(IsShuffleOn);
        }
        if (IsShuffleOn)
        {
            ShuffleOnOffImage = MaterialRounded.Shuffle_on;
        }
        else
        {
            ShuffleOnOffImage = MaterialRounded.Shuffle;
        }
    }
    #endregion
    [ObservableProperty]
    ObservableCollection<string> searchFilters = new([ "Artist", "Album","Genre", "Rating"]);
    [ObservableProperty]
    int achievement=0;
    
    [ObservableProperty]
    string searchText;
    
    public void SearchSong(List<string> SelectedFilters)
    {
        PlayBackService.SearchSong(SearchText, SelectedFilters, Achievement);
        TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
    }
  
    void ReloadSizeAndDuration()
    {
        TotalSongsDuration = PlayBackService.TotalSongsDuration;
        TotalSongsSize = PlayBackService.TotalSongsSizes;
    }

    [RelayCommand]
    async Task UpdateSongToDB(SongsModelView song)
    {
        var res = await Shell.Current.DisplayAlert("Confirm Action", "Confirm Update?", "Yes", "Cancel");
        if (res)
        {
            if(SongsMgtService.UpdateSongDetails(song))
            {
                await Shell.Current.DisplayAlert("Sucess !", "Song Updated!", "Ok");
            }    
        }
    }

    [ObservableProperty]
    ObservableCollection<SongsModelView> recentlyAddedSongs;
    public void LoadSongCoverImage()
    {

        RecentlyAddedSongs = GetXRecentlyAddedSongs(DisplayedSongs);

        if (DisplayedSongs is not null && DisplayedSongs.Count > 0)
        {
            LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();
        }


        if (TemporarilyPickedSong is not null)
        {
            TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
            PickedSong = TemporarilyPickedSong;
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
            CurrentPositionPercentage = AppSettingsService.LastPlayedSongPositionPref.GetLastPosition();
            CurrentPositionInSeconds = AppSettingsService.LastPlayedSongPositionPref.GetLastPosition() * TemporarilyPickedSong.DurationInSeconds;
            DisplayedSongs.First(x => x.Id == TemporarilyPickedSong.Id).IsCurrentPlayingHighlight = true;
        }
        else
        {
            var lastID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
            TemporarilyPickedSong = DisplayedSongs.FirstOrDefault(x => x.Id == lastID);
            if (TemporarilyPickedSong is null)
            {
                IsTemporarySongNull = true;
            }
                return;
        }
        //TemporarilyPickedSong.CoverImagePath = await FetchSongCoverImage();
        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = TemporarilyPickedSong;


    }

    private ObservableCollection<SongsModelView> GetXRecentlyAddedSongs(ObservableCollection<SongsModelView> displayedSongs, int number=15)
    {
        // Sort by DateAdded in descending order and take the top X songs
        var recentSongs = displayedSongs
            .OrderByDescending(song => song.DateAdded)  // Sort by DateAdded in descending order
            .Take(number)  // Limit to the top `number` of songs
            .ToList(); // Convert to a list

        // Convert the list back to ObservableCollection
        return new ObservableCollection<SongsModelView>(recentSongs);
    }


    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> lastFifteenPlayedSongs;

    public static IEnumerable<SingleSongStatistics> GetLastXPlayedSongs(IEnumerable<SongsModelView> allSongs, int number = 15)
    {
        // Filter and flatten only songs with non-empty DatesPlayedAndWasPlayCompleted
        var recentPlays = allSongs
            .Where(song => song.DatesPlayedAndWasPlayCompleted != null && song.DatesPlayedAndWasPlayCompleted.Count > 0) // Ensure the list is not null or empty
            .SelectMany(song => song.DatesPlayedAndWasPlayCompleted
                .Select(play => new SingleSongStatistics
                {
                    Song = song, // Reference to SongsModelView
                    PlayDateTime = play.DatePlayed
                }))
            .OrderByDescending(stat => stat.PlayDateTime)
            .Take(number) // Limit to the specified number of recent plays
            .ToList();

        return recentPlays;
    }


    #region Subscriptions to Services

    private IDisposable _playerStateSubscription;
    [ObservableProperty]
    bool isPlaying = false;
    [ObservableProperty]
    string playPauseIcon = MaterialRounded.Play_arrow;

    MediaPlayerState CurrentPlayerState;
    public void SetPlayerState(MediaPlayerState? state)
    {
        switch (state)
        {
            case MediaPlayerState.Playing:

                TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
                //if (DisplayedSongs is not null)
                //{
                //    if (TemporarilyPickedSong is not null)
                //    {
                //        var songToPlay= DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id);
                //        if (songToPlay is not null)
                //        {
                //            var songIndex = DisplayedSongs.IndexOf(songToPlay);
                //            if (songIndex != -1)
                //            {
                //                DisplayedSongs[songIndex] = TemporarilyPickedSong;
                //            }
                //        }
                //    }

                //}
                PickedSong = TemporarilyPickedSong;
                SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
                AllSyncLyrics = null;
                splittedLyricsLines = null;

                IsPlaying = true;
                CurrentLyricPhrase = new LyricPhraseModel() { Text = "" };

                CurrentRepeatCount = PlayBackService.CurrentRepeatCount;
                LyricsManagerService.LoadLyrics(TemporarilyPickedSong);
                break;
            case MediaPlayerState.Paused:
                IsPlaying = false;
                break;
            case MediaPlayerState.Stopped:
                //PickedSong = "Stopped";
                break;
            case MediaPlayerState.LoadingSongs:
                LoadingSongsProgress = PlayBackService.LoadingSongsProgressPercentage;
                break;
            case MediaPlayerState.ShowPauseBtn:
                IsPlaying = true;
                break;
            case MediaPlayerState.ShowPlayBtn:
                IsPlaying = false;
                break;
            case MediaPlayerState.RefreshStats:
                if (CurrentPage == PageEnum.FullStatsPage)
                {

                    //TemporarilyPickedSong.DatesPlayed = TemporarilyPickedSong.DatesPlayed
                    //.OrderByDescending(date => date).ToList();
                    ShowGeneralTopXSongs();
                    //ShowSingleSongStats(PickedSong);
                }
                break;
            default:
                break;
        }


    }


    public void Dispose()
    {
        _playerStateSubscription?.Dispose();
    }

    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            CurrentLyricPhrase = highlightedLyric is null ? null : highlightedLyric;
        });
    }
    private void SubscribeToPlaylistChanges()
    {
        PlayBackService.SecondaryQueue.Subscribe(songs =>
        {
            DisplayedSongsFromPlaylist = songs;
        });
    }
    private void SubscribeToCurrentSongPosition()
    {
        PlayBackService.CurrentPosition.Subscribe(async position =>
        {
            if (position.CurrentPercentagePlayed != 0)
            {
                CurrentPositionInSeconds = position.CurrentTimeInSeconds;
                CurrentPositionPercentage = position.CurrentPercentagePlayed;
                if (CurrentPositionPercentage >= 0.97 && IsPlaying && IsOnLyricsSyncMode)
                {
                    await PauseSong();
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await SaveLyricsToLrcAfterSyncing();
                    });
                }
            }


        });
    }

    [RelayCommand]
    async Task SaveLyricsToLrcAfterSyncing()
    {
        string? result = await Shell.Current.DisplayActionSheet("Done Syncing?", "No", "Yes");
        if (result is null)
            return;
        if (result.Equals("Yes"))
        {
            string? lyr = string.Join(Environment.NewLine, LyricsLines.Select(line => $"{line.TimeStampText} {line.Text}"));
            if (lyr is not null)
            {
                if (LyricsManagerService.WriteLyricsToLyricsFile(lyr, TemporarilyPickedSong, true))
                {
                    await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
                    CurrentViewIndex = 0;
                }
                LyricsManagerService.InitializeLyrics(lyr);
                DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
            }
        }
    }

    private void SubscribetoDisplayedSongsChanges()
    {
        PlayBackService.NowPlayingSongs.Subscribe(songs =>
        {
            TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            DisplayedSongs?.Clear();
            DisplayedSongs = songs;
            TotalNumberOfSongs = songs.Count;
            
            //ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;
    }
    private void SubscribeToBackEndQChanges()
    {
        PlayBackService.BackEndShufflableSongsQueue.Subscribe(songs =>
        {
            BackEndQ ??= [];
            BackEndQ?.Clear();
            BackEndQ = songs;

        });
    }

    private void SubscribeToSyncedLyricsChanges()
    {
        LyricsManagerService.SynchronizedLyricsStream.Subscribe(synchronizedLyrics =>
        {
            SynchronizedLyrics = synchronizedLyrics?.ToObservableCollection();
            if (TemporarilyPickedSong is not null)
            {
                TemporarilyPickedSong.HasSyncedLyrics = SynchronizedLyrics is not null;
            }
            else
            {
                if (TemporarilyPickedSong is null)
                {
                    return;
                }
                TemporarilyPickedSong.UnSyncLyrics = "No Lyrics Found...";
            }
        });
    }
    #endregion

    [ObservableProperty]
    string lyricsSearchSongTitle;
    [ObservableProperty]
    string lyricsSearchArtistName;
    [ObservableProperty]
    string lyricsSearchAlbumName;
    [ObservableProperty]
    bool useManualSearch;

    [ObservableProperty]
    Content[] allSyncLyrics;
    [ObservableProperty]
    bool isFetchSuccessful = true;
    [ObservableProperty]
    bool isFetching = false;
    
    public async Task<bool> FetchLyrics(bool fromUI = false)
    {
        LyricsSearchSongTitle ??= SelectedSongToOpenBtmSheet.Title;
        LyricsSearchArtistName ??= SelectedSongToOpenBtmSheet.ArtistName;
        LyricsSearchAlbumName ??= SelectedSongToOpenBtmSheet.AlbumName;

        List<string> manualSearchFields =
        [
            LyricsSearchAlbumName,
            LyricsSearchArtistName,
            LyricsSearchSongTitle,
        ];

        //if (fromUI || SynchronizedLyrics?.Count < 1)
        //{
        AllSyncLyrics = Array.Empty<Content>();
        (bool IsSuccessful, Content[] contentData) result= await LyricsManagerService.FetchLyricsOnlineLrcLib(SelectedSongToOpenBtmSheet, true,manualSearchFields);
        
        AllSyncLyrics = result.contentData;
    
        IsFetchSuccessful = result.IsSuccessful;
        LyricsSearchSongTitle = null;
        LyricsSearchArtistName = null;
        LyricsSearchAlbumName = null;

        return IsFetchSuccessful;
    }

    [RelayCommand]
    async Task FetchLyricsLyrist(bool fromUI = false)
    {
        IsFetching = true;
        if (fromUI || SynchronizedLyrics?.Count < 1)
        {
            AllSyncLyrics = Array.Empty<Content>();
            (IsFetchSuccessful, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLyrist(TemporarilyPickedSong.Title, TemporarilyPickedSong.ArtistName);
        }
        IsFetching = false;
        return;
    }

    public async Task ShowSingleLyricsPreviewPopup(Content cont, bool IsPlain)
    {
        var result = (bool)await Shell.Current.ShowPopupAsync(new SingleLyricsPreviewPopUp(cont, IsPlain, this));
        if (result)
        {
            await SaveSelectedLyricsToFile(!IsPlain, cont);
            if (TemporarilyPickedSong is null)
                TemporarilyPickedSong = SelectedSongToOpenBtmSheet;
        }
    }

       

    public async Task SaveSelectedLyricsToFile(bool isSync, Content cont) // rework this!
    {
        bool isSavedSuccessfully;

        if (!isSync)
        {
            SelectedSongToOpenBtmSheet.HasLyrics = true;
            SelectedSongToOpenBtmSheet.UnSyncLyrics = cont.plainLyrics;
            SelectedSongToOpenBtmSheet.HasSyncedLyrics = false;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.plainLyrics, SelectedSongToOpenBtmSheet, isSync);
        }
        else
        {
            SelectedSongToOpenBtmSheet.HasLyrics = false;
            SelectedSongToOpenBtmSheet.UnSyncLyrics = string.Empty;
            SelectedSongToOpenBtmSheet.HasSyncedLyrics = true;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.syncedLyrics, SelectedSongToOpenBtmSheet, isSync);
        }
        if (isSavedSuccessfully)
        {
            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
            AllSyncLyrics = [];
            CurrentViewIndex = 0;
        }
        else
        {
            await Shell.Current.DisplayAlert("Error !", "Failed to Save Lyrics!", "OK");
            return;
        }
        if (!isSync)
        {
            return;
        }
        LyricsManagerService.InitializeLyrics(cont.syncedLyrics);
        if (DisplayedSongs.FirstOrDefault(x => x.Id == SelectedSongToOpenBtmSheet.Id) is not null)
        {
            DisplayedSongs.FirstOrDefault(x => x.Id == SelectedSongToOpenBtmSheet.Id)!.HasLyrics = true;
        }
        if (PlayBackService.CurrentQueue != 2)
        {
            SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
        }

    }

    [ObservableProperty]
    ObservableCollection<LyricPhraseModel>? lyricsLines = new();
    [RelayCommand]
    async Task CaptureTimestamp(LyricPhraseModel lyricPhraseModel)
    {
        var CurrPosition = CurrentPositionInSeconds;
        if (!IsPlaying)
        {
            await PlaySong();
        }

        LyricPhraseModel? Lyricline = LyricsLines?.FirstOrDefault(x => x == lyricPhraseModel);
        if (Lyricline is null)
            return;


        Lyricline.TimeStampMs = (int)CurrPosition * 1000;
        Lyricline.TimeStampText = string.Format("[{0:mm\\:ss\\.ff}]", TimeSpan.FromSeconds(CurrPosition));

    }

    [RelayCommand]
    void DeleteLyricLine(LyricPhraseModel lyricPhraseModel)
    {
        LyricsLines?.Remove(lyricPhraseModel);
        if (TemporarilyPickedSong.UnSyncLyrics is null)
        {
            return;
        }
        TemporarilyPickedSong.UnSyncLyrics = RemoveTextAndFollowingNewline(TemporarilyPickedSong.UnSyncLyrics, lyricPhraseModel.Text);//TemporarilyPickedSong.UnSyncLyrics.Replace(lyricPhraseModel.Text, string.Empty);
    }

    string[]? splittedLyricsLines;

    void PrepareLyricsSync()
    {
        if (TemporarilyPickedSong?.UnSyncLyrics == null)
            return;

        // Define the terms to be removed
        string[] termsToRemove = new[]
        {
        "[Chorus]", "Chorus", "[Verse]", "Verse", "[Hook]", "Hook",
        "[Bridge]", "Bridge", "[Intro]", "Intro", "[Outro]", "Outro",
        "[Pre-Chorus]", "Pre-Chorus", "[Instrumental]", "Instrumental",
        "[Interlude]", "Interlude"
        };

        // Remove all the terms from the lyrics
        string cleanedLyrics = TemporarilyPickedSong.UnSyncLyrics;
        foreach (var term in termsToRemove)
        {
            cleanedLyrics = cleanedLyrics.Replace(term, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        string[]? ss = cleanedLyrics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        splittedLyricsLines = ss?.Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (splittedLyricsLines is null || splittedLyricsLines.Length < 1)
        {
            return;
        }
        foreach (var item in splittedLyricsLines)
        {
            var LyricPhrase = new LyricsPhrase(0, item);
            LyricPhraseModel newLyric = new(LyricPhrase);
            LyricsLines?.Add(newLyric);
        }
    }

    static string RemoveTextAndFollowingNewline(string input, string textt)
    {
        string result = input;

        int index = result.IndexOf(textt);

        while (index != -1)
        {
            int nextCharIndex = index + textt.Length;
            if (nextCharIndex < result.Length)
            {
                if (result[nextCharIndex] == '\r' || result[nextCharIndex] == '\n')
                {
                    result = result.Remove(index, textt.Length + 1);
                }
                else
                {
                    result = result.Remove(index, textt.Length);
                }
            }
            else
            {
                result = result.Remove(index, textt.Length);
            }

            index = result.IndexOf(textt);
        }

        return result;
    }

    [RelayCommand]
    public async Task FetchSongCoverImage(SongsModelView? song=null)
    {
        if (song is null)
        {
            if (!string.IsNullOrEmpty(TemporarilyPickedSong.CoverImagePath))
            {
                if (!File.Exists(TemporarilyPickedSong.CoverImagePath))
                {
                    TemporarilyPickedSong.CoverImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong.Title, TemporarilyPickedSong.ArtistName, TemporarilyPickedSong.AlbumName, TemporarilyPickedSong);
                }
            }
            return;
        }
        else
        {
            var str = await LyricsManagerService.FetchAndDownloadCoverImage(song.Title, song.ArtistName, song.AlbumName, song);
            SelectedSongToOpenBtmSheet.CoverImagePath = str;
        }
        
    }

    [RelayCommand]
    public async Task FetchAlbumCoverImage(AlbumModelView album)
    {
        var firstSong = DisplayedSongs.Where(x => x.Id == album.Id).FirstOrDefault();
        if (album is not null)
        {

            if (!string.IsNullOrEmpty(album.AlbumImagePath))
            {
                if (!File.Exists(album.AlbumImagePath))
                {
                    album.AlbumImagePath = await LyricsManagerService.FetchAndDownloadCoverImage(firstSong.Title, firstSong.ArtistName, firstSong.AlbumName, firstSong);
                }
            }
            return;
        }
        else
        {
            AllAlbums.FirstOrDefault(x => x.Id == album.Id).AlbumImagePath= await LyricsManagerService.FetchAndDownloadCoverImage(firstSong.Title, firstSong.ArtistName, firstSong.AlbumName,firstSong);
        }
        
    }



    [RelayCommand]
    async Task ReloadCoversForAllSongs()
    {
        IsLoadingSongs = true;
        
        foreach (var song in SongsMgtService.AllSongs)
        {
            await FetchSongCoverImage(song);
        }
        IsLoadingSongs = false;
    }

    [ObservableProperty]
    int currentViewIndex;
    [ObservableProperty]
    bool isOnLyricsSyncMode = false;
    [RelayCommand]
    void SwitchViewNowPlayingPage(int viewIndex)
    {
        
        switch (viewIndex)
        {
            case 0:
                if (splittedLyricsLines is null)
                {
                    return;
                }
                Array.Clear(splittedLyricsLines);
                break;
            case 1:
                break;
            case 2:
                SongPickedForStats ??= new SingleSongStatistics();
                SongPickedForStats.Song = TemporarilyPickedSong;

                OpenEditableSongsTagsView();
                CurrentPage = PageEnum.FullStatsPage;
                ShowSingleSongStats(SelectedSongToOpenBtmSheet);
                break;
            default:
                break;
        }

    }

    [RelayCommand]
    async Task OpenSortingPopup()
    {
        var result = await Shell.Current.ShowPopupAsync(new SortingPopUp(this, CurrentSortingOption));

        if (result != null)
        {
            var e = (SortingEnum)result;
            //if (e == CurrentSortingOption)
            //{
            //    return;
            //}
            IsLoadingSongs = true;
            CurrentSortingOption = e;
            if (CurrentPage == PageEnum.MainPage)
            {
                DisplayedSongs = AppSettingsService.ApplySorting(DisplayedSongs, CurrentSortingOption);
                PageCV.ItemsSource = null;
                PageCV.ItemsSource = DisplayedSongs;

            }
            else if (CurrentPage == PageEnum.AllAlbumsPage)
            {
                AllArtistsAlbumSongs = AppSettingsService.ApplySorting(AllArtistsAlbumSongs, CurrentSortingOption);
                
            }
            else if (CurrentPage == PageEnum.SpecificAlbumPage)
            {
                AllArtistsAlbumSongs = AppSettingsService.ApplySorting(AllArtistsAlbumSongs, CurrentSortingOption);
            }
        }

        IsLoadingSongs = false;
    }


    int CurrentRepeatMaxCount;
    [ObservableProperty]
    int currentRepeatCount;
    [RelayCommand]
    async Task OpenRepeatSetterPopup()
    {
        if (!EnableContextMenuItems)
            return;
#if ANDROID
        PickedSong = SelectedSongToOpenBtmSheet;
#endif

        var result = (int)await Shell.Current.ShowPopupAsync(new CustomRepeatPopup(CurrentRepeatMaxCount, PickedSong));

        if (result > 0)
        {
            CurrentRepeatMode = 4;
            CurrentRepeatMaxCount = result;
            await PlaySong();
            ToggleRepeatMode();

            CurrentRepeatMaxCount = 0;
        }
    }

    void OpenEditableSongsTagsView()
    {
        LyricsLines?.Clear();
        PrepareLyricsSync();
    }

    

    [RelayCommand]
    void SetPickedPlaylist(PlaylistModelView pl)
    {
        SelectedPlaylistToOpenBtmSheet = pl;
    }

    public void SetContextMenuSong(SongsModelView? song)
    {
        SelectedSongToOpenBtmSheet = song;
    }

    

    [RelayCommand]
    async Task NavigateToShareStoryPage()
    {
        await Shell.Current.GoToAsync(nameof(ShareSongPage));
    }

    [ObservableProperty]
    ObservableCollection<SongsModelView> backEndQ;

    [ObservableProperty]
    bool isAnimatingFav = false;
    System.Timers.Timer _showAndHideFavGif;
    [RelayCommand]
    public async Task RateSong(Rating obj)
    {
        _showAndHideFavGif = new System.Timers.Timer(2000);
        _showAndHideFavGif.AutoReset = false;
        _showAndHideFavGif.Elapsed += ((send, arg) =>
        {
            IsAnimatingFav = false;
        });
        var willBeFav = false;
        if (obj is not null)
        {
            var rateValue = obj.Value;
            switch (rateValue)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    willBeFav = false;
                    break;
                case 4:
                case 5:
                    willBeFav = true;
                    break;
                default:
                    break;
            }
            SelectedSongToOpenBtmSheet.IsFavorite = willBeFav;
            bool isAdd = false;
            var favPlaylist = new PlaylistModelView { Name = "Favorites" };
            if (SelectedSongToOpenBtmSheet.Rating < 3 && rateValue>3)
            {
                SelectedSongToOpenBtmSheet.Rating = (int)rateValue;
                IsAnimatingFav = true;
#if ANDROID
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
#endif

                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsAddSong: true, playlistModel: favPlaylist);
                await Task.Delay(3000);
                IsAnimatingFav = false;
            }
            else
            if (SelectedSongToOpenBtmSheet.Rating == 0 && rateValue <= 3)
            {
                SelectedSongToOpenBtmSheet.Rating = (int)rateValue;
                SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
            }
            else
            if (SelectedSongToOpenBtmSheet.Rating == 0 && rateValue >= 4)
            {
                IsAnimatingFav = true;
                SelectedSongToOpenBtmSheet.Rating = (int)rateValue;
                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsAddSong: true, playlistModel: favPlaylist);
                await Task.Delay(3000);
                IsAnimatingFav = false;
            }
            else if (SelectedSongToOpenBtmSheet.Rating<3 && rateValue <=3)
            {
                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsRemoveSong: true, playlistModel: favPlaylist);
            }
            else if (SelectedSongToOpenBtmSheet.Rating>4 && rateValue <=3)
            {
                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsRemoveSong: true, playlistModel: favPlaylist);
            }
            else if (SelectedSongToOpenBtmSheet.Rating > 4 && rateValue > 4)
            {
                 SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
            }

        }
    }

    
    [ObservableProperty]
    BottomSheetState nowPlayBtmSheetState = BottomSheetState.Hidden;
    [RelayCommand]
    void ShowNowPlayingBtmSheet()
    {
        NowPlayBtmSheetState = BottomSheetState.FullExpanded;
    }
}