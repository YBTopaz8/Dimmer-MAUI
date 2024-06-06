namespace Dimmer_MAUI.ViewModels;
using Microsoft.VisualBasic.FileIO;
using IconPacks.IconKind;
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
    IList<LyricPhraseModel> synchronizedLyrics;
    [ObservableProperty]
    string currentLyricPhrase;
    
    [ObservableProperty]
    int loadingSongsProgress;

    [ObservableProperty]
    double volumeSliderValue = 1;

    IFolderPicker folderPicker { get; }
    IFilePicker filePicker { get; }
    IPlayBackService PlayBackManagerService { get; }
    ILyricsService LyricsManagerService { get; }

    public HomePageVM(IPlayBackService PlaybackManagerService, IFolderPicker folderPickerService, IFilePicker filePickerService,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService)
    {
        this.folderPicker = folderPickerService;
        filePicker = filePickerService;
        
        PlayBackManagerService = PlaybackManagerService;
        LyricsManagerService = lyricsService;

        //Subscriptions to SongsManagerService
        SubscribeToPlayerStateChanges();
        SubscribetoDisplayedSongsChanges();
        SubscribeToCurrentSongPosition();

        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToUnSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        VolumeSliderValue = AppSettingsService.VolumeSettings.GetVolumeLevel();
        PickedSongCoverImage = ImageSource.FromFile("Resources/musical.png");
        
        DisplayedSongs = songsMgtService.AllSongs.ToObservableCollection();
        TotalSongsDuration= PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;

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
        //if (!await CheckPermissions.CheckAndRequestStoragePermissionAsync())
        //{
        //    await Shell.Current.DisplayAlert("No Permission", "No Permission to read files", "OK");
        //    return;
        //}
        //var filePickingResult = await filePicker.PickAsync();
        //var folderPickingResult = await FolderPicker.PickAsync(token);
        //if (folderPickingResult.Folder is null)
        //{
        //    return;
        //}
        //var folder = folderPickingResult.Folder?.Path;
#if ANDROID
        //var folderPickingResult = await FolderPicker.PickAsync(token);
        //if (folderPickingResult.Folder is null)
        //{
        //    return;
        //}
        //var folder = folderPickingResult.Folder?.Path;
        var folder = "/storage/emulated/0/Music";
#elif WINDOWS

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
        //if (await CheckPermissions.CheckAndRequestStoragePermissionAsync())
        //{
        //    CancellationTokenSource source = new();
        //    CancellationToken token = source.Token;
        //    var result = await filePicker.PickAsync();

        //    if (result != null)
        //    {
        //        PickedSong = result.FullPath;
        //        SongsManagerService.PlayNextSong(PickedSong);
        //    }
        //}
    }

    [ObservableProperty]
    string playPauseImage = Material.PlayArrow;
    [RelayCommand]
    void PlaySong(SongsModelView? SelectedSong = null)
    {
         PlayPauseImage = Material.Pause;
        //PlayPauseImage = "pause_d.png";
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

        SwitchPlayPauseImage();
        await PlayBackManagerService.PauseResumeSongAsync();
    }

    private void SwitchPlayPauseImage()
    {
        if (PlayPauseImage == Material.PlayArrow)
        {
            PlayPauseImage = Material.Pause;
        }
        else
        {
            PlayPauseImage = Material.PlayArrow;
            
        }
    }

    [RelayCommand]
    async Task StopSong()
    {
        await PlayBackManagerService.StopSongAsync();
    }

    [RelayCommand]
    async Task PlayNextSong()
    {
        if (PlayPauseImage == Material.PlayArrow)
        {
            PlayPauseImage = Material.Pause;
        }
        await PlayBackManagerService.PlayNextSongAsync();
    }

    [RelayCommand]
    async Task PlayPreviousSong()
    {
        if (PlayPauseImage == Material.PlayArrow)
        {
            PlayPauseImage = Material.Pause;
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
    void AddSongToFavorites(SongsModelView song)// = null)
    {
        PlayBackManagerService.UpdateSongToFavoritesPlayList(song);
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
                    
                    //PickedSong = PlayBackManagerService.CurrentlyPlayingSong;
                    IsPlaying = PlayBackManagerService.CurrentlyPlayingSong.IsPlaying;
                    if (PickedSong.CoverImage is not null)
                    {                        
                        PickedSongCoverImage = ImageSource.FromStream(() => new MemoryStream(PickedSong.CoverImage));
                    }
                    break;
                case MediaPlayerState.Paused:
                    // PickedSong = "Paused";
                    break;
                case MediaPlayerState.Stopped:
                    //PickedSong = "Stopped";
                    break;
            }
        });
    }


    //Subscriptions to LyricsServices
    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            CurrentLyricPhrase = highlightedLyric is null ? string.Empty : highlightedLyric!.Text;
        });
    }

    [ObservableProperty]
    string unsyncedLyrics;
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
            SynchronizedLyrics = synchronizedLyrics;
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

