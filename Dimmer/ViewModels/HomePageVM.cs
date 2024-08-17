using Microsoft.VisualBasic.FileIO;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM : ObservableObject
{
    [ObservableProperty]
    SongsModelView pickedSong; // I use this a lot with the collection view, mostly to scroll

    [ObservableProperty]
    SongsModelView temporarilyPickedSong;

    //[ObservableProperty]
    //ImageSource pickedSongCoverImage;
    [ObservableProperty]
    double currentPositionPercentage;
    [ObservableProperty]
    double currentPositionInSeconds = 0;

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
    IPlaybackUtilsService PlayBackManagerService { get; }
    ILyricsService LyricsManagerService { get; }
    public ISongsManagementService SongsMgtService { get; }
    public IServiceProvider ServiceProvider { get; }

    [ObservableProperty]
    string unSyncedLyrics;
    [ObservableProperty]
    string localFilePath;
    public HomePageVM(IPlaybackUtilsService PlaybackManagerService, IFolderPicker folderPickerService, IFilePicker filePickerService,
                      ILyricsService lyricsService, ISongsManagementService songsMgtService, IServiceProvider serviceProvider)
    
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
        SubscribeToPlaylistChanges();
        //Subscriptions to LyricsServices
        SubscribeToSyncedLyricsChanges();
        SubscribeToLyricIndexChanges();

        VolumeSliderValue = AppSettingsService.VolumeSettingsPreference.GetVolumeLevel();

        LoadSongCoverImage();

        DisplayedSongs = songsMgtService.AllSongs.ToObservableCollection();
        DisplayedPlaylists = PlayBackManagerService.AllPlaylists;
        TotalSongsDuration = PlaybackManagerService.TotalSongsDuration;
        TotalSongsSize = PlaybackManagerService.TotalSongsSizes;
        IsPlaying = false;
        IsOnLyricsSyncMode = false;
        ToggleShuffleState();
        ToggleRepeatMode();
        AppSettingsService.MusicFoldersPreference.ClearListOfFolders();
    }
    protected int sss;
    [ObservableProperty]
    byte[] allPictureDatas;

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
        IsLoadingSongs = true;
        if (FolderPaths is null)
        {
            await Shell.Current.DisplayAlert("Error !", "No Paths to load", "OK");
            return;
        }
        bool loadSongsResult = await PlayBackManagerService.LoadSongsFromFolder(FolderPaths.ToList());
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
        //if (IsOnLyricsSyncMode)
        //{

        //}
        PlayBackManagerService.SetSongPosition(CurrentPositionPercentage);
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
        SongMenuBtmSheet btmSheet = new(this, song);
        btmSheet.HomePageVM = this;
        await btmSheet.ShowAsync();
    }
    [RelayCommand]
    void AddSongToFavorites(SongsModelView song)
    {
        song.IsFavorite = !song.IsFavorite;
        PlayBackManagerService.UpdateSongToFavoritesPlayList(song);
        if (song.IsFavorite)
        {
            PlayBackManagerService.AddSongToPlayListWithPlayListName(song, "Favorites");
        }
        else
        {
            PlayBackManagerService.RemoveFromPlayListWithPlayListName(song, "Favorites");
        }
    }

    void ReloadSizeAndDuration()
    {
        TotalSongsDuration = PlayBackManagerService.TotalSongsDuration;
        TotalSongsSize = PlayBackManagerService.TotalSongsSizes;
    }


    public void LoadSongCoverImage()
    {

        PickedSong = TemporarilyPickedSong;
    }

    #region Subscriptions to Services
    [ObservableProperty]
    bool isPlaying = false;
    void SubscribeToPlayerStateChanges()
    {
        PlayBackManagerService.PlayerState.Subscribe(state =>
        {
            TemporarilyPickedSong = PlayBackManagerService.CurrentlyPlayingSong;
            PickedSong = TemporarilyPickedSong;
            switch (state)
            {
                case MediaPlayerState.Playing:
                    if(splittedLyricsLines is not null)
                    {
                        Array.Clear(splittedLyricsLines);
                    }
                    if (AllSyncLyrics is not null)
                    {
                        Array.Clear(AllSyncLyrics);
                    }
                    IsPlaying = true;
                    if(CurrentViewIndex == 3)
                    {
                        OpenEditableSongsTagsView();
                    }
                    break;
                case MediaPlayerState.Paused:
                    IsPlaying = false;
                    break;
                case MediaPlayerState.Stopped:
                    //PickedSong = "Stopped";
                    break;
                case MediaPlayerState.LoadingSongs:
                    LoadingSongsProgress = PlayBackManagerService.LoadingSongsProgressPercentage;
                    break;
                default: 
                    break;
            }
        });

    }
    private void SubscribeToLyricIndexChanges()
    {
        LyricsManagerService.CurrentLyricStream.Subscribe(highlightedLyric =>
        {
            //CurrentLyricPhrase = highlightedLyric is null ? null : highlightedLyric!;
            Debug.WriteLine("Current lyric phrase " + highlightedLyric is null);
        });
    }
    private void SubscribeToPlaylistChanges()
    {
        PlayBackManagerService.SecondaryQueue.Subscribe(songs =>
        {
            DisplayedSongsFromPlaylist = songs;
        });
    }
    private void SubscribeToCurrentSongPosition()
    {
        PlayBackManagerService.CurrentPosition.Subscribe(async position =>
        {
            CurrentPositionInSeconds = position.CurrentTimeInSeconds;
            CurrentPositionPercentage = position.TimeElapsed;
            if (CurrentPositionPercentage >= 0.97 && IsPlaying && IsOnLyricsSyncMode)
            {
                await PauseResumeSong();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                  string? result = await Shell.Current.DisplayActionSheet("Done Syncing?", "No", "Yes");
                    if (result is null)
                        return;
                    if (result.Equals("Yes"))
                    {
                        string? lyr = string.Join(Environment.NewLine, LyricsLines?.Select(line => $"{line.TimeStampText} {line.Text}"));
                        if (lyr is not null)
                        {
                            if (await LyricsManagerService.WriteLyricsToLyricsFile(lyr, TemporarilyPickedSong, true))
                            {
                                await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
                                CurrentViewIndex = 0;
                            }
                            LyricsManagerService.InitializeLyrics(lyr);
                            DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
                        }
                    }
                });
                //var result = await Shell.Current.DisplayPromptAsync("Syncing Mode", "Done Syncing?", "Yes", "No");
                //if (result.Equals("Yes"))
                //{
                //    string? lyr = string.Join(Environment.NewLine, LyricsLines?.Select(line => $"{line.TimeStampText} {line.Text}"));
                //    if (lyr is not null)
                //    {
                //        if (LyricsManagerService.WriteLyricsToLyricsFile(lyr, TemporarilyPickedSong, true))
                //        {
                //            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
                //            CurrentViewIndex = 0;
                //        }
                //        LyricsManagerService.InitializeLyrics(lyr);
                //        DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
                //    }
                //}
            }
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
            SynchronizedLyrics = synchronizedLyrics?.ToObservableCollection();

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
        if (await LyricsManagerService.WriteLyricsToLyricsFile(s.syncedLyrics?.Length > 0 ? s.syncedLyrics : s.plainLyrics, TemporarilyPickedSong, s.syncedLyrics?.Length > 0))
        {
            await Shell.Current.DisplayAlert("Success!", "Lyrics Saved Successfully!", "OK");
            CurrentViewIndex = 0;
        }
        LyricsManagerService.InitializeLyrics(s?.syncedLyrics?.Length > 0 ? s?.syncedLyrics : null);
        DisplayedSongs.FirstOrDefault(x => x.Id == TemporarilyPickedSong.Id)!.HasLyrics = true;
        await SongsMgtService.UpdateSongDetailsAsync(TemporarilyPickedSong);
    }

    [ObservableProperty]
    ObservableCollection<LyricPhraseModel>? lyricsLines = new();
    [RelayCommand]
    void CaptureTimestamp(LyricPhraseModel lyricPhraseModel)
    {
        var CurrPosition = CurrentPositionInSeconds;
        if (!IsPlaying)
        {
            PlaySong();
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
        if(TemporarilyPickedSong.UnSyncLyrics is null)
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

        string[]? ss = TemporarilyPickedSong.UnSyncLyrics.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        splittedLyricsLines = ss?.Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (splittedLyricsLines is null || splittedLyricsLines.Length < 1)
        {
            return;
        }
        foreach (var item in splittedLyricsLines)
        {
            var LyricPhrase = new ATL.LyricsInfo.LyricsPhrase(0, item);
            LyricPhraseModel newLyric = new(LyricPhrase);
            LyricsLines?.Add(newLyric);
        }
    }

    static string RemoveTextAndFollowingNewline(string input, string textt)
    {
        string result = input;

        // Find the position of the textt
        int index = result.IndexOf(textt);

        while (index != -1)
        {
            // Check if the character after textt is \r or \n
            int nextCharIndex = index + textt.Length;
            if (nextCharIndex < result.Length)
            {
                if (result[nextCharIndex] == '\r' || result[nextCharIndex] == '\n')
                {
                    // Remove both textt and the following \r or \n
                    result = result.Remove(index, textt.Length + 1);
                }
                else
                {
                    // Only remove textt
                    result = result.Remove(index, textt.Length);
                }
            }
            else
            {
                // Only remove textt if it's at the end
                result = result.Remove(index, textt.Length);
            }

            // Search for the next occurrence of textt
            index = result.IndexOf(textt);
        }

        return result;
    }

    [RelayCommand]
    async Task FetchSongCoverImage()
    {
        await LyricsManagerService.FetchAndDownloadCoverImage(TemporarilyPickedSong);
    }

    [ObservableProperty]
    int currentViewIndex;
    [ObservableProperty]
    bool isOnLyricsSyncMode = false;
    [RelayCommand]
    void SwitchViewNowPlayingPage(int viewIndex)
    {
        CurrentViewIndex = viewIndex;
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
                //await FetchLyrics();
                break;
            case 3:
                OpenEditableSongsTagsView();
                break;
            default:
                break;
        }
        
    }

    void OpenEditableSongsTagsView()
    {
        LyricsLines?.Clear();
        PrepareLyricsSync();        
    }

    [RelayCommand]
    void OpenSongFolder() //SongsModel SelectedSong)
    {
#if WINDOWS
        var filePath = TemporarilyPickedSong.FilePath; // SelectedSong.FilePath
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
        {
            // Open File Explorer and select the file
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
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

