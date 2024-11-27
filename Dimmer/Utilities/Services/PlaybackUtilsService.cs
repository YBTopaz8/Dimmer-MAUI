using System.Collections.Generic;

namespace Dimmer_MAUI.Utilities.Services;
public partial class PlaybackUtilsService : ObservableObject, IPlaybackUtilsService
{

    INativeAudioService audioService;
    public IObservable<ObservableCollection<SongModelView>> NowPlayingSongs => _nowPlayingSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _nowPlayingSubject = new([]);
   
    ObservableCollection<SongModelView> nowPlayingShuffledOrNotSubject = new([]);
    
    public IObservable<ObservableCollection<SongModelView>> SecondaryQueue => _secondaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _secondaryQueueSubject = new([]);
    public IObservable<ObservableCollection<SongModelView>> TertiaryQueue => _tertiaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _tertiaryQueueSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);
    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer? _positionTimer;

    [ObservableProperty]
    private SongModelView? observableCurrentlyPlayingSong;
    public SongModelView? CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    private SongModelView observablePreviouslyPlayingSong;
    public SongModelView PreviouslyPlayingSong => ObservablePreviouslyPlayingSong;
    [ObservableProperty]
    private SongModelView observableNextPlayingSong;
    public SongModelView NextPlayingSong => ObservableNextPlayingSong;

    [ObservableProperty]
    int observableLoadingSongsProgress;
    public int LoadingSongsProgressPercentage => ObservableLoadingSongsProgress;

    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
    ISongsManagementService SongsMgtService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    
    [ObservableProperty]
    ObservableCollection<PlaylistModelView>? allPlaylists;
    [ObservableProperty]
    ObservableCollection<ArtistModelView>? allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView>? allAlbums;

    [ObservableProperty]
    string selectedPlaylistName;

    int _currentSongIndex = 0;

    [ObservableProperty]
    bool isShuffleOn;
    [ObservableProperty]
    int currentRepeatMode;
    public int CurrentRepeatCount { get; set; } = 1;
    bool isSongPlaying;

    List<string> playedSongsIDs = [];
    Random _shuffleRandomizer = Random.Shared;
    SortingEnum CurrentSorting;
    private Lazy<HomePageVM> ViewModel { get; set; }

    public PlaybackUtilsService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IPlaylistManagementService playlistManagementService, Lazy<HomePageVM> viewModel)
    {
        this.SongsMgtService = SongsMgtService;
        PlaylistManagementService = playlistManagementService;

        audioService = AudioService;

        audioService.PlayPrevious += AudioService_PlayPrevious;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.PlayEnded += AudioService_PlayEnded;
        audioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();

        CurrentQueue = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

        LoadLastPlayedSong();
        LoadSongsWithSorting();

        LoadFirstPlaylist();
        AllPlaylists = PlaylistManagementService.AllPlaylists?.ToObservableCollection();
        AllArtists = SongsMgtService.AllArtists?.ToObservableCollection();
        this.ViewModel = viewModel;
    }

    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        currentPositionInSec = e/1000;
    }


    #region Setups/Loadings Region


    public async Task<bool> LoadSongsFromFolderAsync(List<string> folderPaths)
    {
        await SongsMgtService.LoadSongsFromFolderAsync(folderPaths);

        return true;
    }


    private void LoadSongsWithSorting(ObservableCollection<SongModelView>? songss = null, bool isFromSearch = false)
    {
        if (songss == null || songss.Count < 1)
        {
            if (SongsMgtService.AllSongs is null)
            {
                return;
            }
            songss = SongsMgtService.AllSongs.ToObservableCollection();
        }
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        var sortedSongs = AppSettingsService.ApplySorting(songss, CurrentSorting);
        
        _nowPlayingSubject.OnNext(sortedSongs);
        ToggleShuffle(IsShuffleOn);
    }

    public void FullRefresh()
    {
       
       var songss = SongsMgtService.AllSongs.ToObservableCollection();
       
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        var sortedSongs = AppSettingsService.ApplySorting(songss, CurrentSorting);
        
        _nowPlayingSubject.OnNext(sortedSongs);

        
        
        ToggleShuffle(IsShuffleOn);
    }

    public SongModelView? lastPlayedSong { get; set; }

    private void LoadLastPlayedSong()
    {
        if (SongsMgtService.AllSongs is null)
        {
            return;
        }
        var lastPlayedSongID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
        if (lastPlayedSongID is not null)
        {
            lastPlayedSong = SongsMgtService.AllSongs.FirstOrDefault(x => x.LocalDeviceId == (string)lastPlayedSongID);
            
            if (lastPlayedSong is null)
            {
                return;
            }
            ObservableCurrentlyPlayingSong = lastPlayedSong!;
            //audioService.InitializeAsync(ObservableCurrentlyPlayingSong);
            //_nowPlayingSubject.OnNext(ObservableCurrentlyPlayingSong);
            _currentSongIndex = SongsMgtService.AllSongs.IndexOf(ObservableCurrentlyPlayingSong);
        }

        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
    }

    public static ObservableCollection<SongModelView> CheckCoverImage(ObservableCollection<SongModelView> col) 
    {
        foreach (var item in col)
        {
            item.CoverImagePath = GetCoverImagePath(item.FilePath);

        }
        

        return col;
    }
    public static string? GetCoverImagePath(string filePath)
    {
        var LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }

        if(coverImage is not null)
        {
            return LyricsService.SaveOrGetCoverImageToFilePath(filePath, coverImage);
        }

        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);

