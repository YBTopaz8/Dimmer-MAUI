using ATL;
using Dimmer.Data;
using Dimmer.Interfaces;
using Dimmer.Utilities;
using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Ude.Core;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : BaseAppFlow
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper Mapper;
    private Realm db;
    public SongsMgtFlow(IRealmFactory realmFactory, IDimmerAudioService dimmerAudioService, IMapper mapper) : base(realmFactory, dimmerAudioService, mapper)
    {
        _realmFactory = realmFactory;
        AudioService=dimmerAudioService;
        Mapper=mapper;
        _realmFactory.GetRealmInstance();

    }
    public IDimmerAudioService AudioService { get; }

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
        Dictionary<string, SongModelView> existingSongDictionary = new();
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
                    Genre = string.IsNullOrEmpty(track.Genre) ? "Unknown Genre" : track.Genre,
                    ArtistName = string.IsNullOrEmpty(track.Artist) ? "Unknown Artist" : track.Artist,
                    AlbumName = string.IsNullOrEmpty(track.Album) ? "Unknown Album" : track.Album,

                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,


                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc")),
                    //CoverImagePath = PlayBackStaticUtils.SaveOrGetCoverImageToFilePath(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData),
                };
                if (track.Year is not null)
                {
                    newSong.ReleaseYear = (int)track.Year;
                }
                if (track.TrackNumber is not null)
                {
                    newSong.ReleaseYear = (int)track.TrackNumber;
                }
                
                allSongs.Add(newSong);
            }
        }

        ReplaceAndPlayQueue(allSongs[0], allSongs, PlaybackSource.External);
        
        return true;
    }

    private void AfterCallWork()
    {

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



    private Random random = Random.Shared;  // Reuse the same Random instance
    private ObservableCollection<SongModelView> _internalNowPlayingQueue { get; set; }
    private void UpdateActiveQueue(ObservableCollection<SongModelView> songs)
    {
        _internalNowPlayingQueue  = songs;
        if (IsShuffleOn)
        {
           songs.OrderBy(_ => random.Next()).ToObservableCollection(); // Simple shuffle

        }
    }
    public bool PlaySongWithPosition(double positionInSec)
    {
        currentPositionInSec = 0;
        
        byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        AudioService.Initialize(CurrentlyPlayingSong, coverImage);

        if (positionInSec > 0)
        {
            AudioService.Resume(positionInSec);
            currentPositionInSec = positionInSec;
        }
        else
        {
            AudioService.Play();
        }

        base.StartPositionTimer();
        
        CurrentlyPlayingSong.IsPlaying = true; // Update playing status
        
        return true;
    }

    public bool PlaySongInAudioService()
    {
        byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        AudioService.Initialize(CurrentlyPlayingSong, coverImage);
        AudioService.Play();
        
        StartPositionTimer();
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Play);

        
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = true;
        CurrentlyPlayingSong.IsPlaying = true; // Update playing status
        
        return true;
    }


    public bool PauseResumeSong(double currentPosition, bool isPause = false)
    {

        if (AudioService.IsPlaying)
        {
            AudioService.Pause();
            
            StopPositionTimer();
            
        }
        else
        {
            byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
            AudioService.Initialize(CurrentlyPlayingSong, coverImage);
            AudioService.Resume(currentPosition);
            StartPositionTimer();
            
            
        }
        return true;
    }
    public bool StopSong()
    {
        AudioService.Pause();
        StopPositionTimer();
        
        base. CurrentlyPlayingSong.IsPlaying = false;
        

        
        return true;
    }
    Random _random { get; } = new Random();

    #region Audio Service Events Region

    private int repeatCountMax;



    #endregion

    int CurrentIndexInMasterList = 0;
    public void PlayNextSong(bool isUserInitiated = true)
    {
        if (CurrentRepeatMode == RepeatMode.One)
        {
            PlaySongInAudioService();
            
            return;
        }

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Skipped);
        }

        // If not repeat all and at the end, do nothing or stop playback
    }

    int prevCounter = 0;
    public void PlayPreviousSong(bool isUserInitiated = true)
    {
        if (CurrentlyPlayingSong == null)
            return;

        if (prevCounter == 1)
        {


            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Previous);

            prevCounter = 0;
            return;
        }
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Restarted);
        
        if (CurrentRepeatMode == RepeatMode.One)
        {
            return;
        }
        prevCounter++;
    }
    double CurrentPercentage = 0;
    /// <summary>
    /// Seeks to a specific position in the currently SELECTED CurrentlyPlayingSong to play but won't play it if it's paused
    /// </summary>
    /// <param name="positionInSec"></param>
    /// <returns></returns>
    public void SeekTo(double positionInSec)
    {
        currentPositionInSec = positionInSec;
        if (CurrentlyPlayingSong is null)
        {
            return;
        }
        double CurrentPercentage = currentPositionInSec / CurrentlyPlayingSong.DurationInSeconds * 100;

#if ANDROID
        AudioService.SetCurrentTime(positionInSec);

        PlayDateAndCompletionStateSongLink links = new()
        {
            DatePlayed = DateTime.Now,
            PlayType = 4,
            SongId = CurrentlyPlayingSong.LocalDeviceId,
            PositionInSeconds = currentPositionInSec
        };
        if (CurrentPercentage >= 80)
        {
            links.PlayType = 7;
        }

        AddPDaCStateLink(links);
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Seeked, currentPositionInSec);
        return;
