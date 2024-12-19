using DevExpress.Maui.Controls;
using Dimmer_MAUI.Utilities.OtherUtils.CustomControl.RatingsView.Models;
using Hqub.Lastfm.Cache;
using Parse.LiveQuery;
using System.Reflection;


namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
    //LastfmClient LastfmClient;
    [ObservableProperty]
    public partial UserModelView CurrentUser { get; set; } =new();
    [ObservableProperty]
    public partial ParseUser CurrentUserOnline { get; set; }

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

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<SongModelView> PrevCurrNextSongsCollection { get; set; } = new();

    SortingEnum CurrentSortingOption;
    [ObservableProperty]
    public partial int TotalNumberOfSongs { get; set; } = 0;
    [ObservableProperty]
    public partial string? TotalSongsSize { get; set; }
    [ObservableProperty]
    public partial string? TotalSongsDuration { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SynchronizedLyrics { get; set; } = new();
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
    public partial List<AlbumArtistGenreSongLinkView>? AllLinks { get; set; } = new();
    [ObservableProperty]
    public partial List<PlayDateAndCompletionStateSongLinkView>? AllPDaCStateLink { get; set; } = new();

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


        if (SongsMgtService.AllLinks is not null)
        {
            AllLinks = SongsMgtService.AllLinks.ToList();
        }

#if WINDOWS
        ToggleFlyout();
#endif
        CurrentUser = SongsMgtService.CurrentOfflineUser;
        SyncRefresh();
        
        LoadSongCoverImage();

        LoadData();

        _ = GetSecuredData();
        
    }

    partial void OnTemporarilyPickedSongChanging(SongModelView oldValue, SongModelView newValue)
    {
        Debug.WriteLine($"Old Ver {oldValue?.CoverImagePath} | New Ver {newValue?.CoverImagePath} , Song {TemporarilyPickedSong?.Title}");
        if (newValue is not null && string.IsNullOrEmpty(newValue.CoverImagePath))
        {
            newValue.CoverImagePath = null;
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

    partial void OnSynchronizedLyricsChanging(ObservableCollection<LyricPhraseModel>? oldValue, ObservableCollection<LyricPhraseModel>? newValue)
    {
        
        if (oldValue is not null && oldValue.Count > 0)
        {
            
        }
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
        
        AllLinks = SongsMgtService.AllLinks.ToList();
        AllPDaCStateLink = SongsMgtService.AllPlayDataAndCompletionStateLinks.ToList();
        GetAllArtists();
        GetAllAlbums();
        RefreshPlaylists();        
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
    CollectionView DisplayedSongsColView { get; set; }
    public void AssignCV(CollectionView cv)
    {
        DisplayedSongsColView = cv;
    }

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
                            }

                            PickedSong = TemporarilyPickedSong;
                            PickedSong.IsPlaying = false;
                            PickedSong.IsCurrentPlayingHighlight = false;

                            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
                                                        
                            AllSyncLyrics = null;
                            splittedLyricsLines = null;
                            
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
                            
                            break;
                        case MediaPlayerState.ShowPlayBtn:
                            PlayPauseIcon = MaterialRounded.Play_arrow;
                            IsPlaying = false;
                            break;
                        case MediaPlayerState.ShowPauseBtn:
                            IsPlaying = true;
                            PlayPauseIcon = MaterialRounded.Pause;
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

    public async void LoadLocalSongFromOutSideApp(List<string> filePath)
    {
        CurrentQueue = 2;
        await PlayBackService.PlaySelectedSongsOutsideAppAsync(filePath);
    }

    [ObservableProperty]
    bool isShellLoadingPage = false;
    [ObservableProperty]
    bool isViewingDifferentSong = false;

    [RelayCommand]
    public async Task NavToSingleSongShell()
    {
        IsShellLoadingPage = true;
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
                IsViewingDifferentSong = true;
                SynchronizedLyrics?.Clear();
        
            }      
        }
        var ee  = LyricsManagerService.GetSpecificSongLyrics(SelectedSongToOpenBtmSheet).ToObservableCollection();
        SynchronizedLyrics.Clear();
        foreach (var item in ee)
        {
            SynchronizedLyrics.Add(item);
        }


        SelectedSongToOpenBtmSheet.SyncLyrics = SynchronizedLyrics;
        SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
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

    public async Task<string> AfterSingleSongShellAppeared()
    {
        if (SelectedSongToOpenBtmSheet is null)
        {
            return string.Empty;
        }
        IsShellLoadingPage = false;
     
        CurrentPage = PageEnum.NowPlayingPage;
        if (!string.IsNullOrEmpty(SelectedSongToOpenBtmSheet.CoverImagePath) && !File.Exists(SelectedSongToOpenBtmSheet.CoverImagePath))
        {
            var coverImg = await LyricsManagerService
                .FetchAndDownloadCoverImage(SelectedSongToOpenBtmSheet.Title, SelectedSongToOpenBtmSheet.ArtistName!, SelectedSongToOpenBtmSheet.AlbumName!, SelectedSongToOpenBtmSheet);
            SongsMgtService.AllSongs
                .FirstOrDefault(x => x.LocalDeviceId == SelectedSongToOpenBtmSheet.LocalDeviceId)!.CoverImagePath = coverImg;
            return coverImg;
        }
        return string.Empty;
    }

    #region Loadings Region

    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    ObservableCollection<string> folderPaths;
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
    [RelayCommand]    
    public async Task PlaySong(SongModelView SelectedSong)
    {
        //_ = UpdateRelatedPlayingData(SelectedSong!);//always check if we already have the song's artist and other data loaded


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
            string? lyr = string.Join(Environment.NewLine, LyricsLines!.Select(line => $"{line.TimeStampText} {line.Text}"));
            if (lyr is not null)
            {
                if (LyricsManagerService.WriteLyricsToLyricsFile(lyr, TemporarilyPickedSong!, true))
                {
                    await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
                    CurrentViewIndex = 0;
                }
                LyricsManagerService.InitializeLyrics(lyr);
                SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == TemporarilyPickedSong!.LocalDeviceId)!.HasLyrics = true;
            }
        }
    }

    private bool _isLoading = false;
      

    private void SubscribetoDisplayedSongsChanges()
    {
        PlayBackService.NowPlayingSongs.Subscribe(songs =>
        {
            TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            DisplayedSongs?.Clear();
            DisplayedSongs = songs;  //.Take(20).ToObservableCollection();
            if (SongsMgtService.AllSongs is null)
            {
                return;
            }
            TotalNumberOfSongs = songs.Count;

            //ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;

        
    }
    partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    }

    [ObservableProperty]
    public partial CollectionView DesktopColView { get; set; }


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
    public partial string? LyricsSearchSongTitle { get; set; }
    [ObservableProperty]
    public partial string? LyricsSearchArtistName {get;set;}
    [ObservableProperty]
    public partial string? LyricsSearchAlbumName {get;set;}
    [ObservableProperty]
    public partial bool UseManualSearch { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Content?>? AllSyncLyrics {get;set;}
    [ObservableProperty]
    public partial bool IsFetchSuccessful {get;set;}= true;
    [ObservableProperty]
public partial bool IsFetching { get; set; } = false;
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

        (SelectedSongToOpenBtmSheet.HasSyncedLyrics, SelectedSongToOpenBtmSheet.SyncLyrics)= LyricsService.HasLyrics(SelectedSongToOpenBtmSheet);
        if (SelectedSongToOpenBtmSheet.HasSyncedLyrics)
        {
            IsFetchSuccessful = true;
            
        }

        //if (fromUI || SynchronizedLyrics?.Count < 1)
        //{
        AllSyncLyrics = new();
        (bool IsSuccessful, Content[]? contentData) result = await LyricsManagerService.FetchLyricsOnlineLrcLib(SelectedSongToOpenBtmSheet, true,manualSearchFields);
        
        AllSyncLyrics = result.contentData.ToObservableCollection();
        (IsFetchSuccessful, var e) = await LyricsManagerService.FetchLyricsOnlineLyrist(SelectedSongToOpenBtmSheet.Title, TemporarilyPickedSong.ArtistName);
        if (e is not null)
        {
            AllSyncLyrics.Add(e.FirstOrDefault());
        }

        await FetchOnLastFM();

        IsFetchSuccessful = result.IsSuccessful;
        
        //LyricsSearchSongTitle = null;
        //LyricsSearchArtistName = null;
        //LyricsSearchAlbumName = null;

        return IsFetchSuccessful;
    }
    [ObservableProperty]
    public partial List<string>? LinkToFetchSongCoverImage { get; set; } =new();
    
    public async Task ShowSingleLyricsPreviewPopup(Content cont, bool IsPlain)
    {
        var result = ((bool)await Shell.Current.ShowPopupAsync(new SingleLyricsPreviewPopUp(cont!, IsPlain, this)));
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
            SelectedSongToOpenBtmSheet.UnSyncLyrics = cont.PlainLyrics;
            
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.PlainLyrics, SelectedSongToOpenBtmSheet, isSync);
        }
        else
        {
            SelectedSongToOpenBtmSheet.HasLyrics = false;
            
            SelectedSongToOpenBtmSheet.HasSyncedLyrics = true;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.SyncedLyrics, SelectedSongToOpenBtmSheet, isSync);
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
        LyricsManagerService.InitializeLyrics(cont.SyncedLyrics);
        if (DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId== SelectedSongToOpenBtmSheet.LocalDeviceId) is not null)
        {
            DisplayedSongs!.FirstOrDefault(x => x.LocalDeviceId == SelectedSongToOpenBtmSheet.LocalDeviceId)!.HasLyrics = true;
        }
        //if (PlayBackService.CurrentQueue != 2)
        //{
        //    SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
        //}

    }

    [ObservableProperty]
    ObservableCollection<LyricPhraseModel>? lyricsLines = new();
    [RelayCommand]
    async Task CaptureTimestamp(LyricPhraseModel lyricPhraseModel)
    {
        var CurrPosition = CurrentPositionInSeconds;
        if (!IsPlaying)
        {
            await PlaySong(TemporarilyPickedSong);
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
    public async Task FetchSongCoverImage(SongModelView? song=null)
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
        var firstSong = DisplayedSongs.Where(x => x.LocalDeviceId == album.LocalDeviceId).FirstOrDefault();
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
            AllAlbums.FirstOrDefault(x => x.LocalDeviceId == album.LocalDeviceId).AlbumImagePath= await LyricsManagerService.FetchAndDownloadCoverImage(firstSong.Title, firstSong.ArtistName, firstSong.AlbumName,firstSong);
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
    int currentRepeatCount;
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
    ObservableCollection<SongModelView> backEndQ;

    [ObservableProperty]
    bool isAnimatingFav = false;
    System.Timers.Timer _showAndHideFavGif;
    [RelayCommand]
    public async Task RateSong(string value)
    {
        bool willBeFav = false;
        var rateValue = int.Parse(value);
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
            if (SelectedSongToOpenBtmSheet.Rating < 3 && rateValue > 3)
            {
                SelectedSongToOpenBtmSheet.Rating = rateValue;
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
                SelectedSongToOpenBtmSheet.Rating = rateValue;
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
            else if (SelectedSongToOpenBtmSheet.Rating < 3 && rateValue <= 3)
            {
                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsRemoveSong: true, playlistModel: favPlaylist);
            }
            else if (SelectedSongToOpenBtmSheet.Rating > 4 && rateValue <= 3)
            {
                await UpdatePlayList(SelectedSongToOpenBtmSheet, IsRemoveSong: true, playlistModel: favPlaylist);
            }
            else if (SelectedSongToOpenBtmSheet.Rating > 4 && rateValue > 4)
            {
                SongsMgtService.UpdateSongDetails(SelectedSongToOpenBtmSheet);
            }

        }
    

    public async Task<bool> GetSecuredData()
    {
        
        var Uname =  await SecureStorage.Default.GetAsync("ParseUsername");
        var uPass = await SecureStorage.Default.GetAsync("ParsePassWord");
        var uEmail = await SecureStorage.Default.GetAsync("ParseEmail");
        var lastFMUname = await SecureStorage.Default.GetAsync("LastFMUsername");
        var lastFMPass = await SecureStorage.Default.GetAsync("LastFMPassWord");

        CurrentUser.UserEmail = uEmail;
        CurrentUser.UserPassword = uPass;
        CurrentUser.UserName = Uname;
        LastFMUserName = lastFMUname;
        LastFMPassword = lastFMPass;
        if (string.IsNullOrWhiteSpace(Uname) || string.IsNullOrEmpty(uPass) && string.IsNullOrEmpty(uEmail)) //maybe i'm being too agressive here, but I'll see.
        {
            return false; //I saw lmao. best to not be agro since well, what if they just opened app?
        }

        _ = LogInToLastFMWebsite();

        if (string.IsNullOrEmpty(lastFMUname) || string.IsNullOrEmpty(lastFMPass))
        {

        }
        _ = LogInToLastFMWebsite();
        return true;
    }



    [ObservableProperty]
    BottomSheetState nowPlayBtmSheetState = BottomSheetState.Hidden;
    [RelayCommand]
    void ShowNowPlayingBtmSheet()
    {
        NowPlayBtmSheetState = BottomSheetState.FullExpanded;
    }

    [ObservableProperty]
    ObservableCollection<Hqub.Lastfm.Entities.Track> lastfmTracks = new();

    [ObservableProperty]
    string lastFMUserName;
    [ObservableProperty]
    string lastFMPassword;
    LastfmClient clientLastFM;
    [RelayCommand]
    public async Task LogInToLastFMWebsite()
    {
        clientLastFM = LastfmClient.Instance;
        if (!clientLastFM.Session.Authenticated)
        {
            //LoginBtn.IsEnabled = false;
            if (string.IsNullOrWhiteSpace(LastFMUserName) || string.IsNullOrWhiteSpace(LastFMPassword))
            {
                _ = Shell.Current.DisplayAlert("Error when logging to lastfm", "Username and Password are required.", "OK");
                return;
            }
            await clientLastFM.AuthenticateAsync(LastFMUserName, LastFMUserName);
            if (clientLastFM.Session.Authenticated)
            {
                
                var usr = await clientLastFM.User.GetInfoAsync(LastFMUserName);
                _ = SecureStorage.Default.SetAsync("LastFMUsername", usr.Name);
                _ = SecureStorage.Default.SetAsync("LastFMPassWord", LastFMPassword);
            }
        }
    }

    public async Task LogInToParseServer(string uname, string password)
    {
        if (CurrentUserOnline is not null)
        {
            if (await CurrentUserOnline.IsAuthenticatedAsync())
            {
                return;
            }
        }
        if (string.IsNullOrWhiteSpace(password))
        {
        }
        //LoginBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(uname) || string.IsNullOrWhiteSpace(password))
        {
            await Shell.Current.DisplayAlert("Error Online Logging", "Username and Password are required.", "OK");
            return;
        }

        try
        {
            var oUser = await ParseClient.Instance.LogInWithAsync(uname.Trim(), password.Trim()).ConfigureAwait(false);
            SongsMgtService.CurrentOfflineUser.UserPassword = password;
            CurrentUserOnline = oUser;
            CurrentUser.IsAuthenticated = true;
            //await Shell.Current.DisplayAlert("Success !", $"Welcome Back !", "OK"); looks like an issue with parse funnily, I can't exactl reproduce it.

            _ = SecureStorage.Default.SetAsync("ParseUsername", CurrentUserOnline.Username);
            _ = SecureStorage.Default.SetAsync("ParsePassWord", password);
            _ = SecureStorage.Default.SetAsync("ParseEmail", CurrentUser.UserEmail!);

            SetupLiveQueries();
        }
        catch (Exception ex)
        {
            CurrentUser!.IsAuthenticated = false;
            await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");

        }
    }

    public ParseLiveQueryClient LiveQueryClient { get; set; }

    public void SetupLiveQueries()
    {
        return;
        try
        {
            LiveQueryClient = new();
            var SongQuery = ParseClient.Instance.GetQuery("SongModelView");
            var subscription = LiveQueryClient.Subscribe(SongQuery);

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
                .Subscribe(async e =>
                {
                    SongModelView song = new();
                    var objData = (e.objectDictionnary as Dictionary<string, object>);

                    song = ObjectMapper.MapFromDictionary<SongModelView>(objData!);
                    CurrentQueue=0;
                    if (song.LocalDeviceId == SelectedSongToOpenBtmSheet.LocalDeviceId)
                    {
                        _ = PauseSong();
                    }
                    else
                    {
                        _ = PlaySong(song);
                    }

                });
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    [RelayCommand]
    void LogInToLastFMClientLocal()
    {
        var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);


        clientLastFM = new LastfmClient(APIKeys.LASTFM_API_KEY, APIKeys.LASTFM_API_SECRET)
        {
            Cache = new FileRequestCache(Path.Combine(localPath, "cache"))
        };
        //await LastFmService.Authenticate();

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
public static class ObjectMapper
{
    /// <summary>
    /// Maps values from a dictionary to an instance of type T.
    /// Excludes ObjectId fields and skips ParseObjects.
    /// Handles nullable types and performs safe type conversions.
    /// Logs any keys that don't match properties in T.
    /// </summary>
    /// <typeparam name="T">The type of the target model.</typeparam>
    /// <param name="source">The source dictionary from Parse.</param>
    /// <returns>An instance of T with mapped values.</returns>
    public static T MapFromDictionary<T>(IDictionary<string, object> source) where T : new()
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        // Create an instance of T
        T target = new T();

        // Get all writable properties of T, case-insensitive
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        // Track unmatched keys
        List<string> unmatchedKeys = new();

        foreach (var kvp in source)
        {
            // Skip ObjectId fields
            if (kvp.Key.Equals("objectId", StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Equals("ObjectId", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (properties.TryGetValue(kvp.Key, out var property))
            {
                try
                {
                    if (kvp.Value == null)
                    {
                        // Assign null to nullable types or reference types
                        if (IsNullable(property.PropertyType))
                        {
                            property.SetValue(target, null);
                        }
                        // Else, skip assigning null to non-nullable value types
                    }
                    else
                    {
                        Type targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                        // If the target type is an enum, attempt to parse it
                        if (targetType.IsEnum)
                        {
                            var enumValue = Enum.Parse(targetType, kvp.Value.ToString());
                            property.SetValue(target, enumValue);
                        }
                        // Handle special conversions if necessary
                        else if (targetType == typeof(DateTime))
                        {
                            if (kvp.Value is string dateString && DateTime.TryParse(dateString, out DateTime parsedDate))
                            {
                                property.SetValue(target, parsedDate);
                            }
                            else if (kvp.Value is DateTime dateTimeValue)
                            {
                                property.SetValue(target, dateTimeValue);
                            }
                            else
                            {
                                Debug.WriteLine($"Cannot convert value '{kvp.Value}' to DateTime for property '{property.Name}'.");
                            }
                        }
                        else if (targetType == typeof(int))
                        {
                            // Handle cases where source is double but target is int
                            if (kvp.Value is double doubleValue)
                            {
                                property.SetValue(target, Convert.ToInt32(doubleValue));
                            }
                            else
                            {
                                var convertedValue = Convert.ChangeType(kvp.Value, targetType);
                                property.SetValue(target, convertedValue);
                            }
                        }
                        else if (targetType == typeof(int?))
                        {
                            // Handle nullable int separately
                            if (kvp.Value is double doubleValue)
                            {
                                property.SetValue(target, (int?)Convert.ToInt32(doubleValue));
                            }
                            else
                            {
                                var convertedValue = Convert.ChangeType(kvp.Value, targetType);
                                property.SetValue(target, convertedValue);
                            }
                        }
                        else
                        {
                            // General conversion for other types
                            var convertedValue = Convert.ChangeType(kvp.Value, targetType);
                            property.SetValue(target, convertedValue);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to set property '{property.Name}' with value '{kvp.Value}': {ex.Message}");
                    // Optionally, log more details or handle specific exceptions
                }
            }
            else
            {
                // Log unmatched keys
                unmatchedKeys.Add(kvp.Key);
            }
        }

        // Log keys that don't match
        if (unmatchedKeys.Count > 0)
        {
            Debug.WriteLine("Unmatched Keys:");
            foreach (var key in unmatchedKeys)
            {
                Debug.WriteLine($"- {key}");
            }
        }

        return target;
    }

    /// <summary>
    /// Checks if a type is nullable.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if nullable, else false.</returns>
    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
    }
}
