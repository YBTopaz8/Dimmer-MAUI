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
    public IDiscordRPC DiscordRPC { get; }

    [ObservableProperty]
    string unSyncedLyrics;
    [ObservableProperty]
    string localFilePath;

    public AppState CurrentAppState = AppState.OnForeGround;

    public HomePageVM(IPlaybackUtilsService PlaybackManagerService, IFolderPicker folderPickerService, IFileSaver fileSaver,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService, IArtistsManagementService artistMgtService,
                      IDiscordRPC discordRPC)
    {
        this.folderPicker = folderPickerService;
        FileSaver = fileSaver;
        PlayBackService = PlaybackManagerService;
        LyricsManagerService = lyricsService;
        SongsMgtService = songsMgtService;
        ArtistMgtService = artistMgtService;
        DiscordRPC = discordRPC;
        CurrentSortingOption = AppSettingsService.SortingModePreference.GetSortingPref();

        SubscribeToPlayerStateChanges();
        SubscribetoDisplayedSongsChanges();
        SubscribeToCurrentSongPosition();
        SubscribeToPlaylistChanges();

        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        LoadSongCoverImage();

        //DisplayedPlaylists = PlayBackService.AllPlaylists;
        TotalSongsDuration = PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;
        IsPlaying = false;
        ToggleShuffleState();
        ToggleRepeatMode();
        TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
        FolderPaths = AppSettingsService.MusicFoldersPreference.GetMusicFolders().ToObservableCollection();
        IsDRPCEnabled = AppSettingsService.DiscordRPCPreference.IsDiscordRPCEnabled;
        //AppSettingsService.MusicFoldersPreference.ClearListOfFolders();
        GetAllArtists();
        GetAllAlbums();
        RefreshPlaylists();
    }

    CollectionView? PageCV { get; set; }
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
            .Subscribe(state =>
            {
                
                if (TemporarilyPickedSong is not null)
                {

                    TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
                    TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
                        //} WHY does this exist??
                    if (PickedSong is not null)
                    PickedSong.IsPlaying = false;

                    PickedSong = TemporarilyPickedSong;
                    SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
                    switch (state)
                    {
                        case MediaPlayerState.Playing:
                            TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (IsShuffleOn)
                                {
                                    PageCV?.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Center, false);
                                }
                                else
                                {
                                    PageCV?.ScrollTo(TemporarilyPickedSong, null, ScrollToPosition.Center, true);
                                }
                                
                            });
                            AllSyncLyrics = null;
                            splittedLyricsLines = null;
                            TemporarilyPickedSong.IsPlaying = true;
                            PickedSong.IsPlaying = true;
                            IsPlaying = true;
                            CurrentLyricPhrase = new LyricPhraseModel() { Text = "" };
                            if (CurrentPage == PageEnum.FullStatsPage)
                            {
                                ShowGeneralTopXSongs();
                                //ShowSingleSongStats(PickedSong);
                            }
                            CurrentRepeatCount = PlayBackService.CurrentRepeatCount;
                            
                            PrevCurrNextSongsCollection =
    [
                                    PlayBackService.PreviouslyPlayingSong,
                                    PlayBackService.CurrentlyPlayingSong,
                                    PlayBackService.NextPlayingSong,
                                ];
                            break;
                        case MediaPlayerState.Paused:
                            TemporarilyPickedSong.IsCurrentPlayingHighlight = true;
                            PickedSong.IsPlaying = false;
                            IsPlaying = false;
                            break;
                        case MediaPlayerState.Stopped:
                            //PickedSong = "Stopped";
                            break;
                        case MediaPlayerState.LoadingSongs:
                            LoadingSongsProgress = PlayBackService.LoadingSongsProgressPercentage;
                            break;
                        case MediaPlayerState.ShowPlayBtn:
                            IsPlaying = false;
                            break;
                        case MediaPlayerState.ShowPauseBtn:
                            IsPlaying = true;
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

    [RelayCommand]
    async Task NavToNowPlayingPage(SongsModelView? song)
    {

        if (song != null)
        {
            SelectedSongToOpenBtmSheet = (song == TemporarilyPickedSong) ? TemporarilyPickedSong : song;
            if (song == TemporarilyPickedSong)
            {
                SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
            }
            else
            {
                SynchronizedLyrics?.Clear();
                SynchronizedLyrics = LyricsService.LoadSynchronizedAndSortedLyrics(song.FilePath);
                SelectedSongToOpenBtmSheet = song;
            }
            
            CurrentViewIndex = 1;
        }
#if WINDOWS
        await Shell.Current.GoToAsync(nameof(NowPlayingD));
#elif ANDROID

        SongPickedForStats.Song = SelectedSongToOpenBtmSheet;
        ShowSingleSongStats(SongPickedForStats.Song);

        var currentPage = Shell.Current.CurrentPage;

        if (currentPage.GetType() != typeof(SingleSongShell))
        {
            await Shell.Current.GoToAsync(nameof(SingleSongShell));
        }
#endif
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

    [RelayCommand]
    //void PlaySong(SongsModelView? SelectedSong = null)
    async Task PlaySong(SongsModelView? SelectedSong = null)
    {
        CurrentQueue = 0;
        if (SelectedSong != null && CurrentPage == PageEnum.PlaylistsPage)
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
        if (SelectedSong is not null)
        {
            if (CurrentQueue == 1)
            {
                await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue, SecQueueSongs: AllArtistsAlbumSongs);
                return;
            }
            await PlayBackService.PlaySongAsync(SelectedSong, CurrentQueue: CurrentQueue);
            return;
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
        CurrentPositionInSeconds = CurrentPositionPercentage * TemporarilyPickedSong.DurationInSeconds;
        PlayBackService.SetSongPosition(CurrentPositionInSeconds);
    }

    [RelayCommand]
    void ChangeVolume()
    {
        PlayBackService.ChangeVolume(VolumeSliderValue);
    }

    [ObservableProperty]
    string shuffleOnOffImage = MaterialTwoTone.Shuffle;

    [ObservableProperty]
    string repeatModeImage = MaterialTwoTone.Repeat;
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
                RepeatModeImage = MaterialTwoTone.Repeat_on;
                break;
            case 2:
            case 4:
                RepeatModeImage = MaterialTwoTone.Repeat_one_on;
                break;
            case 0:
                RepeatModeImage = MaterialTwoTone.Repeat;
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
            ShuffleOnOffImage = MaterialTwoTone.Shuffle_on;
        }
        else
        {
            ShuffleOnOffImage = MaterialTwoTone.Shuffle;
        }
    }
    #endregion
    [RelayCommand]
    void SearchSong(string songText)
    {
        PlayBackService.SearchSong(songText);
        TemporarilyPickedSong = PlayBackService.CurrentlyPlayingSong;
    }

    bool isSongBtmSheetShown;
    SongMenuBtmSheet songsBtmSheet;
    public async Task OpenSingleSongOptionsBtmSheet(SongsModelView song)// = null)
    {
#if ANDROID
        if (!isSongBtmSheetShown)
        {
            songsBtmSheet = new SongMenuBtmSheet(this, song);
            SelectedSongToOpenBtmSheet = song;
            await songsBtmSheet.ShowAsync();
            songsBtmSheet.Dismissed += SongsBtmSheet_Dismissed;
            isSongBtmSheetShown = true;
        }
#endif
    }

    private void SongsBtmSheet_Dismissed(object? sender, DismissOrigin e)
    {
        isSongBtmSheetShown = false;
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
    public void LoadSongCoverImage()
    {

        if (TemporarilyPickedSong is not null)
        {
            PickedSong = TemporarilyPickedSong;
            SelectedSongToOpenBtmSheet = TemporarilyPickedSong;
            CurrentPositionPercentage = AppSettingsService.LastPlayedSongPositionPref.GetLastPosition();
            CurrentPositionInSeconds = AppSettingsService.LastPlayedSongPositionPref.GetLastPosition() * TemporarilyPickedSong.DurationInSeconds;
        }
        else
        {
            var lastID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
            TemporarilyPickedSong = DisplayedSongs.FirstOrDefault(x => x.Id == lastID);

        }

        SongPickedForStats ??= new SingleSongStatistics();
        SongPickedForStats.Song = TemporarilyPickedSong;
    }

    #region Subscriptions to Services

    private IDisposable _playerStateSubscription;
    [ObservableProperty]
    bool isPlaying = false;

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
            PrevCurrNextSongsCollection =
            [
                PlayBackService.PreviouslyPlayingSong,
                PlayBackService.CurrentlyPlayingSong,
                PlayBackService.NextPlayingSong,
            ];
            //ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;
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
    Content[] allSyncLyrics;
    [ObservableProperty]
    bool isFetchSuccessful = true;
    [ObservableProperty]
    bool isFetching;
    [RelayCommand]
    async Task FetchLyrics(bool fromUI = false)
    {
        IsFetching = true;
        if (fromUI || SynchronizedLyrics?.Count < 1)
        {
            AllSyncLyrics = Array.Empty<Content>();
            (IsFetchSuccessful, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLrcLib(TemporarilyPickedSong);
        }
        IsFetching = false;
        return;
    }

    [RelayCommand]
    async Task FetchLyricsLyrist(bool fromUI = false)
    {
        IsFetching = true;
        if (fromUI || SynchronizedLyrics?.Count < 1)
        {
            AllSyncLyrics = Array.Empty<Content>();
            (IsFetchSuccessful, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLyrist(TemporarilyPickedSong);
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
        }
    }

    [ObservableProperty]
    string songTitle;
    [ObservableProperty]
    string artistName;
    [ObservableProperty]
    string albumName;
    [ObservableProperty]
    bool useManualSearch;
    [RelayCommand]
    async Task UseManualLyricsSearch()
    {
        AllSyncLyrics = Array.Empty<Content>();
        List<string> manualSearchFields = new List<string>();
        manualSearchFields.Add(SongTitle);
        manualSearchFields.Add(ArtistName);
        manualSearchFields.Add(AlbumName);

        (IsFetchSuccessful, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLrcLib(TemporarilyPickedSong, true, manualSearchFields);
        if (!IsFetchSuccessful)
        {
            (IsFetchSuccessful, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLyrist(TemporarilyPickedSong, true, manualSearchFields);
        }
    }

    public async Task SaveSelectedLyricsToFile(bool isSync, Content cont) // rework this!
    {
        bool isSavedSuccessfully;

        if (!isSync)
        {
            TemporarilyPickedSong.HasLyrics = true;
            TemporarilyPickedSong.UnSyncLyrics = cont.plainLyrics;
            TemporarilyPickedSong.HasSyncedLyrics = false;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.plainLyrics, TemporarilyPickedSong, isSync);
        }
        else
        {
            TemporarilyPickedSong.HasLyrics = false;
            TemporarilyPickedSong.UnSyncLyrics = string.Empty;
            TemporarilyPickedSong.HasSyncedLyrics = true;
            isSavedSuccessfully = LyricsManagerService.WriteLyricsToLyricsFile(cont.syncedLyrics, TemporarilyPickedSong, isSync);
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
        if (DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id) is not null)
        {
            DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
        }
        if (PlayBackService.CurrentQueue != 2)
        {
            SongsMgtService.UpdateSongDetails(TemporarilyPickedSong);
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
    async Task FetchSongCoverImage()
    {
        TemporarilyPickedSong.CoverImagePath = string.Empty;

        await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
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

    
}