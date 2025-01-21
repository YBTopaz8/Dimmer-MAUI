namespace Dimmer_MAUI.Utilities.Services;

public partial class PlaybackUtilsService : ObservableObject
{
    [ObservableProperty]
    public partial PlaybackSource CurrentPlaybackSource { get; set; } = PlaybackSource.HomePage;


    #region Playback Control Region

    private CancellationTokenSource? _debounceTokenSource;

    public bool PlaySelectedSongsOutsideApp(List<string> filePaths)
    {
        if (filePaths == null || filePaths.Count < 1)
            return false;

        // Filter the files upfront by extension and size
        var filteredFiles = filePaths
            .Where(path => new[] { ".mp3", ".flac", ".wav", ".m4a" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => new FileInfo(path).Length >= 1000) // Exclude small files (< 1KB)
            .ToList();

        if (filteredFiles.Count == 0)
            return false;

        // Use a HashSet for fast lookup to avoid duplicates
        var processedFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var existingSongDictionary = _tertiaryQueueSubject.Value?.ToDictionary(song => song.FilePath!, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, SongModelView>();
        var allSongs = new List<SongModelView>();


        foreach (var file in filteredFiles)
        {
            if (!processedFilePaths.Add(file))
                continue; // Skip duplicate file paths

            if (existingSongDictionary.TryGetValue(file, out var existingSong))
            {
                // Use the existing song
                allSongs.Add(existingSong);
            }
            else
            {
                // Create a new SongModelView
                var track = new Track(file);
                var fileInfo = new FileInfo(file);

                var newSong = new SongModelView
                {

                    Title = track.Title,
                    GenreName = string.IsNullOrEmpty(track.Genre) ? "Unknown Genre" : track.Genre,
                    ArtistName = string.IsNullOrEmpty(track.Artist) ? "Unknown Artist" : track.Artist,
                    AlbumName = string.IsNullOrEmpty(track.Album) ? "Unknown Album" : track.Album,

                    SampleRate = track.SampleRate,
                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,
                    IsPlayedFromOutsideApp = true,

                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc")),
                    CoverImagePath = LyricsService.SaveOrGetCoverImageToFilePath(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData),
                };
                if (track.Year is not null)
                {
                    newSong.ReleaseYear = (int)track.Year;
                }
                if (track.TrackNumber is not null)
                {
                    newSong.ReleaseYear = (int)track.TrackNumber;
                }
                SongsMgtService.UpdateSongDetails(newSong);
                allSongs.Add(newSong);
            }
        }

        ReplaceAndPlayQueue(allSongs, playFirst: true);
        CurrentPlaybackSource = PlaybackSource.External;
        return true;
    }


    public bool PlaySelectedSongsOutsideAppDebounced(List<string> filePaths)
    {
        // Cancel previous execution if still pending
        _debounceTokenSource?.Cancel();
        _debounceTokenSource = new CancellationTokenSource();

        try
        {
            return PlaySelectedSongsOutsideApp(filePaths);
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }


    bool isLoadingSongs;
    private void ShowMiniPlayBackView()
    {
#if WINDOWS

        MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong!);

#endif
    }

    public AppState CurrentAppState = AppState.OnForeGround;


    private Random random = Random.Shared;  // Reuse the same Random instance
    bool isTrueShuffle = false;  // Set this based on whether shuffle is enabled

    //public ObservableCollection<SongModelView> mobileSongOrder; // yes!
    //because mobileSongOrder is the order of songs on the mobile device
    private void GetPrevAndNextSongs(bool isNext = false, bool isPrevious = false)
    {
        if (ObservableCurrentlyPlayingSong is null || _playbackQueue.Value is null || _playbackQueue.Value.Count == 0)
        {
            return;
        }

        try
        {
            int currentActiveIndex = _playbackQueue.Value.IndexOf(ObservableCurrentlyPlayingSong!);

            if (currentActiveIndex == -1)
            {
                // Handle error: current song not found in the active queue
                Debug.WriteLine("Error: Currently playing song not found in the active queue.");
                return;
            }

            if (isNext)
            {
                int nextIndex = (currentActiveIndex + 1) % _playbackQueue.Value.Count;
                ObservableCurrentlyPlayingSong = _playbackQueue.Value[nextIndex];
            }
            else if (isPrevious)
            {
                int previousIndex = (currentActiveIndex - 1 + _playbackQueue.Value.Count) % _playbackQueue.Value.Count;
                ObservableCurrentlyPlayingSong = _playbackQueue.Value[previousIndex];
            }

            // Update _currentSongIndex if needed for other purposes
            _currentSongIndex = _playbackQueue.Value.IndexOf(ObservableCurrentlyPlayingSong);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error during GetPrevAndNextSongs: {e.Message}");
        }
    }
    private void UpdateActiveQueue()
    {
        if (IsShuffleOn)
        {
            _playbackQueue.OnNext(ShuffleList(SongsMgtService.AllSongs.ToObservableCollection())); // Shuffle your master list
        }
        else
        {
            _playbackQueue.OnNext(SongsMgtService.AllSongs.ToObservableCollection()); // Use the original order
        }
    }

    private ObservableCollection<SongModelView> ShuffleList(ObservableCollection<SongModelView> list)
    {
        var shuffledList = list.OrderBy(_ => random.Next()).ToObservableCollection(); // Simple shuffle
        return shuffledList;
    }

    public bool PlaySong(SongModelView? song, PlaybackSource source, double positionInSec = 0)
    {
        if (song == null)
            return false;

        CurrentPlaybackSource = source;

        if (AudioService.IsPlaying)
        {
            AudioService.Pause();
        }

        ObservableCurrentlyPlayingSong = song;
        var coverImage = GetCoverImage(song.FilePath, true);
        AudioService.Initialize(song, coverImage);

        if (positionInSec > 0)
        {
            AudioService.Resume(positionInSec);            
            currentPositionInSec = positionInSec;
        }
        else
        {
            AudioService.Play();
            currentPositionInSec = 0;
        }

        StartPositionTimer();
        _playerStateSubject.OnNext(MediaPlayerState.Playing);
        song.IsPlaying = true; // Update playing status
        UpdateSongPlaybackDetails(song);
        return true;
    }

    public bool PlaySong(SongModelView? song, bool isPreview = true)
    {
        if (song == null)
        {
            // Consider more informative error handling
            return false;
        }

        if (AudioService.IsPlaying)
        {
            AudioService.Pause();
        } 

        var sixtyPercent = song.DurationInSeconds * 0.6;
        AudioService.Initialize(song);
        AudioService.Volume = 0.2;
        AudioService.Play();
        AudioService.SetCurrentTime(sixtyPercent);
        _playerStateSubject.OnNext(MediaPlayerState.Previewing);
        return true;
    }

    public bool PauseResumeSong(double currentPosition, bool isPause = false)
    {
        ObservableCurrentlyPlayingSong ??= _playbackQueue.Value.FirstOrDefault();
        if (ObservableCurrentlyPlayingSong == null)
            return false;

        if (isPause)
        {
            AudioService.Pause();
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            StopPositionTimer();
            _playerStateSubject.OnNext(MediaPlayerState.Paused);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Pause, currentPosition);
        }
        else
        {
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
                return false;
            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
            AudioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);
            AudioService.Resume(currentPosition);
            StartPositionTimer();
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Resume, currentPosition);
        }
        return true;
    }
    public bool StopSong()
    {
        AudioService.Pause();
        StopPositionTimer();
        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }
        _playerStateSubject.OnNext(MediaPlayerState.Stopped);
        _currentPositionSubject.OnNext(new PlaybackInfo());
        return true;
    }
    Random _random { get; } = new Random();
    public void ShuffleQueue()
    {
        var currentQueue = _playbackQueue.Value.ToList();
        var shuffledQueue = currentQueue.OrderBy(_ => _random.Next()).ToObservableCollection();
        _playbackQueue.OnNext(shuffledQueue);
    }

    #region Audio Service Events Region
   
    private int repeatCountMax;

    

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
    public void PlayNextSong(bool isUserInitiated = true)
    {
        if (ObservableCurrentlyPlayingSong == null)
            return;

        if (CurrentRepeatMode == 2)
        {
            PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
            return;
        }

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Skipped);
        }

        var currentQueue = _playbackQueue.Value;
        int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentIndex < currentQueue.Count - 1)
        {
            PlaySong(currentQueue[currentIndex + 1], CurrentPlaybackSource);
            return;
        }
        else if (CurrentRepeatMode == 1 && currentQueue.Any()) // Repeat All
        {
            PlaySong(currentQueue.First(), CurrentPlaybackSource);
            return;
        }
        // If not repeat all and at the end, do nothing or stop playback
    }
    public void PlayPreviousSong(bool isUserInitiated = true)
    {
        if (ObservableCurrentlyPlayingSong == null)
            return;

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Skipped);
        }

        var currentQueue = _playbackQueue.Value;
        int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentIndex > 0)
        {
            PlaySong(currentQueue[currentIndex - 1], CurrentPlaybackSource);
        }
        else if (CurrentRepeatMode == 1 && currentQueue.Any()) // Repeat All, go to last song
        {
            PlaySong(currentQueue.Last(), CurrentPlaybackSource);
        }
        // If not repeat all and at the beginning, do nothing
    }

    /// <summary>
    /// Seeks to a specific position in the currently SELECTED song to play but won't play it if it's paused
    /// </summary>
    /// <param name="positionInSec"></param>
    /// <returns></returns>
    public void SeekTo(double positionInSec)
    {
        currentPositionInSec = positionInSec;
        if (ObservableCurrentlyPlayingSong is null)
        {
            return;
        }
        var currentPercentage = currentPositionInSec / ObservableCurrentlyPlayingSong.DurationInSeconds * 100;


        if (AudioService.IsPlaying)
        {
            AudioService.SetCurrentTime(positionInSec);

            PlayDateAndCompletionStateSongLink links = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 4,
                SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            if (currentPercentage >= 80)
            {
                links.PlayType = 7;
            }

            SongsMgtService.AddPDaCStateLink(links);
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
            AudioService.Volume = newPercentageValue;

            AppSettingsService.VolumeSettingsPreference.SetVolumeLevel(newPercentageValue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        AudioService.Volume -= 0.1;
    }

    public void IncreaseVolume()
    {
        AudioService.Volume += 0.1;
    }

    /// <summary>
    /// Toggles shuffle mode on or off
    /// </summary>
    /// <param name="isShuffleOn"></param>
    public void ToggleShuffle(bool isShuffleOn)
    {
        IsShuffleOn = isShuffleOn;
        AppSettingsService.ShuffleStatePreference.ToggleShuffleState(isShuffleOn);
        UpdateActiveQueue();

        //GetPrevAndNextSongs();
    }



    /// <summary>
    /// Toggles repeat mode between 0, 1, and 2
    ///  0 for repeat OFF
    ///  1 for repeat ALL
    ///  2 for repeat ONE
    /// </summary>
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
    
    double currentPosition = 0;
    #endregion

    // Method to add songs to the playback queue
    public void AddToImmediateNextInQueue(List<SongModelView> songs, bool playNext = false)
    {
        var currentQueue = _playbackQueue.Value.ToList(); // Work with a copy

        if (playNext && ObservableCurrentlyPlayingSong != null)
        {
            int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);
            if (currentIndex != -1)
            {
                currentQueue.InsertRange(currentIndex + 1, songs);
            }
            else
            {
                currentQueue.AddRange(songs); 
            }
        }
        else
        {
            currentQueue.AddRange(songs);
        }

        _playbackQueue.OnNext(new ObservableCollection<SongModelView>(currentQueue.Distinct().ToList())); 
    }


    public void ReplaceAndPlayQueue(List<SongModelView> songs, bool playFirst = true)
    {
        _playbackQueue.OnNext(new ObservableCollection<SongModelView>(songs));
        if (playFirst && songs.Count != 0)
        {
            PlaySong(songs.First(), source: CurrentPlaybackSource);
        }
    }

    #region Audio Service Event Handlers
    private void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        StopPositionTimer();
        _currentPositionSubject.OnNext(new PlaybackInfo());

        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            ObservableCurrentlyPlayingSong.IsPlayCompleted = true;
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Completed);
        }

        switch (CurrentRepeatMode)
        {
            case 2: // Repeat One
                PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
                break;
            case 4: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.CustomRepeat);
                    PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
                }
                else
                {
                    CurrentRepeatMode = 1;
                    CurrentRepeatCount = 1;
                    PlayNextSong(false);
                }
                break;
            default:
                PlayNextSong(false);
                break;
        }
    }


    private void AudioService_PlayNext(object? sender, EventArgs e)
    {
        PlayNextSong();
    }

    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        PlayPreviousSong();
    }
    #endregion

    #region Helpers
    private void StartPositionTimer()
    {
        if (_positionTimer == null)
        {
            _positionTimer = new System.Timers.Timer(1000);
            _positionTimer.Elapsed += OnPositionTimerElapsed;
            _positionTimer.AutoReset = true;
        }
        _positionTimer.Start();
    }

    private void StopPositionTimer()
    {
        _positionTimer?.Stop();
    }

    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (AudioService.IsPlaying)
        {
            double totalDurationInSeconds = ObservableCurrentlyPlayingSong.DurationInSeconds;
            double percentagePlayed = currentPositionInSec / totalDurationInSeconds;
            currentPositionInSec = AudioService.CurrentPosition;
            _currentPositionSubject.OnNext(new PlaybackInfo { CurrentTimeInSeconds = currentPositionInSec, CurrentPercentagePlayed = percentagePlayed });
#if WINDOWS
            // GeneralStaticUtilities.UpdateTaskBarProgress(currentPositionInSec / (ObservableCurrentlyPlayingSong?.DurationInSeconds ?? 1.0));
#endif
        }
    }

    private void UpdateSongPlaybackDetails(SongModelView song)
    {
        song.IsCurrentPlayingHighlight = true;
        song.IsPlaying = true;
        (song.HasSyncedLyrics, song.SyncLyrics) = LyricsService.HasLyrics(song);
        if (song.DurationInSeconds == 0)
        {
            song.DurationInSeconds = AudioService.Duration;
        }
        SongsMgtService.UpdateSongDetails(song);
        // ShowMiniPlayBackView();
    }

    private void UpdateSongPlaybackState(SongModelView song, PlayType playType, double? position = null)
    {
        var link = new PlayDateAndCompletionStateSongLink
        {
            DatePlayed = DateTime.Now,
            PlayType = (int)playType,
            SongId = song.LocalDeviceId,
            PositionInSeconds = position is null? 0 : (double)position
        };
        SongsMgtService.AddPDaCStateLink(link);
    }
    #endregion

}
public enum PlaybackSource
{
    HomePage,
    Playlist,
    External
}

public enum MediaPlayerState
{
    Stopped,
    Playing,
    Paused,
    Loading,
    Error,
    Previewing,
    LyricsLoad,
    ShowPlayBtn,
    ShowPauseBtn,
    RefreshStats,
    Initialized,
    Ended,
    CoverImageDownload,
    LoadingSongs,    
    SyncingData,
    DoneScanningData,
    
}

public enum PlayType
{
    Play = 0,
    Pause = 1,
    Resume = 2,
    Completed = 3,
    Seeked = 4,
    Skipped = 5,
    Restarted = 6,
    RestSeekRestartedarted = 7,
    CustomRepeat = 8
}