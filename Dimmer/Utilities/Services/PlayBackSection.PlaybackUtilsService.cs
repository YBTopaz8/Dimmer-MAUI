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

    public AppState CurrentAppState = AppState.OnForeGround;


    private Random random = Random.Shared;  // Reuse the same Random instance
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

        if (DimmerAudioService.IsPlaying)
        {
            DimmerAudioService.Pause();
        }

        ObservableCurrentlyPlayingSong = song;
        var coverImage = GetCoverImage(song.FilePath, true);
        DimmerAudioService.Initialize(song, coverImage);

        if (positionInSec > 0)
        {
            DimmerAudioService.Resume(positionInSec);            
            currentPositionInSec = positionInSec;
        }
        else
        {
            DimmerAudioService.Play();
            currentPositionInSec = 0;
        }

        StartPositionTimer();
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = true;
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

        if (DimmerAudioService.IsPlaying)
        {
            DimmerAudioService.Pause();
        } 

        var sixtyPercent = song.DurationInSeconds * 0.6;
        DimmerAudioService.Initialize(song);
        DimmerAudioService.Volume = 1;
        DimmerAudioService.Play();
        DimmerAudioService.SetCurrentTime(sixtyPercent);
        _playerStateSubject.OnNext(MediaPlayerState.Previewing);
        UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Play, 0);
        return true;
    }

    public bool PauseResumeSong(double currentPosition, bool isPause = false)
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            ObservableCurrentlyPlayingSong = _playbackQueue.Value.ToList().FirstOrDefault();
            
            if (ObservableCurrentlyPlayingSong == null)
                return false;
        }
        

        if (isPause)
        {
            DimmerAudioService.Pause();
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
            DimmerAudioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);
            DimmerAudioService.Resume(currentPosition);
            StartPositionTimer();
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Resume, currentPosition);
        }
        return true;
    }
    public bool StopSong()
    {
        DimmerAudioService.Pause();
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

    

    private void DimmerAudioService_PlayingChanged(object? sender, bool e)
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

        if (CurrentRepeatMode == RepeatMode.One)
        {

            PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
            return;
        }

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Skipped);
        }

        var currentQueue = _playbackQueue.Value.Count == 0? SongsMgtService.AllSongs.ToObservableCollection() : _playbackQueue.Value;
        
        int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentQueue.Count < 1 || currentIndex < currentQueue.Count - 1)
        {
            PlaySong(currentQueue[currentIndex + 1], CurrentPlaybackSource);        
            return;
        }

        else if (CurrentRepeatMode == RepeatMode.All && currentQueue.Any()) // Repeat All
        {
            PlaySong(currentQueue.First(), CurrentPlaybackSource);
            return;
        }
        // If not repeat all and at the end, do nothing or stop playback
    }

    int prevCounter = 0;
    public void PlayPreviousSong(bool isUserInitiated = true)
    {
        if (ObservableCurrentlyPlayingSong == null)
            return;

        if (prevCounter == 1)
        {
            var currentQueue = _playbackQueue.Value;
            int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Previous);            
            PlaySong(currentQueue[currentIndex - 1], CurrentPlaybackSource);
            prevCounter = 0;
            return;
        }
        UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Restarted);
        PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
        if (CurrentRepeatMode == RepeatMode.One)
        {
            return;
        }
        prevCounter++;
    }
    double CurrentPercentage = 0;
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
        var CurrentPercentage = currentPositionInSec / ObservableCurrentlyPlayingSong.DurationInSeconds * 100;

#if ANDROID
        DimmerAudioService.SetCurrentTime(positionInSec);

        PlayDateAndCompletionStateSongLink links = new()
        {
            DatePlayed = DateTime.Now,
            PlayType = 4,
            SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
            PositionInSeconds = currentPositionInSec
        };
        if (CurrentPercentage >= 80)
        {
            links.PlayType = 7;
        }

        SongsMgtService.AddPDaCStateLink(links);
        return;
