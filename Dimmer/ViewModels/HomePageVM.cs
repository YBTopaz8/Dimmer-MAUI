namespace Dimmer_MAUI.ViewModels;
using Microsoft.VisualBasic.FileIO;
using System.Text.RegularExpressions;

public partial class HomePageVM : ObservableObject
{
    [ObservableProperty]
    SongsModelView pickedSong;

    [ObservableProperty]
    SongsModelView temporarilyPickedSong;

    //[ObservableProperty]
    //ImageSource pickedSongCoverImage;
    [ObservableProperty]
    double currentPosition;
    [ObservableProperty]
    double currentPositionText = 0;

    [ObservableProperty]
    ObservableCollection<SongsModelView> displayedSongs;

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

    IFolderPicker folderPicker { get; }
    IFilePicker filePicker { get; }
    IPlayBackService PlayBackManagerService { get; }
    ILyricsService LyricsManagerService { get; }
    public ISongsManagementService SongsMgtService { get; }
    public IServiceProvider ServiceProvider { get; }

    [ObservableProperty]
    string unSyncedLyrics;
    [ObservableProperty]
    string localFilePath;
    public HomePageVM(IPlayBackService PlaybackManagerService, IFolderPicker folderPickerService, IFilePicker filePickerService,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService,
                      IServiceProvider serviceProvider)
    {
        this.folderPicker = folderPickerService;
        filePicker = filePickerService;
        PlayBackManagerService = PlaybackManagerService;
        LyricsManagerService = lyricsService;
        SongsMgtService = songsMgtService;
        ServiceProvider = serviceProvider;

        //Subscriptions to SongsManagerService
        SubscribeToPlayerStateChanges();
        SubscribetoDisplayedSongsChanges();
        SubscribeToCurrentSongPosition();

        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        VolumeSliderValue = AppSettingsService.VolumeSettingsPreference.GetVolumeLevel();

        LoadSongCoverImage();
        LoadLyrics();

        DisplayedSongs = songsMgtService.AllSongs.ToObservableCollection();
        //PickedSong = PlaybackManagerService.CurrentlyPlayingSong;
        TotalSongsDuration = PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;
        IsPlaying = false;

        ToggleShuffleState();
        ToggleRepeatMode();
        AppSettingsService.MusicFoldersPreference.ClearListOfFolders();
    }

    [ObservableProperty]
    byte[] allPictureDatas;

    private void LoadLyrics()
    {
        try
        {
            LyricsManagerService.LoadLyrics(TemporarilyPickedSong);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when Loading lyrics in homepagevm " + ex.Message);
        }
    }

    public void LoadLocalSongFromOutSideApp(string[] filePath)
    {
        PlayBackManagerService.PlaySelectedSongsOutsideApp(filePath);
    }


    [RelayCommand]
    async Task NavToNowPlayingPage()
    {
        await Shell.Current.GoToAsync(nameof(NowPlayingD));
    }

    [ObservableProperty]
    bool isLoadingSongs;

    [ObservableProperty]
    ObservableCollection<string> folderPaths;
    [RelayCommand]
    async Task SelectSongFromFolder()
    {
        FolderPaths = AppSettingsService.MusicFoldersPreference.GetMusicFolders().ToObservableCollection();

        bool res = await Shell.Current.DisplayAlert("Select Song", "Sure?", "Yes", "No");
        if (!res)
        {
            return;
        }
        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;


#if WINDOWS || ANDROID
        var folderPickingResult = await FolderPicker.PickAsync(token);
        if (folderPickingResult.Folder is null)
        {
            return;
        }
        var folder = folderPickingResult.Folder?.Path;
#elif IOS || MACCATALYST
        string folder = null;
#endif

        var FolderName = Path.GetFileName(folder);
        FolderPaths.Add(FolderName);

        FullFolderPaths.Add(folder);

        AppSettingsService.MusicFoldersPreference.AddMusicFolder(FullFolderPaths);
        //await LoadSongsFromFolders();//FullFolderPaths);
    }

    List<string> FullFolderPaths = new();

