namespace Dimmer_MAUI.ViewModels;
using Microsoft.VisualBasic.FileIO;

public partial class HomePageVM : ObservableObject
{
    [ObservableProperty]
    SongsModelView pickedSong;

    [ObservableProperty]
    SongsModelView temporarilyPickedSong;

    [ObservableProperty]
    ImageSource pickedSongCoverImage;
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
    IList<LyricPhraseModel>? synchronizedLyrics;
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
    string unsyncedLyrics;
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
        SubscribeToUnSyncedLyricsChanges();
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
    }

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

    public void LoadLocalSong(string filePath)
    {
        Debug.WriteLine("Loaded path should be " + filePath);
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
    async Task NavToNowPlayingPage()
    {
       await Shell.Current.GoToAsync(nameof(NowPlayingD));
    }

    [ObservableProperty]
    bool isLoadingSongs;

    [RelayCommand]
    async Task SelectSongFromFolder()
    {
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
        IsLoadingSongs = true;
        var progressIndicator = new Progress<int>(percent =>
        {
            LoadingSongsProgress = percent;
        });
        IsLoadingSongs = true;
        bool loadSongsResult = await PlayBackManagerService.LoadSongsFromFolder(folder!, progressIndicator);
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
        if (PickedSong is null)
            return;
        if (PickedSong.CoverImagePath is not null)
        {
            PickedSongCoverImage = ImageSource.FromFile(PickedSong.CoverImagePath);
            //PickedSongCoverImage = ImageSource.FromStream(() => new MemoryStream(PickedSong.CoverImage));
        }
        else
        {
            PickedSongCoverImage = ImageSource.FromFile("Resources/musical.png");
        }
    }

    #region Subscriptions to Services
    [ObservableProperty]
    bool isPlaying = false;
    private void SubscribeToPlayerStateChanges()
    {
        PlayBackManagerService.PlayerState.Subscribe(state =>
        {
            TemporarilyPickedSong = PlayBackManagerService.CurrentlyPlayingSong;
            PickedSong = PlayBackManagerService.CurrentlyPlayingSong;
            switch (state)
            {
                case MediaPlayerState.Playing:
                    IsPlaying = true;
                    LoadSongCoverImage();
                    break;
                case MediaPlayerState.Paused:
                    IsPlaying = false;
                    break;
                case MediaPlayerState.Stopped:
                    //PickedSong = "Stopped";
                    break;
            }

            Debug.WriteLine($"Is Playing = " + IsPlaying);
        });
    }
    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            CurrentLyricPhrase = highlightedLyric is null ? null : highlightedLyric!;
        });
        Debug.WriteLine("Current lyric phrase " + CurrentLyricPhrase.Text);
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

    private void SubscribeToUnSyncedLyricsChanges()
    {
        LyricsManagerService.UnSynchedLyricsStream.Subscribe(unsyncedLyrics =>
        {
            UnsyncedLyrics = unsyncedLyrics;
        });
    }

    private void SubscribeToSyncedLyricsChanges()
    {
        LyricsManagerService.SynchronizedLyricsStream.Subscribe(synchronizedLyrics =>
        {
            SynchronizedLyrics = synchronizedLyrics is null ? null : synchronizedLyrics;
        });
    }
    #endregion

    [ObservableProperty]
    Content[] allSyncLyrics;
    [ObservableProperty]
    bool isFetchSuccessul=true;
    [ObservableProperty]
    bool isFetching;
    [RelayCommand]
    async Task FetchLyrics(bool fromUI=false)
    {
        IsFetching = true;
        if(fromUI || SynchronizedLyrics?.Count < 1)
        {
            AllSyncLyrics = Array.Empty<Content>();
            (IsFetchSuccessul, AllSyncLyrics) = await LyricsManagerService.FetchLyricsOnlineLrcLib(TemporarilyPickedSong);            
        }
        IsFetching = false;
        return;
    }

    [RelayCommand]
    async Task SaveUserSelectedLyricsToLrcFile(int id)
    {
        var s = AllSyncLyrics.First(x => x.id == id);
        if(LyricsManagerService.WriteLyricsToLrcFile(s.syncedLyrics, TemporarilyPickedSong))
        {
            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");

            CurrentViewIndex = 0;
            LyricsManagerService.InitializeLyrics(s.syncedLyrics);
        }
    }
    [RelayCommand]
    async Task FetchSongCoverImage()
    {
        await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
        Debug.WriteLine($"{TemporarilyPickedSong.CoverImagePath}");
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
        var directoryPath = Path.GetDirectoryName(PickedSong.FilePath);//SelectedSong.FilePath);
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
        if (PickedSong is null)
        {
            Debug.WriteLine("Null");
            return;
        }
        try
        {
            if (File.Exists(PickedSong.FilePath))
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