#endif
        if (DimmerAudioService.IsPlaying)
        {
            DimmerAudioService.SetCurrentTime(positionInSec);

            PlayDateAndCompletionStateSongLink linkss = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 4,
                SongId = ObservableCurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            if (CurrentPercentage >= 80)
            {
                linkss.PlayType = 7;
            }

            SongsMgtService.AddPDaCStateLink(linkss);
        }
    }
    public void ChangeVolume(double newVolumeOver1)
    {
        try
        {
            if (CurrentlyPlayingSong is null)
            {
                return;
            }
            DimmerAudioService.Volume = newVolumeOver1;

            AppSettingsService.VolumeSettingsPreference.SetVolumeLevel(newVolumeOver1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        DimmerAudioService.Volume -= 0.01;
    }
    public double VolumeLevel => DimmerAudioService.Volume;
    public void IncreaseVolume()
    {
        DimmerAudioService.Volume += 0.01;
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
        CurrentRepeatMode = (RepeatMode)(((int)CurrentRepeatMode + 1) % 3); // Cycle through enum values 0, 1, 2
        AppSettingsService.RepeatModePreference.ToggleRepeatState((int)CurrentRepeatMode); // Store as int
        return (int)CurrentRepeatMode;
    }

    double currentPositionInSec = 0;
    #endregion

    // Method to add songs to the playback queuenext
    public void AddToImmediateNextInQueue(List<SongModelView> songs, bool playNext = true)
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

        _playbackQueue.OnNext(new ObservableCollection<SongModelView>([.. currentQueue.Distinct()])); 
    }


    public void ReplaceAndPlayQueue(List<SongModelView> songs, bool playFirst = false)
    {
        _playbackQueue.OnNext(new ObservableCollection<SongModelView>(songs));
        if (playFirst && songs.Count != 0)
        {
            PlaySong(songs.First(), source: CurrentPlaybackSource);
        }
    }

    #region Audio Service Event Handlers
    private void DimmerAudioService_PlayEnded(object? sender, EventArgs e)
    {
        StopPositionTimer();
        _currentPositionSubject.OnNext(new PlaybackInfo());

        if (ObservableCurrentlyPlayingSong != null)
        {
            //ObservableCurrentlyPlayingSong.IsPlaying = false;
            //ObservableCurrentlyPlayingSong.IsPlayCompleted = true;
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Completed);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.One: // Repeat One
                PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
                break;
            case RepeatMode.Custom: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax) // still on repeat one for same song (later can be same PL/album etc)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.CustomRepeat);
                    PlaySong(ObservableCurrentlyPlayingSong, CurrentPlaybackSource);
                }
                else
                {
                    CurrentRepeatMode = RepeatMode.All;
                    CurrentRepeatCount = 1;
                    PlayNextSong(false);
                }
                break;
            default:
                PlayNextSong(false);
                break;
        }
    }


    private void DimmerAudioService_PlayNext(object? sender, EventArgs e)
    {
        PlayNextSong();
    }

    private void DimmerAudioService_PlayPrevious(object? sender, EventArgs e)
    {
        PlayPreviousSong();
    }
    #endregion


    //public void SetEqualizerSettings(float[] bands)
    //{
    //    try
    //    {
    //        DimmerAudioService.ApplyEqualizerSettings(bands);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Error setting equalizer bands: {ex.Message}");
    //        // Handle error appropriately, e.g., show a message to the user
    //    }
    //}
    //public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    //{
    //    try
    //    {
    //        DimmerAudioService.ApplyEqualizerPreset(presetName);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Error applying equalizer preset '{presetName}': {ex.Message}");
    //        // Handle error appropriately
    //    }
    //}

    public ObservableCollection<SongModelView> GetCurrentQueue()
    {
        return new ObservableCollection<SongModelView>(_playbackQueue.Value); // Return a copy to avoid direct modification from outside
    }
    public void ClearQueue()
    {
        _playbackQueue.OnNext(new ObservableCollection<SongModelView>()); // Clear the queue
    }
    public MediaPlayerState GetPlaybackState()
    {
        return _playerStateSubject.Value; // Get the current playback state
    }

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
        if (DimmerAudioService.IsPlaying)
        {
            double totalDurationInSeconds = ObservableCurrentlyPlayingSong.DurationInSeconds;
            double percentagePlayed = currentPositionInSec / totalDurationInSeconds;
            currentPositionInSec = DimmerAudioService.CurrentPosition;
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
            song.DurationInSeconds = DimmerAudioService.Duration;
        }
        SongsMgtService.UpdateSongDetails(song);
        // ShowMiniPlayBackView();
    }

    private void UpdateSongPlaybackState(SongModelView? song, PlayType playType, double? position = null)
    {
        if (song is null)
        {
            return;
        }
        var link = new PlayDateAndCompletionStateSongLink
        {
            DatePlayed = DateTime.Now,
            PlayType = (int)playType,
            SongId = song.LocalDeviceId,
            PositionInSeconds = position is null? 0 : (double)position,
            WasPlayCompleted = playType == PlayType.Completed,
            
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

/// <summary>
/// Indicates the type of play action performed.    
/// Possible VALID values for <see cref="PlayType"/>:
/// <list type="bullet">
/// <item><term>0</term><description>Play</description></item>
/// <item><term>1</term><description>Pause</description></item>
/// <item><term>2</term><description>Resume</description></item>
/// <item><term>3</term><description>Completed</description></item>
/// <item><term>4</term><description>Seeked</description></item>
/// <item><term>5</term><description>Skipped</description></item>
/// <item><term>6</term><description>Restarted</description></item>
/// <item><term>7</term><description>SeekRestarted</description></item>
/// <item><term>8</term><description>CustomRepeat</description></item>
/// <item><term>9</term><description>Previous</description></item>
/// </list>
/// </summary>
public enum PlayType
{
    Play = 0,
    Pause = 1,
    Resume = 2,
    Completed = 3,
    Seeked = 4,
    Skipped = 5,
    Restarted = 6,
    SeekRestarted = 7,
    CustomRepeat = 8,
    Previous=9
}
public enum RepeatMode // Using enum for repeat modes
{
    Off = 0,
    All = 1,
    One = 2,
    Custom = 3, // If you re-implement Custom Repeat
}