    [RelayCommand]
    private async Task LoadSongsFromFolders()
    {
        IsLoadingSongs = true;

        LoadingSongsProgress = PlayBackManagerService.LoadProgressPercent;
        
        bool loadSongsResult = await PlayBackManagerService.LoadSongsFromFolder(FullFolderPaths);
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

    #region Playback Control Region
    [RelayCommand]
    void PlaySong(SongsModelView? SelectedSong = null)
    {

        if (SelectedSong is not null)
        {
            PlayBackManagerService.PlaySongAsync(SelectedSong);            
        }
        else
        {
            PlayBackManagerService.PlaySongAsync(null);
        }
        AllSyncLyrics = Array.Empty<Content>();
        
    }

    [RelayCommand]
    async Task PauseResumeSong()
    {
        await PlayBackManagerService.PauseResumeSongAsync();
        //Debug.WriteLine($"is liked : {TemporarilyPickedSong.Title} {TemporarilyPickedSong.IsFavorite}");
    }


    [RelayCommand]
    async Task StopSong()
    {
        await PlayBackManagerService.StopSongAsync();
    }

    [RelayCommand]
    async Task PlayNextSong()
    {
        await PlayBackManagerService.PlayNextSongAsync();
    }

    [RelayCommand]
    async Task PlayPreviousSong()
    {
        await PlayBackManagerService.PlayPreviousSongAsync();
    }

    [RelayCommand]
    void DecreaseVolume()
    {
        PlayBackManagerService.DecreaseVolume();
        VolumeSliderValue -= 0.2;
    }
    [RelayCommand]
    void IncreaseVolume()
    {
        PlayBackManagerService.IncreaseVolume();
        VolumeSliderValue += 0.2;
    }

    [RelayCommand]
    void SeekSongPosition(object? value)
    {
        PlayBackManagerService.SetSongPosition(CurrentPosition);
    }

    [RelayCommand]
    void ChangeVolume()
    {
        PlayBackManagerService.ChangeVolume(VolumeSliderValue);
    }

    [ObservableProperty]
    string shuffleOnOffImage = MaterialTwoTone.Shuffle;

    [ObservableProperty]
    string repeatModeImage = MaterialTwoTone.Repeat;
    [RelayCommand]
    void ToggleRepeatMode(bool IsCalledByUI = false)
    {
        CurrentRepeatMode = PlayBackManagerService.CurrentRepeatMode;
        if (IsCalledByUI)
        {
            CurrentRepeatMode = PlayBackManagerService.ToggleRepeatMode();
        }

        switch (CurrentRepeatMode)
        {
            case 1:
                RepeatModeImage = MaterialTwoTone.Repeat_on;
                break;
            case 2:
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
        IsShuffleOn = PlayBackManagerService.IsShuffleOn;
        if (IsCalledByUI)
        {
            IsShuffleOn = !IsShuffleOn;
            PlayBackManagerService.ToggleShuffle(IsShuffleOn);
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
        PlayBackManagerService.SearchSong(songText);
    }


    [RelayCommand]
    async Task OpenNowPlayingBtmSheet(SongsModelView song)// = null)
    {
        SongMenuBtmSheet btmSheet = new(ServiceProvider.GetService<PlaylistsPageVM>(), song);
        btmSheet.HomePageVM = this;
        await btmSheet.ShowAsync();
    }
    [RelayCommand]
    void AddSongToFavorites(SongsModelView song)
    {
        PlayBackManagerService.UpdateSongToFavoritesPlayList(song);
    }

    void ReloadSizeAndDuration()
    {
        Debug.WriteLine(DisplayedSongs.Count);
        TotalSongsDuration = PlayBackManagerService.TotalSongsDuration;
        TotalSongsSize = PlayBackManagerService.TotalSongsSizes;
    }


    public void LoadSongCoverImage()
    {
        if (TemporarilyPickedSong is null)
            return;
        if (TemporarilyPickedSong.CoverImagePath is not null)
        {
            //PickedSongCoverImage = ImageSource.FromFile(TemporarilyPickedSong.CoverImagePath);
        }
        else
        {
            //PickedSongCoverImage = ImageSource.FromFile("Resources/musical.png");
        }
    }

    #region Subscriptions to Services
    [ObservableProperty]
    bool isPlaying = false;
    private async void SubscribeToPlayerStateChanges()
    {
        PlayBackManagerService.PlayerState.Subscribe(state =>
        {
            TemporarilyPickedSong = PlayBackManagerService.CurrentlyPlayingSong;
            //PickedSong = PlayBackManagerService.CurrentlyPlayingSong;
            switch (state)
            {
                case MediaPlayerState.Playing:
                    IsPlaying = true;
                    break;
                case MediaPlayerState.Paused:
                    IsPlaying = false;
                    break;
                case MediaPlayerState.Stopped:
                    //PickedSong = "Stopped";
                    break;
            }
        });

    }
    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            CurrentLyricPhrase = highlightedLyric is null ? null : highlightedLyric!;
            Debug.WriteLine("Current lyric phrase " + highlightedLyric is null);
        });
    }
    private void SubscribeToCurrentSongPosition()
    {
        PlayBackManagerService.CurrentPosition.Subscribe(position =>
        {
            CurrentPositionText = position.CurrentTimeInSeconds;
            CurrentPosition = position.TimeElapsed;
        });
    }
    private void SubscribetoDisplayedSongsChanges()
    {
        PlayBackManagerService.NowPlayingSongs.Subscribe(songs =>
        {
            DisplayedSongs?.Clear();
            DisplayedSongs = songs.ToObservableCollection();
            TotalNumberOfSongs = songs.Count;
            ReloadSizeAndDuration();
        });
        IsLoadingSongs = false;
    }

