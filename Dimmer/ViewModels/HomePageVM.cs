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
    string currentLyricPhrase;
    
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
    public IServiceProvider ServiceProvider { get; }

    [ObservableProperty]
    string unsyncedLyrics;

    public HomePageVM(IPlayBackService PlaybackManagerService, IFolderPicker folderPickerService, IFilePicker filePickerService,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService,
                      IServiceProvider serviceProvider)
    {
        this.folderPicker = folderPickerService;
        filePicker = filePickerService;
        
        PlayBackManagerService = PlaybackManagerService;
        LyricsManagerService = lyricsService;
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
        LoadPickedSongCoverImage();
        DisplayedSongs = songsMgtService.AllSongs.ToObservableCollection();
        //PickedSong = PlaybackManagerService.CurrentlyPlayingSong;
        TotalSongsDuration= PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;

        ToggleShuffleState();
        ToggleRepeatMode();
    }

    void LoadPickedSongCoverImage()
    {
        if (PickedSong is not null)
        {
            PickedSongCoverImage = ImageSource.FromStream(() => new MemoryStream(PickedSong.CoverImage));
        }
    }
    [ObservableProperty]
    string shuffleOnOffImage = MaterialTwoTone.Shuffle;

    [RelayCommand]
    void ToggleShuffleState(bool IsCalledByUI = false)
    {
        IsShuffleOn = PlayBackManagerService.IsShuffleOn;
        if(IsCalledByUI)
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
            Debug.WriteLine("Songs Loaded Successfully");
        }
        else
        {
            Debug.WriteLine("No Songs Found");
        }
        IsLoadingSongs = false;
    }

    [ObservableProperty]
    string playPauseImage = MaterialTwoTone.Play_arrow;
    [RelayCommand]
    void PlaySong(SongsModelView? SelectedSong = null)
    {
         PlayPauseImage = MaterialTwoTone.Pause;
        
        if (SelectedSong is not null)
        {
            PlayBackManagerService.PlaySongAsync(SelectedSong);
        }
        else
        {
            PlayBackManagerService.PlaySongAsync(null);
        }
    }

    [RelayCommand]
    async Task PauseResumeSong()
    {
        await PlayBackManagerService.PauseResumeSongAsync();
    }


    [RelayCommand]
    async Task StopSong()
    {
        await PlayBackManagerService.StopSongAsync();
    }

    [RelayCommand]
    async Task PlayNextSong()
    {
        if (PlayPauseImage == MaterialTwoTone.Play_arrow)
        {
            PlayPauseImage = MaterialTwoTone.Pause;
        }
        await PlayBackManagerService.PlayNextSongAsync();
    }

    [RelayCommand]
    async Task PlayPreviousSong()
    {
        if (PlayPauseImage == MaterialTwoTone.Play_arrow)
        {
            PlayPauseImage = MaterialTwoTone.Pause;
        }
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
    void SearchSong(string songText)
    {
        PlayBackManagerService.SearchSong(songText);
    }


    [RelayCommand]
    void OpenBtmSheet(SongsModelView song)// = null)
    {        
        SongMenuBtmSheet btmSheet =  new(ServiceProvider.GetService<PlaylistsPageVM>(), song);
        btmSheet.HomePageVM = this;
        btmSheet.ShowAsync();
        //PlayBackManagerService.UpdateSongToFavoritesPlayList(song);
    }

    //Subscriptions to SongsManagerService
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
    void ReloadSizeAndDuration()
    {
        TotalSongsDuration = PlayBackManagerService.TotalSongsDuration;
        TotalSongsSize = PlayBackManagerService.TotalSongsSizes;
    }

    [ObservableProperty]
    bool isPlaying;
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

    public void LoadSongCoverImage()
    {
        if (PickedSong.CoverImage is not null)
        {
            PickedSongCoverImage = ImageSource.FromStream(() => new MemoryStream(PickedSong.CoverImage));
        }
        else
        {
            PickedSongCoverImage = ImageSource.FromFile("Resources/musical.png");
        }
    }


    //Subscriptions to LyricsServices
    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            CurrentLyricPhrase = highlightedLyric is null ? string.Empty : highlightedLyric!.Text;
        });
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
        Debug.WriteLine("here");
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
                if(result is true)
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

