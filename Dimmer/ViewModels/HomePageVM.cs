﻿namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
    //LastfmClient LastfmClient;
    [ObservableProperty]
    public partial UserModelView? CurrentUser { get; set; }
    [ObservableProperty]
    public partial ParseUser? CurrentUserOnline { get; set; }

    [ObservableProperty]
    public partial FlyoutBehavior ShellFlyoutBehavior { get; set; } = FlyoutBehavior.Disabled;
    [ObservableProperty]
    public partial bool IsFlyOutPaneOpen { get; set; } = false;
    
    [ObservableProperty]
    public partial SongModelView? PickedSong { get; set; } = null;// I use this a lot with the collection view, mostly to scroll

    [ObservableProperty]
    public partial SongModelView? TemporarilyPickedSong { get; set; } = null;
    [ObservableProperty]
    public partial SongModelView? NextSong { get; set; } = null;

    [ObservableProperty]
    public partial double CurrentPositionPercentage { get; set; }
    
    //[ObservableProperty]
    //public partial ObservableCollection<SongModelView> PrevCurrNextSongsCollection { get; set; } = new();
    [ObservableProperty]
    public partial SortingEnum CurrentSortingOption { get; set; }
    [ObservableProperty]
    public partial int TotalNumberOfSongs { get; set; } = 0;
    [ObservableProperty]
    public partial string? TotalSongsSize { get; set; }
    [ObservableProperty]
    public partial string? TotalSongsDuration { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModel> SynchronizedLyrics { get; set; } = Enumerable.Empty<LyricPhraseModel>().ToObservableCollection();
    [ObservableProperty]
    public partial LyricPhraseModel? CurrentLyricPhrase { get; set; } = null;

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
        //ToggleFlyout();
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
        if (CurrentUser is null)
        {
            return;
        }
        if (CurrentUserOnline is null || string.IsNullOrEmpty(CurrentUserOnline.Username) )
        {

            if( await LogInParseOnline(true))
            {
                if (CurrentUser.IsAuthenticated)
                {

                    //SetupLiveQueries();
                }

            }            
            
            //LastFMUtils.QuickLoginToLastFM();
            return;
            /*
            */
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
    public List<PlayDataLink> AllPlayDataLinks { get; internal set; }
    List<AlbumArtistGenreSongLinkView> AllLinks { get; set; }
    public void SyncRefresh()
    {
        if (SongsMgtService.AllArtists is null || SongsMgtService.AllAlbums is null || SongsMgtService.AllLinks is null)
        {
            return;
        }
       PlayBackService.FullRefresh();
        AllArtists = SongsMgtService.AllArtists.ToObservableCollection();
        AllAlbums = SongsMgtService.AllAlbums.ToObservableCollection();
        
        AllLinks = SongsMgtService.AllLinks;
        AllPlayDataLinks = SongsMgtService.AllPlayDataLinks;

        RefreshPlaylists();        
    }

    void DoRefreshDependingOnPage()
    {
        LyricsSearchSongTitle = MySelectedSong.Title;
        LyricsSearchArtistName = MySelectedSong.ArtistName;
        LyricsSearchAlbumName = MySelectedSong.AlbumName;
        LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();

        CurrentLyricPhrase = new LyricPhraseModel() { Text = "" };
        AllSyncLyrics = Enumerable.Empty<Content>().ToObservableCollection();
        splittedLyricsLines = null;
        
        switch (CurrentPage)
        {
            case PageEnum.MainPage:
                SearchPlaceHolder = "Type to search...";
                break;
            case PageEnum.NowPlayingPage:
                //LastfmTracks.Clear();
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
            case PageEnum.AllArtistsPage:
                SearchPlaceHolder = "Search All Artists...";
                break;
            
            case PageEnum.AllAlbumsPage:
                SearchPlaceHolder = "Search All Albums...";
                LoadAlbumsPage();
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
    }

    private void LoadAlbumsPage()
    {
        if (TemporarilyPickedSong is null || MySelectedSong is null)
        {
            return;
        }
        var pickedSong = TemporarilyPickedSong;
        if (TemporarilyPickedSong != MySelectedSong)
            pickedSong = MySelectedSong;

        SelectedAlbumOnAlbumPage = GetAlbumFromSongID(pickedSong.LocalDeviceId!).FirstOrDefault();
        //SelectedArtistOnAlbumPage = GetAllArtistsFromSongID(pickedSong.LocalDeviceId!).FirstOrDefault();
        var SpecificAlbumSongs = GetAllSongsFromAlbumID(SelectedAlbumOnAlbumPage!.LocalDeviceId);

    }

    [ObservableProperty]
    public partial bool IsMultiSelectOn { get; set; }
    CollectionView DisplayedSongsColView { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> NowPlayingSongsUI { get; set; }
   

    CollectionView? SyncLyricsCV { get; set; }
    public void AssignSyncLyricsCV(CollectionView cv)
    {
        SyncLyricsCV = cv;
    }
    [ObservableProperty]
    public partial string LoadingSongsText { get; set; }
    public void SetLoadingProgressValue(double newValue)
    {
        if (newValue<100)
        {
            LoadingSongsText = $"Loading {newValue}% done";
            return;
        }
        LoadingSongsText = $"Loading Completed !";

    }

    public void LoadLocalSongFromOutSideApp(List<string> filePath)
    {
        CurrentQueue = 2;
        PlayBackService.PlaySelectedSongsOutsideApp(filePath);
    }

    [RelayCommand]
    public async Task NavToSingleSongShell()
    {
        
        CurrentViewIndex = 0;
        if (CurrentPage == PageEnum.NowPlayingPage)
        {
            return;
        }
#if WINDOWS
        
        await Shell.Current.GoToAsync(nameof(SingleSongShellPageD));
        
        await AfterSingleSongShellAppeared();
        
        //ToggleFlyout();
        
#elif ANDROID
        var currentPage = Shell.Current.CurrentPage;

        if (currentPage.GetType() != typeof(SingleSongShell))
        {
            await Shell.Current.GoToAsync(nameof(SingleSongShell), true);
        }
#endif
        if (TemporarilyPickedSong is not null)
        {
            if (string.IsNullOrEmpty(TemporarilyPickedSong.FilePath))
            {
                return;
            }
            if (MySelectedSong != TemporarilyPickedSong)
            {        
                SynchronizedLyrics?.Clear();
            }      
        }
        if (MySelectedSong.SyncLyrics is null || MySelectedSong.SyncLyrics.Count < 1)
        {            
            var ee = LyricsManagerService.GetSpecificSongLyrics(MySelectedSong).ToObservableCollection();
            SynchronizedLyrics?.Clear();
            foreach (var item in ee)
            {
                SynchronizedLyrics?.Add(item);
            }

            MySelectedSong.SyncLyrics = SynchronizedLyrics!;
            SongsMgtService.UpdateSongDetails(MySelectedSong);
        }
        if (SongPickedForStats is null)
        {
            SongPickedForStats = new()
            {
                Song = MySelectedSong
            };
        }
        else
        {
            SongPickedForStats.Song = MySelectedSong;
        }
        
    }

    public async Task AfterSingleSongShellAppeared()
    {
        if (MySelectedSong is null)
        {
            return ;
        }
        
     
        CurrentPage = PageEnum.NowPlayingPage;
        if (!string.IsNullOrEmpty(MySelectedSong.CoverImagePath) && !File.Exists(MySelectedSong.CoverImagePath))
        {
            var coverImg = await LyricsManagerService
                .FetchAndDownloadCoverImage(MySelectedSong.Title!, MySelectedSong.ArtistName!, MySelectedSong.AlbumName!, MySelectedSong);
            SongsMgtService.AllSongs
                .FirstOrDefault(x => x.LocalDeviceId == MySelectedSong.LocalDeviceId)!.CoverImagePath = coverImg;
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
        //bool res = Shell.Current.DisplayAlert("Select Song", "Sure?", "Yes", "No");
        //if (!res)
        //{
        //    return;
        //}

        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;
#if ANDROID
        var  status = await Permissions.RequestAsync<CheckPermissions>();
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
        //LoadSongsFromFolders();//FullFolderPaths);
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
            bool loadSongsResult = await PlayBackService.LoadSongsFromFolder(FolderPaths.ToList());
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

    [ObservableProperty]
    public partial PageEnum CurrentPage { get; set; }
    #endregion

    #region Playback Control Region


    async void UpdateRelatedPlayingData(SongModelView song)
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

                    if(await PlayBackService.LoadSongsFromFolder(load))
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

    [ObservableProperty]
    public partial List<SongModelView> FilteredSongs { get; set; } = new();
    [ObservableProperty]
    public partial ImageSource PlayPauseIcon { get; set; } = "playdark.svg";    
    [ObservableProperty]
    public partial bool IsPreviewing { get; set; } = false;

    public void PlaySong(SongModelView selectedSong, bool isPrevieww = false)
    {
        if (isPrevieww)
        {
            IsPreviewing = isPrevieww;
            CurrentPage = PageEnum.FullStatsPage;
            PlayBackService.PlaySong(selectedSong, isPreview: true);
            return;
        }

        TemporarilyPickedSong = selectedSong;
        TemporarilyPickedSong.IsCurrentPlayingHighlight = false;

        if (selectedSong != null)
        {
            selectedSong.IsCurrentPlayingHighlight = false;
            MySelectedSong = selectedSong;

            if (CurrentPage == PageEnum.PlaylistsPage && DisplayedSongsFromPlaylist != null)
            {
                PlayBackService.ReplaceAndPlayQueue(DisplayedSongsFromPlaylist.ToList(), playFirst: false); // Set the queue
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist);
            }
            else if (CurrentPage == PageEnum.FullStatsPage)
            {
                // Assuming TopTenPlayedSongs is available
                var topTenSongs = Enumerable.Empty<SongModelView>().ToList(); // Replace with your actual logic
                PlayBackService.ReplaceAndPlayQueue(topTenSongs, playFirst: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist); // Or a more appropriate source
            }
            else if ((CurrentPage == PageEnum.SpecificAlbumPage || CurrentPage == PageEnum.AllArtistsPage) && AllArtistsAlbumSongs != null)
            {
                PlayBackService.ReplaceAndPlayQueue(AllArtistsAlbumSongs.ToList(), playFirst: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist); // Or Album source
            }
            else if (IsOnSearchMode && FilteredSongs != null)
            {
                PlayBackService.ReplaceAndPlayQueue(FilteredSongs.ToList(), playFirst: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.HomePage); // Or Search source
            }
            else // Default playing on the main page (HomePage)
            {
                PlayBackService.ReplaceAndPlayQueue(DisplayedSongs.ToList(), playFirst: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.HomePage);
            }
        }
        else
        {
            // Handle the case where selectedSong is null
            // Perhaps play the currently "picked" song?
        }
    }



    [RelayCommand]
    void PlayPauseSong()
    {
        if (IsPlaying)
        {
            PauseSong();
        }
        else
        {
            ResumeSong();
        }
    }
    [RelayCommand]

    public void PauseSong()
    {
        PlayBackService.PauseResumeSong(CurrentPositionInSeconds, true);
    }
    
    [RelayCommand]
    public void ResumeSong()
    {
        if (IsPreviewing)
        {
            return;
        }
        PlayBackService.PauseResumeSong(CurrentPositionInSeconds);
    }
    

    [RelayCommand]
    void StopSong()
    {
        PlayBackService.StopSong();
    }

    [RelayCommand]
    void PlayNextSong()
    {
        TemporarilyPickedSong!.IsCurrentPlayingHighlight= TemporarilyPickedSong is null;
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        PlayBackService.PlayNextSong(true);
    }

    [RelayCommand]
    void PlayPreviousSong()
    {
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        PlayBackService.PlayPreviousSong(true);
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

    [ObservableProperty]
    public partial double CurrentPositionInSeconds { get; set; }
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

    /// <summary>
    /// Toggles repeat mode between 0, 1, and 2
    ///  0 for repeat OFF
    ///  1 for repeat ALL
    ///  2 for repeat ONE
    /// </summary>

    [RelayCommand]
    void ToggleRepeatMode(bool IsCalledByUI = false)
    {
        CurrentRepeatMode = PlayBackService.CurrentRepeatMode;
        if (IsCalledByUI)
        {
            CurrentRepeatMode = PlayBackService.ToggleRepeatMode();
        }

        //switch (CurrentRepeatMode)
        //{
        //    case 1:
        //        RepeatImgSrc = ImageSource.FromFile("repoff.svg");
        //        break;
        //    case 2:
        //    case 4:
        //        RepeatImgSrc = ImageSource.FromFile("repeat1.svg");
        //        break;
        //    case 0:
        //        RepeatImgSrc = ImageSource.FromFile("repeatoff1.svg");
        //        break;
        //    default:
        //         break;
        //}

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
    
    }
    #endregion
    [ObservableProperty]
    public partial ObservableCollection<string> SearchFilters { get; set; } = new([ "Artist", "Album","Genre", "Rating"]);
    [ObservableProperty]
    public partial int Achievement { get; set; } =0;
    
    [ObservableProperty]
    public partial string SearchText { get; set; }

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
        
        if (SongsMgtService.AllSongs is null || SongsMgtService.AllSongs.Count < 1)
        {
            return;
        }
        DisplayedSongs = SongsMgtService.AllSongs.ToObservableCollection();
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
            MySelectedSong = TemporarilyPickedSong;
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
        //TemporarilyPickedSong.CoverImagePath = FetchSongCoverImage();
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
    public partial ObservableCollection<SingleSongStatistics> LastFifteenPlayedSongs { get; set; }

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
    
    public bool isFirstTimeOpeningApp = false;
#if ANDROID
    [RelayCommand]
    public async Task GrantPermissionsAndroid()
    {

    PermissionStatus status = await Permissions.RequestAsync<CheckPermissions>();
            
    isFirstTimeOpeningApp = false;
    GeneralStaticUtilities.RunFireAndForget(LoadSongsFromFolders(), e
        =>
    {
        Debug.WriteLine(e.Message);
    });
     
        await Shell.Current.GoToAsync("..");        
    
    }
#endif


    #region Subscriptions to Services

    private IDisposable _playerStateSubscription;
    [ObservableProperty]
    public partial bool IsPlaying { get; set; } = false;

    MediaPlayerState currentPlayerState;
    public void SetPlayerState(MediaPlayerState? state)
    {
        switch (state)
        {
            case MediaPlayerState.Playing:

                TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            
                PickedSong = TemporarilyPickedSong;
                MySelectedSong = TemporarilyPickedSong;
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
            case MediaPlayerState.DoneScanningData:
                //SyncRefresh();
                //SetLoadingProgressValue(100);
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
        PlayBackService.CurrentPosition.Subscribe( position =>
        {
            if (position.CurrentPercentagePlayed != 0)
            {
                CurrentPositionInSeconds = position.CurrentTimeInSeconds;
                CurrentPositionPercentage = position.CurrentPercentagePlayed;
                if (CurrentPositionPercentage >= 0.97 && IsPlaying && IsOnLyricsSyncMode)
                {
                    PauseSong();
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await SaveLyricsToLrcAfterSyncing();
                    });
                }
            }


        });
    }

    private bool IsLoading = false;

    [ObservableProperty]
    public partial ImageSource? PlayPauseImg { get; set; }

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
                            MySelectedSong = null;

                            if (PlayBackService.CurrentlyPlayingSong is null)
                                break;

                            TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;

                            if (PickedSong is not null)
                            {
                                PickedSong.IsPlaying = false;
                                PickedSong.IsCurrentPlayingHighlight = false;                                
                            }
                            PickedSong = TemporarilyPickedSong;                            

                            MySelectedSong = TemporarilyPickedSong;

                            IsPlaying = true;

                            if (DisplayedSongs?.Count > 1)
                            {
                                var ind = DisplayedSongs.IndexOf(TemporarilyPickedSong);
                                NextSong = DisplayedSongs.ElementAtOrDefault(ind + 1);
                            }
                            DoRefreshDependingOnPage();

                            CurrentRepeatCount = PlayBackService.CurrentRepeatCount;
                            
                            await FetchSongCoverImage();

                            if (CurrentUser is not null)
                            {
                                await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);

                            }
                            
                            break;
                        case MediaPlayerState.Paused:
                            if (CurrentUser is not null)
                            {
                                await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);
                            }
                            TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
                            PickedSong = null;
                            PickedSong = TemporarilyPickedSong;

                            IsPlaying = false;

                            PlayPauseImg = ImageSource.FromFile("pausedark.svg");
                            //PlayPauseIcon = MaterialRounded.Play_arrow;
                            break;
                        case MediaPlayerState.Stopped:
                            IsPlaying = false;
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
        //PlayBackService.NowPlayingSongs            
        //.Subscribe(songs =>
        //{
        //    TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
            
            
        //    if (AllLinks is null || AllLinks.Count < 0)
        //    {
        //        if (SongsMgtService.AllLinks is not null && SongsMgtService.AllLinks.Count > 0)
        //        {
        //            AllLinks = SongsMgtService.AllLinks;
        //        }
            
        //    }
        //    MainThread.BeginInvokeOnMainThread( () =>
        //    {
        //        NowPlayingSongsUI= songs;
        //        if (DisplayedSongs is null)
        //        {
        //            return;
        //        }
        //        if (DisplayedSongsColView is null)
        //        {
        //            return;
        //        }
        //        DisplayedSongsColView.ItemsSource = songs;
        //        TotalNumberOfSongs = songs.Count;
        //        //ReloadSizeAndDuration();
        //    });

        //    //ReloadSizeAndDuration();
        //});
        //IsLoadingSongs = false;

      
    }

    //partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    //{
    //    Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    //}
    partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    {
        Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    }


    private void SubscribeToSyncedLyricsChanges()
    {
        LyricsManagerService.SynchronizedLyricsStream.Subscribe(synchronizedLyrics =>
        {
            if (MySelectedSong is not null)
            {
                MySelectedSong.HasSyncedLyrics = synchronizedLyrics.Count > 0;
            }

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
    public partial string? LyricsSearchSongTitle { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? LyricsSearchArtistName {get;set;} = string.Empty;
    [ObservableProperty]
    public partial string? LyricsSearchAlbumName {get;set;}=string.Empty;
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
                LyricsSearchSongTitle = MySelectedSong.Title;
                LyricsSearchArtistName = MySelectedSong.ArtistName;
                LyricsSearchAlbumName = MySelectedSong.AlbumName;

                break;
            case 2:
                SongPickedForStats ??= new SingleSongStatistics();
                SongPickedForStats.Song = TemporarilyPickedSong;

                OpenEditableSongsTagsView();
                CurrentPage = PageEnum.FullStatsPage;
                ShowSingleSongStats(MySelectedSong);
                break;
            case 3:
                SelectedArtistOnArtistPage = GetAllArtistsFromSongID(MySelectedSong.LocalDeviceId!).FirstOrDefault();
                SelectedAlbumOnArtistPage = GetAlbumFromSongID(MySelectedSong.LocalDeviceId!).FirstOrDefault();

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
            else if (CurrentPage == PageEnum.AllArtistsPage)
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
        PickedSong = MySelectedSong;
#endif

        var result = ((int)await Shell.Current.ShowPopupAsync(new CustomRepeatPopup(CurrentRepeatMaxCount, PickedSong)));

        if (result > 0)
        {
            CurrentRepeatMode = 4;
            CurrentRepeatMaxCount = result;
            PlaySong(TemporarilyPickedSong);
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
        MySelectedSong = song;
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

    //[ObservableProperty]
    //public partial ObservableCollection<Hqub.Lastfm.Entities.Track> LastfmTracks { get; set; } = new();

    //[ObservableProperty]
    //public partial string? LastFMUserName { get; set; }
    //[ObservableProperty]
    //public partial string? LastFMPassword { get; set; }
    //LastfmClient clientLastFM;
    //[RelayCommand]
    //public async Task<bool> LogInToLastFMWebsite(bool isSilent)
    //{
    //    return LastFMUtils.LogInToLastFMWebsite(LastFMUserName, LastFMPassword, isSilent);        
    //}

    public ParseLiveQueryClient LiveQueryClient { get; set; }

    public void SetupLiveQueries()
    {        
        try
        {
            LiveQueryClient = new ParseLiveQueryClient();
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
                .Subscribe(e =>
                {
                    var objData = (e.objectData as Dictionary<string, object>);
                    
                    SongModelView song = new();
                    
                    song = ObjectHelper.MapFromDictionary<SongModelView>(objData!);
                    CurrentQueue = 0;
                   
                });
        }
        catch (Exception ex)
        {
            
        }
    }

    [ObservableProperty]
    public partial ImageSource? RepeatImgSrc { get; set; }

    [RelayCommand]
    public void AddNextInQueue(SongModelView song)
    {
        List<SongModelView> songs = [song];
        if (song is null)
        {
            return;
        }
        PlayBackService.AddToImmediateNextInQueue(songs);
    }




    //public void LogInToLastFMClientLocal()
    //{
    //    var localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);


    //    clientLastFM = new LastfmClient(APIKeys.LASTFM_API_KEY, APIKeys.LASTFM_API_SECRET)
    //    {
    //        Cache = new FileRequestCache(Path.Combine(localPath, "cache"))
    //    };

    //    return;

    //}
    //[ObservableProperty]
    //public partial Hqub.Lastfm.Entities.Track LastFMTrackInfoToSave { get; set; }
    //public async Task FetchOnLastFM() //0 == song, 1 == artist, 2 = album. will rework this
    //{
    //    LyricsSearchSongTitle ??= MySelectedSong.Title;
    //    LyricsSearchArtistName ??= MySelectedSong.ArtistName;
    //    LyricsSearchAlbumName ??= MySelectedSong.AlbumName;
    //    //if (LastfmClient.Session.Authenticated)
    //    //{
    //    if (clientLastFM is null)
    //    {
    //        LogInToLastFMClientLocal();
    //    }

    //    PagedResponse<Hqub.Lastfm.Entities.Track>? tracks = clientLastFM!.Track.SearchAsync(LyricsSearchSongTitle, LyricsSearchArtistName);
    //    if (tracks != null && tracks.Count != 0)
    //    {
    //        LastfmTracks.Clear(); // Clear existing tracks
    //        foreach (var track in tracks)
    //        {
    //            LastfmTracks.Add(track);
    //        }
    //    }
    //    else
    //    {
    //        Shell.Current.DisplayAlert("No Results", "No Results Found on Last FM", "OK");
    //        // Handle no results found
    //    }

    //    //}
    //}
    //public void SaveLastFMTrackInfo()
    //{
    //    if (LastFMTrackInfoToSave is not null)
    //    {
    //        MySelectedSong!.ArtistName = LastFMTrackInfoToSave.Artist.Name;
    //        MySelectedSong.Title = LastFMTrackInfoToSave.Name;
    //        MySelectedSong.AlbumName = LastFMTrackInfoToSave.Album.Name;
    //        MySelectedSong.SongWiki = LastFMTrackInfoToSave.Wiki.Summary;
    //        SongsMgtService.UpdateSongDetails(MySelectedSong);
    //    }
    //}
}