#if ANDROID
            string folderPath = Path.Combine(FileSystem.AppDataDirectory, "CoverImagesDimmer"); // Use AppDataDirectory for Android compatibility
#elif WINDOWS
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
#endif

      
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly))
                .ToArray();

            if (imageFiles.Length > 0)
            {                
                return imageFiles.ToString();
            }
        }

        if (coverImage is null)
        {
            return null;
        }

        return null;
    }
    byte[]? GetCoverImage(string filePath, bool isToGetByteArrayImages)
    {
        var LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }


        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");
            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly))
                .ToArray();

            if (imageFiles.Length > 0)
            {
                coverImage = File.ReadAllBytes(imageFiles[0]);

                if (!string.IsNullOrEmpty(ObservableCurrentlyPlayingSong.CoverImagePath))
                {
                    ObservableCurrentlyPlayingSong.CoverImagePath = imageFiles[0];

                }
                return coverImage;
            }
        }

        if (coverImage is null)
        {
            _playerStateSubject.OnNext(MediaPlayerState.CoverImageDownload);
        }

        return coverImage;
    }
    #endregion


    #region Playback Control Region
    public async Task<bool> PlaySelectedSongsOutsideAppAsync(List<string> filePaths)
    {
        if(filePaths.Count < 1)
            return false;
        // Filter the array to include only specific file extensions
        var filteredFiles = filePaths.Where(path => path.EndsWith(".mp3") || path.EndsWith(".flac") || path.EndsWith(".wav") || path.EndsWith(".m4a")).ToList();
        Dictionary<string, SongModelView>? existingSongDictionary = new();
        if (_tertiaryQueueSubject.Value is not null)
        {
            existingSongDictionary = _tertiaryQueueSubject.Value.ToDictionary(song => song.FilePath!, song => song, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
        }
        var allSongs = new ObservableCollection<SongModelView>();
        foreach (var file in filteredFiles)
        {
            FileInfo fileInfo = new(file);
            if (fileInfo.Length < 1000)
            {
                continue;
            }
            if (existingSongDictionary.TryGetValue(file, out var existingSong))
            {
                // If the song already exists, use the existing song
                allSongs.Add(existingSong);
            }
            else
            {
                // Otherwise, create a new song object and add it to the collection
                Track track = new Track(file);
                var newSong = new SongModelView
                {
                    Title = track.Title,
                    GenreName = track.Genre,
                    ArtistName = track.Artist,
                    AlbumName = track.Album,
                    ReleaseYear = track.Year,
                    SampleRate = track.SampleRate,
                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,
                    IsPlayedFromOutsideApp = true,
                    TrackNumber = track.TrackNumber,
                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc")),
                    CoverImagePath = LyricsService.SaveOrGetCoverImageToFilePath(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData),
                };
                allSongs.Add(newSong);
            }

        }
        _tertiaryQueueSubject.OnNext(allSongs);  
        await PlaySongAsync(allSongs[0], 2, allSongs);

        return true;
    }
    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);  // Initialize with a count of 1
    bool isLoadingSongs;
    public async Task<bool> PlaySongAsync(SongModelView? song = null, int currentQueue = 0,
        ObservableCollection<SongModelView>? currentList = null, double positionInSec = 0,
        int repeatMode = 0, int repeatMaxCount = 0,
        bool IsFromPreviousOrNext = false, AppState CurrentAppStatee = AppState.OnForeGround)
    {
        

        CurrentQueue = currentQueue;
        if (currentList is not null && CurrentQueue == 1)
        {
            _secondaryQueueSubject.OnNext(currentList);
        }
        if (currentList is not null && CurrentQueue == 2)
        {
            _tertiaryQueueSubject.OnNext(currentList);
        }
        if (CurrentAppState != CurrentAppStatee)
        {
            CurrentAppState = CurrentAppStatee;
        }
        if (_positionTimer != null)
        {
            _positionTimer.Stop();
            _positionTimer.Elapsed -= OnPositionTimerElapsed;
            _positionTimer.Dispose();
            _positionTimer = null;
        }
        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }
        else
        {
            if (song is null)
            {
                ObservableCurrentlyPlayingSong = _nowPlayingSubject.Value.FirstOrDefault()!;
                if (ObservableCurrentlyPlayingSong is null)
                {
                    if (!isLoadingSongs)
                    {
                        await Shell.Current.DisplayAlert("Error when trying to play song", "Song Not Ready to play yet!", "OK");
                    }
                }
            }
        }
        if (repeatMode == 4)
        {
            CurrentRepeatCount = 1;
            CurrentRepeatMode = 4;
            this.repeatCountMax = repeatMaxCount;
        }
        try
        {
            if (song != null)
            {
                if (!File.Exists(song.FilePath))
                {
                    SongsMgtService.DeleteSongFromDB(song);                    
                    _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());                    
                    return false;
                }
                ObservableCurrentlyPlayingSong = song!;
            }

            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong!.FilePath, true);
            
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
            {
                return false;
            }
            currentPositionInSec = 0;
            _positionTimer = new System.Timers.Timer(1000);
            _positionTimer.Elapsed += OnPositionTimerElapsed;
            _positionTimer.AutoReset = true;

            CurrentQueue = currentQueue;
            _playerStateSubject.OnNext(MediaPlayerState.LyricsLoad);

            audioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);

            // Now, play the audio after initialization has completed
            

            if (positionInSec > 0)
            {
                await audioService.PlayAsync(IsFromPreviousOrNext);
                currentPositionInSec = positionInSec;
                await audioService.SetCurrentTime(currentPositionInSec);
            }
            else
            {
                await audioService.PlayAsync(true);
            }
            _positionTimer.Start();

            ViewModel.Value.SetPlayerState(MediaPlayerState.Playing);
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            ViewModel.Value.SetPlayerState(MediaPlayerState.RefreshStats);

            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error message!!:  " + e.Message);

            await Shell.Current.DisplayAlert("Error message!!",
                $"***** UNHANDLED EXCEPTION! Details: {e.Message} | {e.Source} ", "Ok");
            return false;
        }
        finally
        {
            //here consider adding a prompt that will show when focused OR think of a quicker way to know if ext or not, that way we can give option for user to add to db or not. (It'd be awesome!!!!)
            if (File.Exists(ObservableCurrentlyPlayingSong!.FilePath) && ObservableCurrentlyPlayingSong != null && currentQueue != 2)
            {
                ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = true;
                ObservableCurrentlyPlayingSong.IsPlaying = true;

                PlayDateAndCompletionStateSongLink link = new()
                {
                    DatePlayed = DateTime.Now,
                    PlayType = 0,
                    SongId = ObservableCurrentlyPlayingSong.LocalDeviceId
                };

                SongsMgtService.UpdateSongDetails(ObservableCurrentlyPlayingSong);
                SongsMgtService.AddPlayAndCompletionLink(link);
                _currentPositionSubject.OnNext(new());
                
            }

            //ShowMiniPlayBackView();

        }
    }

    private void ShowMiniPlayBackView()
    {
#if WINDOWS
       
            MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong!);
       