    private void SubscribeToSyncedLyricsChanges()
    {
        LyricsManagerService.SynchronizedLyricsStream.Subscribe(synchronizedLyrics =>
        {
            SynchronizedLyrics = synchronizedLyrics is null ? null : synchronizedLyrics.ToObservableCollection();

            Debug.WriteLine("Lyrics should have been updated " + SynchronizedLyrics?.Count);
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
    [RelayCommand]
    async Task SaveUserSelectedLyricsToFile(int id)
    {
        var s = AllSyncLyrics.First(x => x.id == id);
        if (s.syncedLyrics is null || s.syncedLyrics?.Length < 1)
        {
            TemporarilyPickedSong.UnSyncLyrics = s.plainLyrics;
            TemporarilyPickedSong.HasLyrics = true;
            TemporarilyPickedSong.HasSyncedLyrics = false;
        }
        if (LyricsManagerService.WriteLyricsToLyricsFile(s.syncedLyrics?.Length > 0 ? s.syncedLyrics : s.plainLyrics, TemporarilyPickedSong, s.syncedLyrics?.Length > 0))
        {
            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
            CurrentViewIndex = 0;
        }
        LyricsManagerService.InitializeLyrics(s?.syncedLyrics?.Length > 0 ? s?.syncedLyrics : null);
        DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
    }

    [RelayCommand]
    async Task FetchSongCoverImage()
    {
        await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
    }

    [ObservableProperty]
    int currentViewIndex;

    [RelayCommand]
    async Task SwitchViewNowPlayingPage(int viewIndex)
    {
        CurrentViewIndex = viewIndex;
        if (viewIndex == 1)
        {
            await FetchLyrics();

        }
    }


    [RelayCommand]
    void OpenSongFolder()//SongsModel SelectedSong)
    {
#if WINDOWS
        var directoryPath = Path.GetDirectoryName(TemporarilyPickedSong.FilePath);//SelectedSong.FilePath);
        Debug.WriteLine(directoryPath);
        if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = directoryPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
#endif
    }

    [RelayCommand]
    async Task DeleteFile()
    {
        if (TemporarilyPickedSong is null)
        {
            Debug.WriteLine("Null");
            return;
        }
        try
        {
            if (File.Exists(TemporarilyPickedSong.FilePath))
            {
                bool result = await Shell.Current.DisplayAlert("Delete File", "Are you sure you want to delete this file?", "Yes", "No");
                if (result is true)
                {
                    FileSystem.DeleteFile(PickedSong.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

                    //File.Delete(PickedSong.FilePath);
                    Debug.WriteLine("File Deleted");

                }
            }
            else
            {
                Debug.WriteLine("Not Deleted");
            }
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
        }
    }
}