#endif
        if (AudioService.IsPlaying)
        {
            AudioService.SetCurrentTime(positionInSec);

            PlayDateAndCompletionStateSongLink linkss = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = (int) PlayType.Seeked,
                SongId = CurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = currentPositionInSec
            };
            if (CurrentPercentage >= 80)
            {
                linkss.PlayType =(int)PlayType.SeekRestarted;
            }

            AddPDaCStateLink(linkss);
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
            AudioService.Volume = newVolumeOver1;

            AppSettingsService.VolumeSettingsPreference.SetVolumeLevel(newVolumeOver1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        AudioService.Volume -= 0.01;
    }
    public double VolumeLevel => AudioService.Volume;
    public void IncreaseVolume()
    {
        AudioService.Volume += 0.01;
    }

    /// <summary>
    /// Toggles shuffle mode on or off
    /// </summary>
    /// <param name="isShuffleOn"></param>
    public void ToggleShuffle(bool isShuffleOn)
    {
        IsShuffleOn = isShuffleOn;
        AppSettingsService.ShuffleStatePreference.ToggleShuffleState(isShuffleOn);
        
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




    public void ReplaceAndPlayQueue(SongModelView currentlyPlayingSong, List<SongModelView> songs, PlaybackSource source)
    {
        CurrentlyPlayingSong = currentlyPlayingSong;
        base.PlaySong();
        PlaySongInAudioService();

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

        
    }

    #region Audio Service Event Handlers
    private void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        StopPositionTimer();
        

        if (CurrentlyPlayingSong != null)
        {
            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Completed);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.One: // Repeat One
                base.PlaySong();
                break;
            case RepeatMode.Custom: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax) // still on repeat one for same CurrentlyPlayingSong (later can be same PL/album etc)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.CustomRepeat);
                    base.PlaySong();
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


    private void AudioService_PlayNext(object? sender, EventArgs e)
    {
        PlayNextSong();
    }

    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        PlayPreviousSong();
    }
    #endregion


 
    public void ClearQueue()
    {
        
    }
    

    #region Helpers
    

    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (AudioService.IsPlaying)
        {
            double totalDurationInSeconds = CurrentlyPlayingSong.DurationInSeconds;
            double percentagePlayed = currentPositionInSec / totalDurationInSeconds;
            currentPositionInSec = AudioService.CurrentPosition;
            

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
    PlayCompleted,

}