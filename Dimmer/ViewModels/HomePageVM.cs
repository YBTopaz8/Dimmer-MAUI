using DevExpress.Maui.Controls;
using Hqub.Lastfm.Cache;
using Parse.LiveQuery;


namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
    //LastfmClient LastfmClient;
    [ObservableProperty]
    public partial UserModelView CurrentUser { get; set; } =new();
    [ObservableProperty]
    public partial ParseUser? CurrentUserOnline { get; set; }

    [ObservableProperty]
    public partial FlyoutBehavior ShellFlyoutBehavior { get; set; } = FlyoutBehavior.Disabled;
    [ObservableProperty]
    public partial bool IsFlyOutPaneOpen { get; set; } = false;
    [ObservableProperty]
    public partial SongModelView PickedSong { get; set; } = new(); // I use this a lot with the collection view, mostly to scroll

    [ObservableProperty]
    public partial SongModelView TemporarilyPickedSong { get; set; } = new();

    [ObservableProperty]
    public partial double CurrentPositionPercentage { get; set; }
    [ObservableProperty]
    public partial double CurrentPositionInSeconds { get; set; } = 0;

    List<PlayDataLink> AllPlayDataLinks { get; set; } = Enumerable.Empty<PlayDataLink>().ToList();   
    List<AlbumArtistGenreSongLinkView> AllLinks { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; } = Enumerable.Empty<SongModelView>().ToObservableCollection();

    //[ObservableProperty]
    //public partial ObservableCollection<SongModelView> PrevCurrNextSongsCollection { get; set; } = new();

    SortingEnum CurrentSortingOption;
    [ObservableProperty]
    public partial int TotalNumberOfSongs { get; set; } = 0;
    [ObservableProperty]
    public partial string? TotalSongsSize { get; set; }
    [ObservableProperty]
    public partial string? TotalSongsDuration { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SynchronizedLyrics { get; set; } = Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();
    [ObservableProperty]
    public partial LyricPhraseModel CurrentLyricPhrase { get; set; } = new();

    [ObservableProperty]
    public partial int LoadingSongsProgress { get; set; }

    [ObservableProperty]
    public partial double VolumeSliderValue { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsShuffleOn { get; set; }
    [ObservableProperty]
    public partial int CurrentRepeatMode { get; set; }
    [ObservableProperty]
    public partial bool IsDRPCEnabled { get; set; }
    IFolderPicker FolderPicker { get; }
    IFileSaver FileSaver { get; }
    IPlaybackUtilsService PlayBackService { get; }
    ILyricsService LyricsManagerService { get; }
    public ISongsManagementService SongsMgtService { get; }
    [ObservableProperty]
    public partial string? UnSyncedLyrics { get; set; }
    [ObservableProperty]
    public partial string? LocalFilePath { get; set; }

    public AppState CurrentAppState = AppState.OnForeGround;

    [ObservableProperty]
    public partial int CurrentQueue { get; set; } = 0;
    public HomePageVM(IPlaybackUtilsService PlaybackManagerService, IFolderPicker folderPickerService, IFileSaver fileSaver,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService)
    {
        this.FolderPicker = folderPickerService;
        FileSaver = fileSaver;
        PlayBackService = PlaybackManagerService;
        LyricsManagerService = lyricsService;
        SongsMgtService = songsMgtService;
        SongsMgtService.InitApp(this);
        CurrentSortingOption = AppSettingsService.SortingModePreference.GetSortingPref();

        SubscribeToPlayerStateChanges();
        SubscribetoDisplayedSongsChanges();

        SubscribeToCurrentSongPosition();
        SubscribeToPlaylistChanges();

        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        IsPlaying = false;
        //DisplayedPlaylists = PlayBackService.AllPlaylists;
        TotalSongsDuration = PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;
        IsPlaying = false;
        ToggleShuffleState();
        ToggleRepeatMode();
        //AppSettingsService.MusicFoldersPreference.ClearListOfFolders();
        FolderPaths = AppSettingsService.MusicFoldersPreference.GetMusicFolders().ToObservableCollection();
        IsDRPCEnabled = AppSettingsService.DiscordRPCPreference.IsDiscordRPCEnabled;
        
        //SubscribeToDataChanges();
#if WINDOWS
        ToggleFlyout();
#endif
        CurrentUser = SongsMgtService.CurrentOfflineUser;
        SyncRefresh();
        

        LoadData();

        LoadSongCoverImage();
        //_ = GetSecuredData();

    }
    public async Task AssignCV(CollectionView cv)
    {
        DisplayedSongsColView = cv;

        if (CurrentUserOnline is null || string.IsNullOrEmpty(CurrentUserOnline.Username))
        {
            await LogInParseOnline(true);
            CurrentUserOnline = await APIKeys.LogInParseOnline();
            
            //await LastFMUtils.QuickLoginToLastFM();
            return;
            /*
            */
        }
        if (!string.IsNullOrEmpty(CurrentUserOnline.SessionToken))
        {
            CurrentUser.IsAuthenticated = true;
            CurrentUser.IsLoggedInLastFM = true;
            SetupLiveQueries();
        }

    }
    partial void OnTemporarilyPickedSongChanging(SongModelView oldValue, SongModelView newValue)
    {
        
        if (newValue is not null && string.IsNullOrEmpty(newValue.CoverImagePath))
        {
            newValue.CoverImagePath = string.Empty;
        }
        if (newValue is not null && !string.IsNullOrEmpty(newValue.CoverImagePath))
        {
            if (newValue.CoverImagePath == oldValue?.CoverImagePath)
            {
                if (oldValue.AlbumName != newValue.AlbumName)
                {
                    newValue.CoverImagePath = string.Empty;
                }
            }
        }

        if (newValue is not null)
        {
         
        }
    }

    partial void OnSynchronizedLyricsChanging(ObservableCollection<LyricPhraseModel> oldValue, ObservableCollection<LyricPhraseModel> newValue)
    {
        if (newValue is not null && newValue.Count < 1)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (SyncLyricsCV is not null)
                {
                    SyncLyricsCV!.ItemsSource = null;               
                }

            });
        }
        if (newValue is not null && newValue.Count > 0)
        {
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (SyncLyricsCV is not null)
                {
                    SyncLyricsCV!.ItemsSource = null;
                    SyncLyricsCV.ItemsSource = newValue;
                    SyncLyricsCV.ScrollTo(CurrentLyricPhrase, null, ScrollToPosition.Center, true);
                }

            });
        }
        
    }
    public void SyncRefresh()
    {
       PlayBackService.FullRefresh();
        AllArtists = SongsMgtService.AllArtists.ToObservableCollection();
        AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        
        GetAllArtists();
        GetAllAlbums();
        RefreshPlaylists();        
    }

    void DoRefreshDependingOnPage()
    {
        LyricsSearchSongTitle = SelectedSongToOpenBtmSheet.Title;
        LyricsSearchArtistName = SelectedSongToOpenBtmSheet.ArtistName;
        LyricsSearchAlbumName = SelectedSongToOpenBtmSheet.AlbumName;
        LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();
        switch (CurrentPage)
        {
            case PageEnum.MainPage:
                
                break;
            case PageEnum.NowPlayingPage:
                LastfmTracks.Clear();
                CalculateGeneralSongStatistics(TemporarilyPickedSong.LocalDeviceId!);
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
                //ShowGeneralTopXSongs();
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
    public partial bool IsMultiSelectOn { get; set; }
    CollectionView DisplayedSongsColView { get; set; }
   

    CollectionView? SyncLyricsCV { get; set; }
    public void AssignSyncLyricsCV(CollectionView cv)
    {
        SyncLyricsCV = cv;
    }
    [ObservableProperty]
    string loadingSongsText;
    public void SetLoadingProgressValue(double newValue)
    {
        if (newValue<100)
        {
            LoadingSongsText = $"Loading {newValue}% done";
            return;
        }
        LoadingSongsText = $"Loading Completed !";

    }

    public async void LoadLocalSongFromOutSideApp(List<string> filePath)
    {
        CurrentQueue = 2;
        await PlayBackService.PlaySelectedSongsOutsideAppAsync(filePath);
    }

    [RelayCommand]
    public async Task NavToSingleSongShell()
    {
        
        CurrentViewIndex = 0;
#if WINDOWS
        
        await Shell.Current.GoToAsync(nameof(SingleSongShellPageD));
        
        await AfterSingleSongShellAppeared();
        
        ToggleFlyout();
        
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
                SynchronizedLyrics?.Clear();        
            }      
        }
        if (SelectedSongToOpenBtmSheet.SyncLyrics is null || SelectedSongToOpenBtmSheet.SyncLyrics.Count < 1)
        {            
            var ee = LyricsManagerService.GetSpecificSongLyrics(SelectedSongToOpenBtmSheet).ToObservableCollection();
            SynchronizedLyrics?.Clear();
            foreach (var item in ee)
            {
                SynchronizedLyrics?.Add(item);
            }

            SelectedSongToOpenBtmSheet.SyncLyrics = SynchronizedLyrics!;
            SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
        }
        if (SongPickedForStats is null)
        {
            SongPickedForStats = new()
            {
                Song = SelectedSongToOpenBtmSheet
            };
        }
        else
        {
            SongPickedForStats.Song = SelectedSongToOpenBtmSheet;
        }
        
    }

    public async Task AfterSingleSongShellAppeared()
    {
        if (SelectedSongToOpenBtmSheet is null)
        {
            return ;
        }
        
     
        CurrentPage = PageEnum.NowPlayingPage;
        if (!string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.CoverImagePath) && !File.Exists(SelectedSongToOpenBtmSheet.CoverImagePath))
        {
            var coverImg = await LyricsManagerService
                .FetchAndDownloadCoverImage(SelectedSongToOpenBtmSheet.Title, SelectedSongToOpenBtmSheet.ArtistName!, SelectedSongToOpenBtmSheet.AlbumName!, SelectedSongToOpenBtmSheet);
            SongsMgtService.AllSongs
                .FirstOrDefault(x => x.LocalDeviceId == SelectedSongToOpenBtmSheet.LocalDeviceId)!.CoverImagePath = coverImg;
            return;
        }
        return;
    }

    #region Loadings Region

    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; }
    [RelayCommand]
    public async Task SelectSongFromFolder()
    {
        //bool res = await Shell.Current.DisplayAlert("Select Song", "Sure?", "Yes", "No");
        //if (!res)
        //{
        //    return;
        //}

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
#if ANDROID
        PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
#endif

#if WINDOWS || ANDROID 
        var res = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync(token);

        if (res.Folder is null)
        {
            return;
        }
        var folder = res.Folder?.Path;
#elif IOS || MACCATALYST && NET9_0
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
    public async Task LoadSongsFromFolders()
    {
        try
        {
            DeviceDisplay.Current.KeepScreenOn = true;
            IsLoadingSongs = true;
            if (FolderPaths is null || FolderPaths.Count < 0)
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


    async Task UpdateRelatedPlayingData(SongModelView song)
    {
        var songArt = song.ArtistName;
        if (!string.IsNullOrEmpty(songArt))
        {
            if (AllArtists is not null && AllArtists.Count > 0)
            {
                var art= GetAllArtistsFromSongID(song.LocalDeviceId!).FirstOrDefault();

                if (art is null)
                {
                    List<string> load = [song.FilePath];

                    if(await PlayBackService.LoadSongsFromFolderAsync(load))
                    {
                        string msg = $"Song {song.Title} remembered.";
                        //_ = ShowNotificationAsync(msg);
                    }
                    return;
                }
                SelectedAlbumOnArtistPage = GetAlbumFromSongID(song.LocalDeviceId!).SingleOrDefault();
            }
        }
    }


    public List<SongModelView> filteredSongs = new();
    [ObservableProperty]
    public partial bool IsPreviewing { get; set; } = false;
    public async Task PlaySong(SongModelView SelectedSong, bool isPrevieww=false)
    {
        //_ = UpdateRelatedPlayingData(SelectedSong!);//always check if we already have the song's artist and other data loaded
        if (isPrevieww)
        {
            IsPreviewing = isPrevieww;
            CurrentPage = PageEnum.FullStatsPage;  
            CurrentQueue = 0;
            await PlayBackService.PlaySongAsync(SelectedSong, isPreview: true);
            return;
        }

        TemporarilyPickedSong ??= SelectedSong!;
        TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        
        if (SelectedSong is not null)
        {
            SelectedSong.IsCurrentPlayingHighlight = false;
            SelectedSongToOpenBtmSheet = SelectedSong;

            CurrentQueue = 0;
            if (CurrentPage == PageEnum.PlaylistsPage) // plays from a PL
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: DisplayedSongsFromPlaylist, repeatMode: CurrentRepeatMode, repeatMaxCount: CurrentRepeatMaxCount);
                return;
            }
            if (CurrentPage == PageEnum.FullStatsPage) // plays according to their stats
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: TopTenPlayedSongs.Select(x => x.Song).ToObservableCollection()!);
                //ShowGeneralTopXSongs();
                return;
            }
            // below here is for when on full or specific album page since it's ok and album actually has both
            if (CurrentPage == PageEnum.SpecificAlbumPage || CurrentPage == PageEnum.AllAlbumsPage && SelectedSong != null)
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: AllArtistsAlbumSongs);
                return;
            }
            if (IsOnSearchMode) // here is for when I SEARCH and I'm passing also the list of songs displayed too
            {
                CurrentQueue = 1;
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue, SecQueueSongs: filteredSongs.ToObservableCollection());
            }
            else // default playing on main page
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
        if (IsPreviewing)
        {
            return;
        }
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
        TemporarilyPickedSong!.IsCurrentPlayingHighlight= TemporarilyPickedSong is null;
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        await PlayBackService.PlayNextSongAsync(true);
    }

    [RelayCommand]
    async Task PlayPreviousSong()
    {
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        await PlayBackService.PlayPreviousSongAsync(true);
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
            PlayBackService.SeekTo(CurrentPositionInSeconds);
            return;
        }
        if (TemporarilyPickedSong is null)
            return;
        CurrentPositionInSeconds = CurrentPositionPercentage * TemporarilyPickedSong.DurationInSeconds;
        PlayBackService.SeekTo(CurrentPositionInSeconds);
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
                RepeatModeImage = "repeaton.png";
                break;
            case 2:
            case 4:
                RepeatModeImage = "repeatoffdark.png";
                break;
            case 0:
                RepeatModeImage = "repeatone.png";
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
    async Task UpdateSongToDB(SongModelView song)
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
    ObservableCollection<SongModelView> recentlyAddedSongs;
    public void LoadSongCoverImage()
    {
        if (DisplayedSongs is null)
        {
            return;
        }

        if (DisplayedSongs.Count < 1)
        {
            return;
        }
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

            var s = DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == TemporarilyPickedSong.LocalDeviceId);
            if (s is not null)
            {
                DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == TemporarilyPickedSong.LocalDeviceId)!.IsCurrentPlayingHighlight = true;
            }

        }
        else
        {
            var lastID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
            TemporarilyPickedSong = DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == lastID);
            if (TemporarilyPickedSong is null)
            {
                TemporarilyPickedSong= DisplayedSongs!.First();
            }
            
        }
        //TemporarilyPickedSong.CoverImagePath = await FetchSongCoverImage();
        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = TemporarilyPickedSong;

        SelectedArtistOnArtistPage = GetAllArtistsFromSongID(TemporarilyPickedSong.LocalDeviceId!).FirstOrDefault();
        SelectedAlbumOnArtistPage = GetAlbumFromSongID(TemporarilyPickedSong.LocalDeviceId!).FirstOrDefault();
    }

    private ObservableCollection<SongModelView> GetXRecentlyAddedSongs(ObservableCollection<SongModelView> displayedSongs, int number=15)
    {
        // Sort by DateAdded in descending order and take the top X songs
        var recentSongs = displayedSongs
            .OrderByDescending(song => song.DateCreated)  
            .Take(number)  
            .ToList(); 
        
        return new ObservableCollection<SongModelView>(recentSongs);
    }


    [ObservableProperty]
    ObservableCollection<SingleSongStatistics> lastFifteenPlayedSongs;

    public static IEnumerable<SingleSongStatistics> GetLastXPlayedSongs(IEnumerable<SongModelView>? allSongs, int number = 15)
    {
        //if (allSongs is null)
            return Enumerable.Empty<SingleSongStatistics>();
        //var recentPlays = allSongs
        //    .Where(song => song.DatesPlayedAndWasPlayCompleted != null && song.DatesPlayedAndWasPlayCompleted.Count > 0) // Ensure the list is not null or empty
        //    .SelectMany(song => song.DatesPlayedAndWasPlayCompleted
        //        .Select(play => new SingleSongStatistics
        //        {
        //            Song = song, 
        //            PlayDateTime = play.DatePlayed
        //        }))
        //    .OrderByDescending(stat => stat.PlayDateTime)
        //    .Take(number) 
        //    .ToList();

        //return recentPlays;
    }


    #region Subscriptions to Services

    private IDisposable _playerStateSubscription;
    [ObservableProperty]
    bool isPlaying = false;
    [ObservableProperty]
    string playPauseIcon = MaterialRounded.Play_arrow;

    MediaPlayerState currentPlayerState;
    public void SetPlayerState(MediaPlayerState? state)
    {
        switch (state)
        {
            case MediaPlayerState.Playing:

                TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            
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
               
                break;
            case MediaPlayerState.LoadingSongs:
                
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

    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            if (highlightedLyric is not null)
            {
                CurrentLyricPhrase = highlightedLyric;                 
            }
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

    private bool _isLoading = false;


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
                            {
                                PickedSong.IsPlaying = false;
                                PickedSong.IsCurrentPlayingHighlight = false;                                
                                await ParseStaticUtils.UpdateSongStatusOnline(PickedSong, CurrentUser.IsAuthenticated);
                            }
                            PickedSong = TemporarilyPickedSong;
                            

                            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;

                            AllSyncLyrics = Enumerable.Empty<Content>().ToObservableCollection();
                            splittedLyricsLines = null;

                            IsPlaying = true;


                            PlayPauseIcon = MaterialRounded.Pause;
                            CurrentLyricPhrase = new LyricPhraseModel() { Text = "" };
                            DoRefreshDependingOnPage();

                            CurrentRepeatCount = PlayBackService.CurrentRepeatCount;

                            await FetchSongCoverImage();

                            await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);

                            break;
                        case MediaPlayerState.Paused:
                            await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);
                            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
                            PickedSong ??= TemporarilyPickedSong;

                            IsPlaying = false;
                            //PlayPauseIcon = MaterialRounded.Play_arrow;
                            break;
                        case MediaPlayerState.Stopped:
                            //PickedSong = "Stopped";
                            break;
                        case MediaPlayerState.LoadingSongs:

                            break;
                        case MediaPlayerState.ShowPlayBtn:
                            //PlayPauseIcon = MaterialRounded.Play_arrow;
                            IsPlaying = false;
                            break;
                        case MediaPlayerState.ShowPauseBtn:
                            IsPlaying = true;
                            //PlayPauseIcon = MaterialRounded.Pause;
                            break;
                        case MediaPlayerState.DoneScanningData:
                            SyncRefresh();
                            SetLoadingProgressValue(100);
                            break;
                        default:
                            break;
                    }
                }
            });

    }
    private void SubscribetoDisplayedSongsChanges()
    {
        PlayBackService.NowPlayingSongs            
            .Subscribe(songs =>
        {
            TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            DisplayedSongs?.Clear();
            MainThread.BeginInvokeOnMainThread( () =>
            {
                DisplayedSongs = songs;
                if (DisplayedSongs is null)
                {
                    return;
                }
                if (DisplayedSongsColView is null)
                {
                    return;
                }
                DisplayedSongsColView.ItemsSource = songs;
                TotalNumberOfSongs = songs.Count;
                //ReloadSizeAndDuration();
            });

            //ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;

      
    }
  
    partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    }


    private void SubscribeToSyncedLyricsChanges()
    {
        LyricsManagerService.SynchronizedLyricsStream.Subscribe(synchronizedLyrics =>
        {
            SynchronizedLyrics = synchronizedLyrics.ToObservableCollection();
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
    public partial string LyricsSearchSongTitle { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string LyricsSearchArtistName {get;set;} = string.Empty;
    [ObservableProperty]
    public partial string LyricsSearchAlbumName {get;set;}=string.Empty;
    [ObservableProperty]
    public partial bool UseManualSearch { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Content> AllSyncLyrics { get; set; } = Enumerable.Empty<Content>().ToObservableCollection();
    [ObservableProperty]
    public partial bool IsFetchSuccessful {get;set;}= true;
   

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
    public partial int CurrentViewIndex { get; set; }
    

    [ObservableProperty]
    public partial bool IsOnLyricsSyncMode { get; set; } = false;
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
                LyricsSearchSongTitle = SelectedSongToOpenBtmSheet.Title;
                LyricsSearchArtistName = SelectedSongToOpenBtmSheet.ArtistName;
                LyricsSearchAlbumName = SelectedSongToOpenBtmSheet.AlbumName;

                break;
            case 2:
                SongPickedForStats ??= new SingleSongStatistics();
                SongPickedForStats.Song = TemporarilyPickedSong;

                OpenEditableSongsTagsView();
                CurrentPage = PageEnum.FullStatsPage;
                ShowSingleSongStats(SelectedSongToOpenBtmSheet);
                break;
            case 3:
                SelectedArtistOnArtistPage = GetAllArtistsFromSongID(SelectedSongToOpenBtmSheet.LocalDeviceId!).FirstOrDefault();
                SelectedAlbumOnArtistPage = GetAlbumFromSongID(SelectedSongToOpenBtmSheet.LocalDeviceId!).FirstOrDefault();

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
            IsLoadingSongs = true;
            CurrentSortingOption = e;
            if (CurrentPage == PageEnum.MainPage)
            {
                DisplayedSongs = AppSettingsService.ApplySorting(DisplayedSongs!, CurrentSortingOption);
                DisplayedSongsColView.ItemsSource = null;
                DisplayedSongsColView.ItemsSource = DisplayedSongs;

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
    public partial int CurrentRepeatCount { get; set; }
    [RelayCommand]
    async Task OpenRepeatSetterPopup()
    {
        if (!EnableContextMenuItems)
            return;
#if ANDROID
        PickedSong = SelectedSongToOpenBtmSheet;
#endif

        var result = ((int)await Shell.Current.ShowPopupAsync(new CustomRepeatPopup(CurrentRepeatMaxCount, PickedSong)));

        if (result > 0)
        {
            CurrentRepeatMode = 4;
            CurrentRepeatMaxCount = result;
            await PlaySong(TemporarilyPickedSong);
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
    [RelayCommand]
    public void SetContextMenuSong(SongModelView song)
    {
        SelectedSongToOpenBtmSheet = song;
    }

    

    [RelayCommand]
    async Task NavigateToShareStoryPage()
    {
        await Shell.Current.GoToAsync(nameof(ShareSongPage));
    }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> BackEndQ { get; set; }

    [ObservableProperty]
    public partial bool IsAnimatingFav { get; set; } = false;
   

    [ObservableProperty]
    public partial BottomSheetState NowPlayBtmSheetState { get; set; } = BottomSheetState.Hidden;
    [RelayCommand]
    void ShowNowPlayingBtmSheet()
    {
        NowPlayBtmSheetState = BottomSheetState.FullExpanded;
    }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track> LastfmTracks { get; set; } = new();

    [ObservableProperty]
    public partial string LastFMUserName { get; set; }
    [ObservableProperty]
    public partial string LastFMPassword { get; set; }
    LastfmClient clientLastFM;
    [RelayCommand]
    public async Task<bool> LogInToLastFMWebsite(bool isSilent)
    {
        return await LastFMUtils.LogInToLastFMWebsite(LastFMUserName, LastFMPassword, isSilent);
        
    }

    public ParseLiveQueryClient LiveQueryClient { get; set; }

    public void SetupLiveQueries()
    {
        
        try
        {
            LiveQueryClient = new ParseLiveQueryClient();
            var SongQuery = ParseClient.Instance.GetQuery("SongModelView");
            var subscription = LiveQueryClient.Subscribe(SongQuery);
            //var DeviceStatusQuery = ParseClient.Instance.GetQuery("DeviceStatus");
            //var subToDeviceStatus = LiveQueryClient.Subscribe(DeviceStatusQuery); //added this, now i need to handle its updates too

            

            LiveQueryClient.OnConnected.
                Subscribe(_ =>
                {
                    Debug.WriteLine("Connected to LiveQuery Server");
                });

            LiveQueryClient.OnError.
                Subscribe(ex =>
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                });

            LiveQueryClient.OnSubscribed.
                Subscribe(sub =>
                {
                    Debug.WriteLine($"Subscribed to {sub.requestId}");
                });

            LiveQueryClient.OnObjectEvent
                .Where(e => e.evt == Subscription.Event.Update)
                .Subscribe(e =>
                {
                    var objData = (e.objectData as Dictionary<string, object>);
                    
                    SongModelView song = new();
                    
                    song = ObjectHelper.MapFromDictionary<SongModelView>(objData!);
                    CurrentQueue = 0;
                    //if (song.LocalDeviceId == SelectedSongToOpenBtmSheet.LocalDeviceId)
                    //{
                    //    GeneralStaticUtilities.RunFireAndForget(PauseSong(), ex =>
                    //    {
                    //        // Log or handle the exception as needed
                    //        Debug.WriteLine($"Task error: {ex.Message}");
                    //    });                        
                    //}
                    //else
                    //{
                    //    GeneralStaticUtilities.RunFireAndForget(PlaySong(song), ex =>
                    //    {
                    //        // Log or handle the exception as needed
                    //        Debug.WriteLine($"Task error: {ex.Message}");
                    //    });                        
                    //}

                });
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public void LogInToLastFMClientLocal()
    {
        var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);


        clientLastFM = new LastfmClient(APIKeys.LASTFM_API_KEY, APIKeys.LASTFM_API_SECRET)
        {
            Cache = new FileRequestCache(Path.Combine(localPath, "cache"))
        };

        return;

    }
    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track LastFMTrackInfoToSave { get; set; }
    public async Task FetchOnLastFM() //0 == song, 1 == artist, 2 = album. will rework this
    {
        LyricsSearchSongTitle ??= SelectedSongToOpenBtmSheet.Title;
        LyricsSearchArtistName ??= SelectedSongToOpenBtmSheet.ArtistName;
        LyricsSearchAlbumName ??= SelectedSongToOpenBtmSheet.AlbumName;
        //if (LastfmClient.Session.Authenticated)
        //{
        if (clientLastFM is null)
        {
            LogInToLastFMClientLocal();
        }

        PagedResponse<Hqub.Lastfm.Entities.Track>? tracks = await clientLastFM!.Track.SearchAsync(LyricsSearchSongTitle, LyricsSearchArtistName);
        if (tracks != null && tracks.Count != 0)
        {
            LastfmTracks.Clear(); // Clear existing tracks
            foreach (var track in tracks)
            {
                LastfmTracks.Add(track);
            }
        }
        else
        {
            await Shell.Current.DisplayAlert("No Results", "No Results Found on Last FM", "OK");
            // Handle no results found
        }

        //}
    }
    public void SaveLastFMTrackInfo()
    {
        if (LastFMTrackInfoToSave is not null)
        {
            SelectedSongToOpenBtmSheet!.ArtistName = LastFMTrackInfoToSave.Artist.Name;
            SelectedSongToOpenBtmSheet.Title = LastFMTrackInfoToSave.Name;
            SelectedSongToOpenBtmSheet.AlbumName = LastFMTrackInfoToSave.Album.Name;
            SelectedSongToOpenBtmSheet.SongWiki = LastFMTrackInfoToSave.Wiki.Summary;
            SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
        }
    }
}