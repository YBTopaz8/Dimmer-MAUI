﻿using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.Services;

public partial class PlaybackUtilsService : ObservableObject
{
    [ObservableProperty]
    public partial PlaybackSource CurrentPlaybackSource { get; set; } = PlaybackSource.MainQueue;

    #region Playback Control Region

    private CancellationTokenSource? _debounceTokenSource;

    public bool PlaySelectedSongsOutsideApp(List<string> filePaths)
    {
        if (filePaths == null || filePaths.Count < 1)
            return false;

        // Filter the files upfront by extension and size
        List<string> filteredFiles = filePaths
            .Where(path => new[] { ".mp3", ".flac", ".wav", ".m4a" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => new FileInfo(path).Length >= 1000) // Exclude small files (< 1KB)
            .ToList();

        if (filteredFiles.Count == 0)
            return false;

        // Use a HashSet for fast lookup to avoid duplicates
        HashSet<string> processedFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, SongModelView> existingSongDictionary = _tertiaryQueueSubject.Value?.ToDictionary(song => song.FilePath!, StringComparer.OrdinalIgnoreCase) ?? [];
        List<SongModelView> allSongs = new List<SongModelView>();


        foreach (string? file in filteredFiles)
        {
            if (!processedFilePaths.Add(file))
                continue; // Skip duplicate file paths

            if (existingSongDictionary.TryGetValue(file, out SongModelView? existingSong))
            {
                // Use the existing song
                allSongs.Add(existingSong);
            }
            else
            {
                // Create a new SongModelView
                Track track = new Track(file);
                FileInfo fileInfo = new FileInfo(file);

                SongModelView newSong = new SongModelView
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

        ReplaceAndPlayQueue(allSongs[0], allSongs, PlaybackSource.External);
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
    private ObservableCollection<SongModelView> _internalNowPlayingQueue { get; set; }
    private void UpdateActiveQueue()
    {
        _internalNowPlayingQueue  = _playbackQueue.Value.ToObservableCollection(); // Work with a copy
        if (IsShuffleOn)
        {
            _playbackQueue.OnNext(ShuffleList(_internalNowPlayingQueue)); // Shuffle your master list

        }
        else
        {
            _playbackQueue.OnNext(_internalNowPlayingQueue); // Use the original order
        }
    }
    private ObservableCollection<SongModelView> ShuffleList(ObservableCollection<SongModelView> list)
    {
        ObservableCollection<SongModelView> shuffledList = list.OrderBy(_ => random.Next()).ToObservableCollection(); // Simple shuffle
        return shuffledList;
    }

    public bool PlaySongWithPosition(SongModelView song, double positionInSec)
    {
        if (song == null)
            return false;

        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        song.IsCurrentPlayingHighlight = false;
        

        if (DimmerAudioService.IsPlaying)
        {
            DimmerAudioService.Pause();
        }

        ObservableCurrentlyPlayingSong = song;
        byte[]? coverImage = GetCoverImage(song.FilePath, true);
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
            return false;

        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        song.IsCurrentPlayingHighlight = false;

        ObservableCurrentlyPlayingSong = song;
        byte[]? coverImage = GetCoverImage(song.FilePath, true);
        DimmerAudioService.Initialize(song, coverImage);

        DimmerAudioService.Play();
        currentPositionInSec = 0;


        StartPositionTimer();
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = true;
        _playerStateSubject.OnNext(MediaPlayerState.Playing);
        song.IsPlaying = true; // Update playing status
        UpdateSongPlaybackDetails(song);
        return true;
    }
       

    public bool PauseResumeSong(double currentPosition, bool isPause = false)
    {
        if (ObservableCurrentlyPlayingSong is null | ObservableCurrentlyPlayingSong.LocalDeviceId == null)
        {
            if (ViewModel.Value.TemporarilyPickedSong is not null)
            {
                ObservableCurrentlyPlayingSong = ViewModel.Value.TemporarilyPickedSong;                
            }
            else
            {
                ObservableCurrentlyPlayingSong = _playbackQueue.Value.ToList().FirstOrDefault();
            }
            
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
            byte[]? coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
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
        List<SongModelView> currentQueue = _playbackQueue.Value.ToList();
        ObservableCollection<SongModelView> shuffledQueue = currentQueue.OrderBy(_ => _random.Next()).ToObservableCollection();
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

    int CurrentIndexInMasterList = 0;
    public void PlayNextSong(bool isUserInitiated = true)
    {
        if (ObservableCurrentlyPlayingSong == null)
            return;

        if (CurrentRepeatMode == RepeatMode.One)
        {
            PlaySong(ObservableCurrentlyPlayingSong);
            return;
        }

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Skipped);
        }

        ObservableCollection<SongModelView> currentQueue = _playbackQueue.Value.Count == 0? SongsMgtService.AllSongs.ToObservableCollection() : _playbackQueue.Value;

        CurrentIndexInMasterList = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);

        if (currentQueue.Count < 1 || CurrentIndexInMasterList < currentQueue.Count - 1)
        {
            PlaySong(currentQueue[CurrentIndexInMasterList + 1]);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Play);
            return;
        }

        else if (CurrentRepeatMode == RepeatMode.All && currentQueue.Any()) // Repeat All
        {
            PlaySong(currentQueue.FirstOrDefault());
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Play);
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
            ObservableCollection<SongModelView> currentQueue = _playbackQueue.Value;
            int currentIndex = currentQueue.IndexOf(ObservableCurrentlyPlayingSong);
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Previous);            
            PlaySong(currentQueue[currentIndex - 1]);
            prevCounter = 0;
            return;
        }
        UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Restarted);
        PlaySong(ObservableCurrentlyPlayingSong);
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
        double CurrentPercentage = currentPositionInSec / ObservableCurrentlyPlayingSong.DurationInSeconds * 100;

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
        UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Seeked, currentPositionInSec);
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
        List<SongModelView> currentQueue = _playbackQueue.Value.ToList(); // Work with a copy

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


    public void ReplaceAndPlayQueue(SongModelView song, List<SongModelView> songs, PlaybackSource source)
    {
        CurrentPlaybackSource=  source;
        PlaySong(song);

        switch (source)
        {
            case PlaybackSource.MainQueue:
                break;
            case PlaybackSource.AlbumsQueue:
                break;
            case PlaybackSource.SearchQueue:
                break;
            case PlaybackSource.External:
                break;
            case PlaybackSource.PlaylistQueue:
                break;
            case PlaybackSource.ArtistQueue:
                break;
            default:
                break;
        }

        _playbackQueue.OnNext([.. songs]);        
    }

    #region Audio Service Event Handlers
    private void DimmerAudioService_PlayEnded(object? sender, EventArgs e)
    {
        StopPositionTimer();
        _currentPositionSubject.OnNext(new PlaybackInfo());

        if (ObservableCurrentlyPlayingSong != null)
        {
            UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.Completed);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.One: // Repeat One
                PlaySong(ObservableCurrentlyPlayingSong);
                break;
            case RepeatMode.Custom: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax) // still on repeat one for same song (later can be same PL/album etc)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(ObservableCurrentlyPlayingSong, PlayType.CustomRepeat);
                    PlaySong(ObservableCurrentlyPlayingSong);
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
        return [.. _playbackQueue.Value]; // Return a copy to avoid direct modification from outside
    }
    public void ClearQueue()
    {
        _playbackQueue.OnNext([]); // Clear the queue
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
        PlayDateAndCompletionStateSongLink link = new PlayDateAndCompletionStateSongLink
        {
            DatePlayed = DateTime.Now,
            PlayType = (int)playType,
            SongId = song.LocalDeviceId,
            PositionInSeconds = position is null ? 0 : (double)position,
            WasPlayCompleted = playType == PlayType.Completed,

        };
        SongsMgtService.AddPDaCStateLink(link);
        string MessageContent = string.Empty;
        if (ViewModel.Value.CurrentUserOnline != null)
        {
            ParseUser CurrentUserOnline = ViewModel.Value.CurrentUserOnline;
            position ??= 0;
            TimeSpan pos = TimeSpan.FromSeconds((long)position);
            string formattedPosition = pos.ToString(@"mm\:ss");

            switch (playType)
            {

                case PlayType.Play:
                    MessageContent = $"{CurrentUserOnline.Username} Started Playing {song.Title} - {song.ArtistName}";
                    break;
                case PlayType.Pause:
                    MessageContent = $"{CurrentUserOnline.Username} Paused {song.Title} - {song.ArtistName} at {formattedPosition}s";
                    ;
                    break;
                case PlayType.Resume:
                    MessageContent = $"{CurrentUserOnline.Username} Resumed {song.Title} - {song.ArtistName} at {formattedPosition}s";

                    break;
                case PlayType.Completed:
                    MessageContent = $"{CurrentUserOnline.Username} Finished Playing {song.Title} - {song.ArtistName} at {formattedPosition}";

                    break;
                case PlayType.Seeked:
                    MessageContent = $"{CurrentUserOnline.Username} Skipped to {formattedPosition}s when playing {song.Title} - {song.ArtistName} ";

                    break;
                case PlayType.Skipped:
                    MessageContent = $"{CurrentUserOnline.Username} Skipped {song.Title} - {song.ArtistName} Entirely";

                    break;
                case PlayType.Restarted:
                    MessageContent = $"{CurrentUserOnline.Username} Restarted {song.Title} - {song.ArtistName}";

                    break;
                case PlayType.SeekRestarted:
                    MessageContent = $"{CurrentUserOnline.Username} Restarted back {song.Title} - {song.ArtistName}";

                    break;
                case PlayType.CustomRepeat:
                    break;
                case PlayType.Previous:
                    break;
                case PlayType.LogEvent:
                    break;
                default:
                    break;
            }

            GeneralStaticUtilities.RunFireAndForget(
                ViewModel.Value.SendMessageAsync(MessageContent, playType, CurrentlyPlayingSong), 
                e =>
                {
                    Debug.WriteLine(e.Message);
                });

        }
    }
    #endregion
}

public enum PlaybackSource
{
    MainQueue,
    AlbumsQueue,
    SearchQueue,
    External,
    PlaylistQueue,
    ArtistQueue,

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
    Previous=9,
    LogEvent=10,
    ChatSent=11,
    ChatReceived = 12,
    ChatDeleted = 13,
    ChatEdited = 14,
    ChatPinned = 15,
    ChatUnpinned = 16,
    ChatLiked = 17,
    ChatUnliked = 18,
    ChatShared = 19,
    
    ChatUnread = 20,
    ChatRead = 21,
    ChatMentioned = 22,
    ChatUnmentioned = 23,
    ChatReplied = 24,
    ChatUnreplied = 25,
    ChatForwarded = 26,
    ChatUnforwarded = 27,
    ChatSaved = 28,
    ChatUnsaved = 29,
    ChatReported = 30,
    ChatUnreported = 31,
    ChatBlocked = 32,
    ChatUnblocked = 33,
    ChatMuted = 34,
    ChatUnmuted = 35,
    ChatPinnedMessage = 36,

}
public enum RepeatMode // Using enum for repeat modes
{
    Off = 0,
    All = 1,
    One = 2,
    Custom = 3, // If you re-implement Custom Repeat
}
