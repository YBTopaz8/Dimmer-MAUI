
#if WINDOWS
using System.Diagnostics;
using TView = YB.MauiDataGridView.TableView;
#endif
namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
#if WINDOWS

    public TView MyTableView { get; set; }
#endif
    //LastfmClient LastfmClient;
    [ObservableProperty]
    public partial bool IsFlyoutPresented { get; set; } = false;
    [ObservableProperty]
    public partial UserModelView? CurrentUser { get; set; }
    [ObservableProperty]
    public partial ParseUser? CurrentUserOnline { get; set; }

    [ObservableProperty]
    public partial FlyoutBehavior ShellFlyoutBehavior { get; set; } = FlyoutBehavior.Disabled;    
    
    [ObservableProperty]
    public partial SongModelView? PickedSong { get; set; } = null;// I use this a lot with the collection view, mostly to scroll

    [ObservableProperty]
    public partial SongModelView? TemporarilyPickedSong { get; set; } = null; 
    [ObservableProperty]
    public partial SongModelView? MySelectedSong { get; set; } = null;
    [ObservableProperty]
    public partial SongModelView? NextSong { get; set; } = null;

    [ObservableProperty]
    public partial double CurrentPositionPercentage { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> PartOfNowPlayingSongs { get; set; } = new();
    public CollectionView PartOfNowPlayingSongsCV { get; set; } 
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

    public void DoRefreshDependingOnPage()
    {
        //CurrentPositionInSeconds = 0;
        //CurrentPositionPercentage = 0;
        LyricsSearchSongTitle = MySelectedSong?.Title;
        LyricsSearchArtistName = MySelectedSong?.ArtistName;
        LyricsSearchAlbumName = MySelectedSong?.AlbumName;
        
        LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();
        PartOfNowPlayingSongs?.Clear(); 
        
        
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
    public void UpdateContextMenuData(SongModelView? mySelectedSong, ObservableCollection<SongModelView>? MiniQueue=null)
    {
        if (DisplayedSongs == null || DisplayedSongs.Count == 0 || mySelectedSong == null)
        {
            PartOfNowPlayingSongs = new ObservableCollection<SongModelView>();
            return;
        }

        // **1. Check if mySelectedSong is already in PartOfNowPlayingSongs AND get its index**
        int selectedSongIndexInCurrentList = -1;
        if (PartOfNowPlayingSongs != null) // Check if PartOfNowPlayingSongs is not null
        {
            for (int i = 0; i < PartOfNowPlayingSongs.Count; i++)
            {
                if (PartOfNowPlayingSongs[i] == mySelectedSong) // Again, ensure proper Equals or unique ID comparison
                {
                    selectedSongIndexInCurrentList = i;
                    break;
                }
            }
        }

        // **2. Check if index is in the "edge" ranges (0-10 or 90-101, assuming max 101 size)**
        bool shouldRecenter = false;
        int edgeMargin = 10; // Define the margin for "edge" (0-10 and from 101-10 down)
        int listSize = PartOfNowPlayingSongs?.Count ?? 0; // Get current list size, handle null case

        if (selectedSongIndexInCurrentList != -1 && listSize > 0) // Song is in the current list
        {
            if (selectedSongIndexInCurrentList <= edgeMargin || selectedSongIndexInCurrentList >= Math.Max(0, listSize - 1 - edgeMargin))
            {
                shouldRecenter = true; // Yes, re-center because it's at the edge
            }
        }


        if (MiniQueue is null)
        {
            MiniQueue = DisplayedSongs;
        }

        // **3. (If shouldRecenter is true) -  Regenerate PartOfNowPlayingSongs (the original logic)**
        int selectedSongIndex = -1; // Index in the *full* DisplayedSongs list
        for (int i = 0; i < MiniQueue.Count; i++)
        {
            if (MiniQueue[i] == mySelectedSong)
            {
                selectedSongIndex = i;
                break;
            }
        }

        if (selectedSongIndex == -1)
        {
            PartOfNowPlayingSongs = new ObservableCollection<SongModelView>();
            Debug.WriteLine("Warning: MySelectedSong not found in MiniQueue list (even in re-center logic!).");
            return;
        }

        int desiredChunkSize = 101;
        int centerIndex = desiredChunkSize / 2;

        int startIndex = Math.Max(0, selectedSongIndex - centerIndex);
        int endIndex = Math.Min(MiniQueue.Count - 1, startIndex + desiredChunkSize - 1);

        int actualChunkSize = endIndex - startIndex + 1;
        if (actualChunkSize < desiredChunkSize)
        {
            int difference = desiredChunkSize - actualChunkSize;
            startIndex = Math.Max(0, startIndex - difference);
            endIndex = Math.Min(MiniQueue.Count - 1, startIndex + desiredChunkSize - 1);
        }


        PartOfNowPlayingSongs = new ObservableCollection<SongModelView>();
        for (int i = startIndex; i <= endIndex; i++)
        {
            PartOfNowPlayingSongs.Add(MiniQueue[i]);
        }
        try
        {

            if (PartOfNowPlayingSongsCV is not null && CurrentAppState == AppState.OnForeGround)
            {
                PartOfNowPlayingSongsCV.ItemsSource = PartOfNowPlayingSongs;
                PartOfNowPlayingSongsCV.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Start, false);
                Debug.WriteLine("Context menu list re-centered because MySelectedSong was at the edge.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Weird bug "+ex.Message);
        }
    }
    public CollectionView? QueueCV { get; set; }

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
    
    [ObservableProperty]
    public partial CollectionView DisplayedSongsColView { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> DisplayedSongs { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> NowPlayingSongsUI { get; set; }
   
    
   
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
            if (MySelectedSong is null)
            {
                MySelectedSong = TemporarilyPickedSong;
            }
            if (string.IsNullOrEmpty(TemporarilyPickedSong.FilePath))
            {
                return;
            }
            if (MySelectedSong != TemporarilyPickedSong)
            {        
                SynchronizedLyrics?.Clear();
            }      
        }
        if (MySelectedSong is null)
        {
            return;
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
        

        if (selectedSong != null)
        {
            selectedSong.IsCurrentPlayingHighlight = false;
            MySelectedSong = selectedSong;

            if (CurrentPage == PageEnum.PlaylistsPage && DisplayedSongsFromPlaylist != null)
            {
                PlayBackService.ReplaceAndPlayQueue(DisplayedSongsFromPlaylist.ToList(), playImmediately: false); // Set the queue
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist);
            }
            else if (CurrentPage == PageEnum.FullStatsPage)
            {
                // Assuming TopTenPlayedSongs is available
                var topTenSongs = Enumerable.Empty<SongModelView>().ToList(); // Replace with your actual logic
                PlayBackService.ReplaceAndPlayQueue(topTenSongs, playImmediately: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist); // Or a more appropriate source
            }
            else if ((CurrentPage == PageEnum.SpecificAlbumPage || CurrentPage == PageEnum.AllArtistsPage) && AllArtistsAlbumSongs != null)
            {
                PlayBackService.ReplaceAndPlayQueue(AllArtistsAlbumSongs.ToList(), playImmediately: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.Playlist); // Or Album source
            }
            else if (IsOnSearchMode && FilteredSongs != null)
            {
                PlayBackService.ReplaceAndPlayQueue(FilteredSongs.ToList(), playImmediately: false);
                PlayBackService.PlaySong(selectedSong, PlaybackSource.HomePage); // Or Search source
            }
            else // Default playing on the main page (HomePage)
            {
                PlayBackService.ReplaceAndPlayQueue(DisplayedSongs.ToList(), playImmediately: false);
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
            if (CurrentPositionPercentage >= 0.98)
            {
                PlaySong(TemporarilyPickedSong);
                return;
            }
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
        if (TemporarilyPickedSong is not null)
        {
            TemporarilyPickedSong!.IsCurrentPlayingHighlight = TemporarilyPickedSong is null;
        }
        IsOnLyricsSyncMode = false;
        SynchronizedLyrics?.Clear();
        PlayBackService.PlayNextSong(true);
    }

    
    private int _backPressCount = 0;

    [RelayCommand]
    void PlayPreviousSong()
    {
        PlayBackService.PlayPreviousSong(true);  // Double press: play previous song.
    }
    [RelayCommand]
    void DecreaseVolume()
    {
        PlayBackService.DecreaseVolume();
        VolumeSliderValue = (PlayBackService.VolumeLevel*100);
    }
    [RelayCommand]
    void IncreaseVolume()
    {
        PlayBackService.IncreaseVolume();
        VolumeSliderValue = (PlayBackService.VolumeLevel * 100);
    }

    [ObservableProperty]
    public partial double CurrentPositionInSeconds { get; set; }
    public void SeekSongPosition(LyricPhraseModel? lryPhrase = null, double currPosPer=0)
    {
        if (lryPhrase is not null)
        {

            CurrentPositionInSeconds = lryPhrase.TimeStampMs * 0.001;
            PlayBackService.SeekTo(CurrentPositionInSeconds);
            return;
        }
        if (TemporarilyPickedSong is null)
        {
            if (MySelectedSong is not null)
            {
                TemporarilyPickedSong = MySelectedSong;
            }
        }
        if (currPosPer !=0 )
        {
#if WINDOWS
            CurrentPositionInSeconds = currPosPer * TemporarilyPickedSong.DurationInSeconds;
#elif ANDROID
            CurrentPositionInSeconds = currPosPer;
#endif
        }
        PlayBackService.SeekTo(CurrentPositionInSeconds);
    }

    [RelayCommand]
    void ChangeVolume()
    {
        if (VolumeSliderValue >1 || VolumeSliderValue<0)
        {
            return;
        }
        PlayBackService.ChangeVolume(VolumeSliderValue);
        
    }
    [ObservableProperty]
    public partial bool IsContextMenuOpened { get; set; } = false;
    [ObservableProperty]
    public partial int ContextMenuOpenedHeight { get; set; } = 0;
    public async Task ShowHideContextMenuFromBtmBar(View callerView) // Pass callerView as a parameter
    {
        double windowHeight = Shell.Current.Window?.Height ?? DeviceDisplay.MainDisplayInfo.Height;
        double targetTranslationYPageCaller = -windowHeight * 0.5; // Target translation for pageCaller (move up - you can adjust or remove this if only height animation is desired)
        double desiredCallerViewHeight = 50; // **Define your desired full height for callerView here (e.g., 50)**
        double collapsedCallerViewHeight = 0; // Height when context menu is closed (collapsed)

        
        var pageCaller = CurrentPageMainLayout; // Assuming CurrentPageMainLayout is your pageCaller ContentView

        if (pageCaller == null || callerView == null)
        {
            Debug.WriteLine("Error: pageCaller or callerView is null. Make sure they are properly referenced.");
            return; // Exit if either view is not found
        }

        if (IsContextMenuOpened)
        {
            callerView.HeightRequest = 105;
            IsContextMenuOpened = false;
            ContextMenuOpenedHeight = 0;
        }
        else
        {
            callerView.HeightRequest = 605;
            IsContextMenuOpened = true;
            ContextMenuOpenedHeight = 400;
        }
        
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
        CurrentRepeatMode = (int)PlayBackService.CurrentRepeatMode;
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
    public partial ObservableCollection<SongModelView> RecentlyAddedSongs { get; set; }
    public void LoadSongCoverImage()
    {
        
        if (SongsMgtService.AllSongs is null || SongsMgtService.AllSongs.Count < 1)
        {
            return;
        }
        if (DisplayedSongs is null || DisplayedSongs.Count < 1)
        {
            DisplayedSongs = SongsMgtService.AllSongs.ToObservableCollection();
            if (DisplayedSongs.Count < 1)
            {
                return;
            }
        }
        //RecentlyAddedSongs = GetXRecentlyAddedSongs(DisplayedSongs);

        if (DisplayedSongs is not null && DisplayedSongs.Count > 0)
        {
            //LastFifteenPlayedSongs = GetLastXPlayedSongs(DisplayedSongs).ToObservableCollection();
        }


        if (TemporarilyPickedSong is not null)
        {            
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
        PickedSong = TemporarilyPickedSong;
        PickedSong.IsCurrentPlayingHighlight = true;
        MySelectedSong = TemporarilyPickedSong;
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
                AllSyncLyrics.Clear();
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


    void SubscribeToPlayerStateChanges()
    {
        if (_playerStateSubscription != null)
            return; // Already subscribed
        
        _playerStateSubscription = PlayBackService.PlayerState
            .DistinctUntilChanged()
            .Subscribe(async state =>
            {
                switch (state)
                {
                    case MediaPlayerState.Playing:
                        IsPlaying = true;
                            
                    if (PlayBackService.CurrentlyPlayingSong is null)
                        break;
                    if (TemporarilyPickedSong is not null)
                    {

                        if (TemporarilyPickedSong == PlayBackService.CurrentlyPlayingSong)
                        {
                            return;
                        }
                        TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
                        DoRefreshDependingOnPage();
                        CurrentRepeatCount = PlayBackService.CurrentRepeatCount;
                    }

                    //await FetchSongCoverImage();

                    //if (CurrentUser is not null)
                    //{
                    //    await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);

                    //}
#if WINDOWS
                    //MyTableView.ScrollIntoView(TemporarilyPickedSong);
                    //MyTableView.
#endif
                    break;
                    case MediaPlayerState.Paused:
                        if (CurrentUser is not null)
                        {
                            await ParseStaticUtils.UpdateSongStatusOnline(TemporarilyPickedSong, CurrentUser.IsAuthenticated);
                        }
                        //TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                        //PickedSong = null;
                        //PickedSong = TemporarilyPickedSong;

                        IsPlaying = false;

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
                
            });

    }


    //partial void OnTemporarilyPickedSongChanging(SongModelView? value)
    //{

    //}
    
    private void DisplayedSongsColView_SelectionChanged(object? sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        MultiSelectSongs = e.CurrentSelection.Cast<SongModelView>().ToObservableCollection();
        
        ContextViewText = $"{DisplayedSongsColView.SelectedItems.Count} Song{(DisplayedSongsColView.SelectedItems.Count > 1 ? "s" : "")}/{SongsMgtService.AllSongs.Count} Selected";
        return;
    }



    partial void OnTemporarilyPickedSongChanging(SongModelView? oldValue, SongModelView? newValue)
    {
        if (newValue == null)
        {
            return;
        }

        if (MySelectedSong != null)
        {
            MySelectedSong.IsPlaying = false;
            MySelectedSong = newValue;
            MySelectedSong.IsCurrentPlayingHighlight = false;
            
        }
        if (PickedSong != null)
        {
            PickedSong.IsPlaying = false;
            PickedSong.IsCurrentPlayingHighlight = false;

            PickedSong = newValue;
            
        }


        if (string.IsNullOrEmpty(newValue.CoverImagePath))
        {
            newValue.CoverImagePath = "musicnoteslider.png";
            return;
        }

        if (!string.IsNullOrEmpty(newValue.CoverImagePath))
        {
            if (newValue.CoverImagePath == oldValue?.CoverImagePath)
            {
                if (oldValue.AlbumName != newValue.AlbumName)
                {
                    newValue.CoverImagePath = "musicnoteslider.png";
                }
            }
        }
    }
    //    if (newValue is not null)
    //    {
    //        if (IsPlaying)
    //        {
    //            newValue.IsCurrentPlayingHighlight = true;
    //        }
    //        else
    //        {
    //            newValue.IsCurrentPlayingHighlight = false;
    //        }
    //    }
    //}
    private void SubscribetoDisplayedSongsChanges()
    {
        PlayBackService.NowPlayingSongs.Subscribe(songs =>
        {
            //UpdateContextMenuData(TemporarilyPickedSong, songs);
            //PartOfNowPlayingSongs = songs.ToObservableCollection();
            //if (PartOfNowPlayingSongsCV is not null)
            //{
            //    PartOfNowPlayingSongsCV.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Start, false);
            //}
            //TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;


            //if (AllLinks is null || AllLinks.Count < 0)
            //{
            //    if (SongsMgtService.AllLinks is not null && SongsMgtService.AllLinks.Count > 0)
            //    {
            //        AllLinks = SongsMgtService.AllLinks;
            //    }

            //}
            //MainThread.BeginInvokeOnMainThread(() =>
            //{
            //    NowPlayingSongsUI = songs;
            //    if (DisplayedSongs is null)
            //    {
            //        return;
            //    }
            //    if (DisplayedSongsColView is null)
            //    {
            //        return;
            //    }
            //    DisplayedSongsColView.ItemsSource = songs;
            //    TotalNumberOfSongs = songs.Count;
            //    //ReloadSizeAndDuration();
            //});

            //ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;


    }

    //partial void OnDisplayedSongsChanging(ObservableCollection<SongModelView> oldValue, ObservableCollection<SongModelView> newValue)
    //{
    //    Debug.WriteLine($"Old {oldValue?.Count} | New {newValue?.Count}");
    //}
  


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
                    
                    song = ObjectMapper.MapFromDictionary<SongModelView>(objData!);
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

        var ind = PartOfNowPlayingSongs.IndexOf(TemporarilyPickedSong);
        if (ind == 0)
        {
            return;
        }
        PartOfNowPlayingSongs.Insert(ind + 1, song);
        if (PartOfNowPlayingSongsCV is not null && CurrentAppState == AppState.OnForeGround)
        {
            PartOfNowPlayingSongsCV.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Start, false);
            Debug.WriteLine("Context menu list re-centered because MySelectedSong was at the edge.");
        }
        var songs = PartOfNowPlayingSongs.ToList();
        if (song is null)
        {
            return;
        }
        PlayBackService.ReplaceAndPlayQueue(songs);
    }


    partial void OnIsFlyoutPresentedChanging(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            UpdateContextMenuData(MySelectedSong);            
        }

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