#endif
    }

    public AppState CurrentAppState = AppState.OnForeGround;


    private Random random = Random.Shared;  // Reuse the same Random instance
    bool isTrueShuffle = false;  // Set this based on whether shuffle is enabled

    public ObservableCollection<SongModelView> mobileSongOrder;
    private void GetPrevAndNextSongs(bool IsNext = false, bool IsPrevious = false)
    {
        try
        {
            if (!nowPlayingShuffledOrNotSubject.Contains(ObservableCurrentlyPlayingSong))
            {
                nowPlayingShuffledOrNotSubject.Add(ObservableCurrentlyPlayingSong!); // Add to shuffled list
            }
            // Determine the list to pick from based on CurrentQueue
            ObservableCollection<SongModelView>? listToPickFrom = CurrentQueue switch
            {
                0 => SongsMgtService.AllSongs.ToObservableCollection(),
                1 => _secondaryQueueSubject.Value,
                2 => _tertiaryQueueSubject.Value,
                _ => null,
            };
            if (listToPickFrom == null || listToPickFrom.Count < 1)
                return;

            _currentSongIndex = listToPickFrom.IndexOf(ObservableCurrentlyPlayingSong!);

            // Next song requested
            if (IsNext)
            {
                if (IsShuffleOn)
                {
                    int newPosition;

                    // Ensure the random song picked is not the same as the current song
                    newPosition = random.Next(0, listToPickFrom.Count);
                    

                    // Update current song and add it to the shuffled playlist
                    _currentSongIndex = newPosition;
                    ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
                }
                else
                {
                    // Non-shuffle logic for next song
                    _currentSongIndex = (_currentSongIndex + 1) % listToPickFrom.Count;
                    ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
                }
            }

            // Previous song requested
            else if (IsPrevious)
            {
                _currentSongIndex--;
                if(_currentSongIndex < 0)
                    _currentSongIndex = listToPickFrom.Count - 1;

                ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
            }

            // Optional: Update observable properties or UI
            // UpdateObservableProperties();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error message!!:  {e.Message}");
        }
    }
    public async Task<bool> PauseResumeSongAsync(double currentPosition, bool isPause=false)
    {
        
        currentPositionInSec = currentPosition;
        ObservableCurrentlyPlayingSong ??= _nowPlayingSubject.Value.First();
        if (isPause) //we are pausing
        {
            await audioService.PauseAsync();
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            _playerStateSubject.OnNext(MediaPlayerState.Paused);  // Update state to paused
            ViewModel.Value.SetPlayerState(MediaPlayerState.Paused);

            _positionTimer?.Stop();


            PlayDateAndCompletionStateSongLink link = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 1,
                SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            SongsMgtService.AddPlayAndCompletionLink(link);
        }
        else //we are resuming
        {
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
            {
                return false;
            }
            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
            if (ObservableCurrentlyPlayingSong is null)
                return false;

            //ObservableCurrentlyPlayingSong.IsPlaying = true;
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            ViewModel.Value.SetPlayerState(MediaPlayerState.Playing);

            if (_positionTimer is null)
            {
                _positionTimer = new System.Timers.Timer(1000);
                _positionTimer.Elapsed += OnPositionTimerElapsed;
                _positionTimer.AutoReset = true;
            }
            _positionTimer?.Start();

            audioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);
            await audioService.ResumeAsync(currentPosition);

            PlayDateAndCompletionStateSongLink link = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 2,
                SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            SongsMgtService.AddPlayAndCompletionLink(link);
            ShowMiniPlayBackView();
        }

        return true;
    }

    public async Task<bool> StopSongAsync()
    {
        try
        {
            await audioService.PauseAsync();
            currentPosition = 0;
            CurrentlyPlayingSong.IsPlaying = false;

            _playerStateSubject.OnNext(MediaPlayerState.Stopped);  // Update state to stopped
            ViewModel.Value.SetPlayerState(MediaPlayerState.Stopped);
            _positionTimer?.Stop();
            _currentPositionSubject.OnNext(new());

            return true;
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    Stack<int> shuffleHistory = new();


    #region Audio Service Events Region
    private async void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        await PlayPreviousSongAsync();
    }
    SemaphoreSlim _playLock = new SemaphoreSlim(1, 1);
    private async void AudioService_PlayNext(object? sender, EventArgs e)
    {
        bool isLocked = await _playLock.WaitAsync(0);
        if (!isLocked)
            return;

        try
        {
            if (CurrentRepeatMode == 2) //repeat the same song
            {
                await PlaySongAsync(IsFromPreviousOrNext: true);
                return;
            }

            await PlayNextSongAsync();
        }
        finally
        {
            _playLock.Release();
        }
    }

    private int repeatCountMax;

    private async void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        if (_positionTimer != null)
        {
            _positionTimer.Stop();
            _positionTimer.Elapsed -= OnPositionTimerElapsed;
            _positionTimer.Dispose();
            _positionTimer = null;
        }
        _currentPositionSubject.OnNext(new());
        ObservableCurrentlyPlayingSong!.IsCurrentPlayingHighlight = false;

        ObservableCurrentlyPlayingSong.IsPlaying = false;
        ObservableCurrentlyPlayingSong!.IsPlayCompleted = true;
        PlayDateAndCompletionStateSongLink link = new()
        {
            DateFinished = DateTimeOffset.Now,
            WasPlayCompleted = true,
            PlayType = 3,
            SongId = ObservableCurrentlyPlayingSong.LocalDeviceId
        };
        SongsMgtService.UpdateSongDetails(ObservableCurrentlyPlayingSong);
        SongsMgtService.AddPlayAndCompletionLink(link);   
        if (CurrentRepeatMode == 2) // Repeat the same song
        {
            await PlaySongAsync();
        }

        else if (CurrentRepeatMode == 4) // Custom repeat mode
        {
            if (CurrentRepeatCount < repeatCountMax)
            {
                CurrentRepeatCount++;
                Debug.WriteLine($"Repeating song {CurrentRepeatCount}/{repeatCountMax}");
                await PlaySongAsync();
            }
            else
            {
                CurrentRepeatMode = 1;
                CurrentRepeatCount = 1;
                Debug.WriteLine("Finished repeating the song, moving to next song.");
                await PlayNextSongAsync();
            }
        }
        else
        {
            await PlayNextSongAsync();
        }


        // Additional logic to force re-triggering if the app is stuck
        await Task.Delay(2000); // Wait for 2 seconds
        if (!audioService.IsPlaying)
        {
            Debug.WriteLine("Audio service seems stuck, forcing play event.");
            AudioService_PlayEnded(null, EventArgs.Empty); // Force re-triggering if still stuck
        }

    }
    private void AudioService_PlayingChanged(object? sender, bool e)
    {
        if (isSongPlaying != e)
        {
            isSongPlaying = e;
        }

        if (isSongPlaying)
        {
            _positionTimer?.Start();
            _playerStateSubject.OnNext(MediaPlayerState.ShowPauseBtn);  // Update state to playing
        }
        else
        {
            _positionTimer?.Stop();
            _playerStateSubject.OnNext(MediaPlayerState.ShowPlayBtn);
        }
    }
    #endregion
    public async Task<bool> PlayNextSongAsync()
    {
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        if (CurrentRepeatMode == 0)
        {
            await PlaySongAsync(currentQueue: CurrentQueue, IsFromPreviousOrNext: true);
            return true;
        }

        GetPrevAndNextSongs(IsNext: true);
        return await PlaySongAsync(ObservableCurrentlyPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }
    public async Task<bool> PlayPreviousSongAsync()
    {
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;

        GetPrevAndNextSongs(IsPrevious: true);
        return await PlaySongAsync(ObservableCurrentlyPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }

    /// <summary>
    /// Seeks to a specific position in the currently SELECTED song to play but won't play it if it's paused
    /// </summary>
    /// <param name="positionInSec"></param>
    /// <returns></returns>
    public async Task SeekTo(double positionInSec)
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            return;
        }

        currentPositionInSec = positionInSec;

        if (audioService.IsPlaying)
        {
            await audioService.SetCurrentTime(positionInSec);

            PlayDateAndCompletionStateSongLink link = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 4,
                SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            SongsMgtService.AddPlayAndCompletionLink(link);
        }
    }
    public void ChangeVolume(double newPercentageValue)
    {
        try
        {
            if (CurrentlyPlayingSong is null)
            {
                return;
            }
            audioService.Volume = newPercentageValue;

            AppSettingsService.VolumeSettingsPreference.SetVolumeLevel(newPercentageValue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        audioService.Volume -= 0.1;
    }

    public void IncreaseVolume()
    {
        audioService.Volume += 0.1;
    }

    /// <summary>
    /// Toggles shuffle mode on or off
    /// </summary>
    /// <param name="isShuffleOn"></param>
    public void ToggleShuffle(bool isShuffleOn)
    {
        IsShuffleOn = isShuffleOn;
        AppSettingsService.ShuffleStatePreference.ToggleShuffleState(isShuffleOn);
        //GetPrevAndNextSongs();
    }

    /// <summary>
    /// Toggles repeat mode between 0, 1, and 2
    ///  0 for repeat OFF
    ///  1 for repeat ALL
    ///  2 for repeat ONE
    /// </summary>
    /// <returns></returns>
    public int ToggleRepeatMode()
    {
        CurrentRepeatMode = (CurrentRepeatMode + 1) % 3;
        switch (CurrentRepeatMode)
        {
            case 0:
                CurrentRepeatMode = 0;
                break;
            case 1:
                CurrentRepeatMode = 1;
                break;
            case 2:
                CurrentRepeatMode = 2;
                break;
            default:
                break;
        }
        AppSettingsService.RepeatModePreference.ToggleRepeatState();
        return CurrentRepeatMode;
    }

    double currentPositionInSec = 0;
    PlaybackInfo CurrentPlayBackInfo;
    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        currentPositionInSec++;

        double totalDurationInSeconds = ObservableCurrentlyPlayingSong.DurationInSeconds;
        double percentagePlayed = currentPositionInSec / totalDurationInSeconds;

        if (CurrentPlayBackInfo is null)
        {
            CurrentPlayBackInfo = new PlaybackInfo
            {
                CurrentPercentagePlayed = percentagePlayed, //this is the value of that CurrentPosition then takes
                CurrentTimeInSeconds = currentPositionInSec
            };
        }
        else
        {
            CurrentPlayBackInfo.CurrentPercentagePlayed = percentagePlayed;
            CurrentPlayBackInfo.CurrentTimeInSeconds = currentPositionInSec;
        }

        _currentPositionSubject.OnNext(CurrentPlayBackInfo);
    }

    double currentPosition = 0;
    #endregion

    //string PreviouslyLoadedPlaylist;
    public int CurrentQueue { get; set; } = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

    public void UpdateCurrentQueue(IList<SongModelView> songs, int QueueNumber = 1) //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
    {
        CurrentQueue = QueueNumber;
        _secondaryQueueSubject.OnNext(value: songs.ToObservableCollection());
    }
    public void UpdateSongToFavoritesPlayList(SongModelView song)
    {
        if (song is not null)
        {
            SongsMgtService.UpdateSongDetails(song);
        }
    }
    public void AddSongToQueue(SongModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Add(song);
        _nowPlayingSubject.OnNext(list);
    }
    public void RemoveSongFromQueue(SongModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Remove(song);
        _nowPlayingSubject.OnNext(list);
    }

    #region Region Search

    Dictionary<string, string> normalizationCache = new();
    List<SongModelView> SearchedSongsList;

    public void SearchSong(string songTitleOrArtistName, List<string>? selectedFilters, int Rating)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(songTitleOrArtistName) && selectedFilters?.Count == 0)
            {
                ResetSearch();
                return;
            }

            // Normalize the search term
            string normalizedSearchTerm = NormalizeAndCache(songTitleOrArtistName ?? string.Empty).ToLowerInvariant();

            // Clear the search result list
            SearchedSongsList?.Clear();

            // Step 1: Start with all songs and apply the rating filter
            var filteredSongs = SongsMgtService.AllSongs
                .Where(s => s.Rating >= Rating);

            // Step 2: Apply additional filters from selectedFilters if any
            if (selectedFilters?.Count > 0)
            {
                filteredSongs = filteredSongs.Where(s => selectedFilters.Contains("Artist")
                                                        || selectedFilters.Contains("Album")
                                                        || selectedFilters.Contains("Genre"));
            }

            // Step 3: Perform the search with normalization and comparison on the filtered list
            SearchedSongsList = filteredSongs
                .Where(s => NormalizeAndCache(s.Title).ToLowerInvariant().Contains(normalizedSearchTerm)
                            || (s.ArtistName != null && NormalizeAndCache(s.ArtistName).ToLowerInvariant().Contains(normalizedSearchTerm))
                            || (s.AlbumName != null && NormalizeAndCache(s.AlbumName).ToLowerInvariant().Contains(normalizedSearchTerm)))
                .ToList();

            // Step 4: Load the results with sorting
            Debug.WriteLine(SearchedSongsList.Count + "Search Count");
            LoadSongsWithSorting(SearchedSongsList.ToObservableCollection(), true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private string NormalizeAndCache(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Retrieve from cache if already normalized
        if (normalizationCache.TryGetValue(text, out string? cachedValue))
        {
            return cachedValue;
        }

        // Normalize the string (spaces are preserved)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            // Retain characters that are not non-spacing marks
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to Form C and cache the result
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        normalizationCache[text] = result;
        return result;
    }

    void ResetSearch()
    {
        SearchedSongsList?.Clear();
        LoadSongsWithSorting();
    }


    #endregion

    public void LoadFirstPlaylist()
    {
        var firstPlaylist = PlaylistManagementService?.AllPlaylists?.ToList();
        if (firstPlaylist is not null && firstPlaylist.Count > 0)
        {           
            SelectedPlaylistName = firstPlaylist.First().Name;
            GetSongsFromPlaylistID(firstPlaylist.FirstOrDefault().LocalDeviceId);
        }
    }

    public void AddSongToPlayListWithPlayListID(SongModelView song, PlaylistModelView playlistModel)
    {
        var anyExistingPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x=>x.Name == playlistModel.Name);
        if (anyExistingPlaylist is not null)
        {
            playlistModel = anyExistingPlaylist;
        }
        else
        {         
            playlistModel.Name = playlistModel.Name;
         
            playlistModel.TotalSongsCount = 1;
            playlistModel.TotalDuration = song.DurationInSeconds;
            playlistModel.TotalSize = song.FileSize;
        
        }
        var newPlaylistSongLinkByUserManual = new PlaylistSongLink()
        {            
            PlaylistId = playlistModel.LocalDeviceId,
            SongId = song.LocalDeviceId,
        };

        PlaylistManagementService.UpdatePlayList(playlistModel, newPlaylistSongLinkByUserManual, true);
        
        //GetAllPlaylists(); it's called in HomePageVM but let's see
    }

    public void RemoveSongFromPlayListWithPlayListID(SongModelView song, string playlistID)
    {
        var playlists = PlaylistManagementService.GetPlaylists();
        var specificPlaylist = playlists.FirstOrDefault(x => x.LocalDeviceId == playlistID);
        if (specificPlaylist is not null)
        {
            var songsInPlaylist = _secondaryQueueSubject.Value;
            songsInPlaylist.Remove(song);
            _secondaryQueueSubject.OnNext(songsInPlaylist);
            specificPlaylist.TotalSongsCount -= 1;
            specificPlaylist.TotalDuration -= song.DurationInSeconds;
            specificPlaylist.TotalSize -= song.FileSize;
        }

        PlaylistManagementService.UpdatePlayList(specificPlaylist, IsRemoveSong: true);
    }

    public List<SongModelView> GetSongsFromPlaylistID(string playlistID)
    {
        try
        {
            var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.LocalDeviceId == playlistID);
            var songsIdsFromPL = new HashSet<string>(PlaylistManagementService.GetSongsIDsFromPlaylistID(specificPlaylist.LocalDeviceId));
            var songsFromPlaylist = SongsMgtService.AllSongs;
            List<SongModelView> songsinpl = new();
            songsinpl?.Clear();
            foreach (var song in songsFromPlaylist)
            {
                if (songsIdsFromPL.Contains(song.LocalDeviceId))
                {
                    songsinpl.Add(song);
                }
            }
            SelectedPlaylistName = specificPlaylist.Name;
            
            if (songsinpl is null)
            {
                return Enumerable.Empty<SongModelView>().ToList();
            }
            _secondaryQueueSubject.OnNext(songsinpl.ToObservableCollection());
            return songsinpl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<SongModelView>().ToList();
        }

    }

    public ObservableCollection<PlaylistModelView> GetAllPlaylists()
    {
        PlaylistManagementService.GetPlaylists();
        AllPlaylists = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
        return AllPlaylists;
    }

    public ObservableCollection<ArtistModelView> GetAllArtists()
    {
        SongsMgtService.GetArtists();
        if (SongsMgtService.AllArtists is null)
            return Enumerable.Empty<ArtistModelView>().ToObservableCollection();

        AllArtists = new ObservableCollection<ArtistModelView>(SongsMgtService.AllArtists);
        return AllArtists;
    }
    public ObservableCollection<AlbumModelView> GetAllAlbums()
    {
        SongsMgtService.GetAlbums();
        AllAlbums = new ObservableCollection<AlbumModelView>(SongsMgtService.AllAlbums);
        return AllAlbums;
    }
    

    public ObservableCollection<SongModelView> GetAllArtistsAlbumSongsAlbumID(string albumID)
    {
        try
        {
            var artistID = SongsMgtService.AllLinks.FirstOrDefault(x=>x.AlbumId == albumID)!.ArtistId;

            var allSongIDsLinkedToArtistID = SongsMgtService.AllLinks.
                Where(x => x.ArtistId == artistID)
                .Select(x => x.SongId)
                .ToList();

            var allSongsLinkedToArtist = SongsMgtService.AllSongs
                .Where(song => allSongIDsLinkedToArtistID.Contains(song.LocalDeviceId))
                .ToObservableCollection();

            return allSongsLinkedToArtist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from album: {ex.Message}");
            return (Enumerable.Empty<SongModelView>().ToObservableCollection());
        }
    }


    public bool DeletePlaylistThroughID(string playlistID)
    {
        //TODO: DO THIS TOO
        throw new NotImplementedException();
    }

    public void DeleteSongFromHomePage(SongModelView song)
    {
        // Get the current list from the subject
        var list = nowPlayingShuffledOrNotSubject.ToList();

        // Find the index of the song using its unique ID
        var index = list.FindIndex(x => x.LocalDeviceId == song.LocalDeviceId); // Assuming each song has a unique Id

        if (index != -1) // If the song was found
        {
            list.RemoveAt(index); // Remove the song at the found index

            // Push the updated list back into the subject
            _nowPlayingSubject.OnNext(list.ToObservableCollection());
            nowPlayingShuffledOrNotSubject = list.ToObservableCollection();
        }


        SongsMgtService.DeleteSongFromDB(song);
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }
    public async Task MultiDeleteSongFromHomePage(ObservableCollection<SongModelView> songs)
    {
        // Get the current list from the subject
        var list = _nowPlayingSubject.Value;

        // Filter out all songs in the passed collection
        list = list.Where(x => !songs.Any(s => s.LocalDeviceId == x.LocalDeviceId)).ToObservableCollection();
        
        _nowPlayingSubject.OnNext(list.ToObservableCollection());        

        // Delete the songs from the database
        await SongsMgtService.MultiDeleteSongFromDB(songs);

        // Update the _nowPlayingSubject with the latest list of songs
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }

}