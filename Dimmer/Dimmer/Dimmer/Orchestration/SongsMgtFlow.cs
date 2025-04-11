using ATL;
using Dimmer.Data;
using Dimmer.Interfaces;
using Dimmer.Utilities;
using System.Threading.Tasks;
using System.Timers;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : BaseAppFlow
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper Mapper;
    
    public SongsMgtFlow(IRealmFactory realmFactory, IDimmerAudioService dimmerAudioService, IMapper mapper) : base(realmFactory, dimmerAudioService, mapper)
    {
        _realmFactory = realmFactory;
        AudioService=dimmerAudioService;
        Mapper=mapper;
        _realmFactory.GetRealmInstance();
        SubscribeToAppCurrentState();
    }

    private void SubscribeToAppCurrentState()
    {
        CurrentAppState.DistinctUntilChanged()
            .Subscribe(state =>
            {
                switch (state)
                {
                    case DimmerPlaybackState.Playing:

                        break;
                    case DimmerPlaybackState.Paused:

                        break;
                    case DimmerPlaybackState.Ended:
                        PlayNextSong();
                        break;
                    default:
                        break;
                }
            });
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
   
    public async Task<bool> PlaySongWithPosition(double positionInSec)
    {
        CurrentPositionInSec = 0;
        
        byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        await AudioService.InitializeAsync(CurrentlyPlayingSong);

        if (positionInSec > 0)
        {
            await AudioService.SeekAsync(positionInSec);
            CurrentPositionInSec = positionInSec;
        }
        else
        {
            await AudioService.PlayAsync();
        }

        
        
        CurrentlyPlayingSong.IsPlaying = true; // Update playing status
        
        return true;
    }

    public async Task<bool> PlaySongInAudioService()
    {
        base.PlaySong();
        byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        CurrentlyPlayingSong.ImageBytes = coverImage;
        await AudioService.InitializeAsync(CurrentlyPlayingSong);
        await AudioService.PlayAsync();
        
        
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Play);

        CurrentlyPlayingSong.IsCurrentPlayingHighlight = true;
        CurrentlyPlayingSong.IsPlaying = true; // Update playing status
        
        return true;
    }


    public async Task<bool> PauseResumeSong(double currentPosition, bool isPause = false)
    {

        if (isPause)
        {
            await AudioService.PauseAsync();
            base.PauseSong();
        }
        else
        {
            byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
             await AudioService.InitializeAsync(CurrentlyPlayingSong);
            await AudioService.SeekAsync(currentPosition);
            base.ResumeSong();
        }
        return true;
    }
    public async Task<bool> StopSong()
    {
        await AudioService.PauseAsync();
        base. CurrentlyPlayingSong.IsPlaying = false;
        return true;
    }

    public void SeekTo(double positionInSec)
    {
        CurrentPositionInSec = positionInSec;
        if (CurrentlyPlayingSong is null)
        {
            return;
        }
        double CurrentPercentage = CurrentPositionInSec / CurrentlyPlayingSong.DurationInSeconds * 100;

        if (AudioService.IsPlaying)
        {
            AudioService.SeekAsync(positionInSec);

            PlayDateAndCompletionStateSongLink linkss = new()
            {
                DatePlayed = DateTime.Now,
                PlayType = 4,
                SongId = CurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = CurrentPositionInSec
            };
            if (CurrentPercentage >= 80)
            {
                linkss.PlayType = 7;
            }

            AddPDaCStateLink(linkss);
        }
    }


    Random _random { get; } = new Random();

    #region Audio Service Events Region

   

    #endregion

    int CurrentIndexInMasterList = 0;
    private int repeatCountMax;

    public void PlayNextSong(bool isUserInitiated = false)
    {
        

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Skipped);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.One: // Repeat One
                PlaySongInAudioService();
                return;
            case RepeatMode.Custom: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax) // still on repeat one for same CurrentlyPlayingSong (later can be same PL/album etc)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.CustomRepeat);
                    PlaySong();
                }
                return;
            default:
                

                break;

        }

        var currentindex = AllSongs.Value.IndexOf(CurrentlyPlayingSongDB);
        if (currentindex == -1)
        {
            return; // Song not found in the list
        }
        if (currentindex + 1 < AllSongs.Value.Count)
        {
            CurrentlyPlayingSongDB = AllSongs.Value[currentindex + 1];
            CurrentlyPlayingSong = Mapper.Map<SongModelView>(CurrentlyPlayingSongDB);
            CurrentIndexInMasterList = currentindex + 1;
            PlaySongInAudioService();

        }
        else
        {
            if (CurrentRepeatMode == RepeatMode.All)
            {
                CurrentlyPlayingSongDB = AllSongs.Value[0];
                CurrentIndexInMasterList = 0;
                PlaySongInAudioService();
            }
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

    public void ChangeVolume(double newVolumeOver1)
    {
        try
        {
            if (CurrentlyPlayingSong is null)
            {
                return;
            }
            AudioService.Volume = Math.Clamp(newVolumeOver1, 0, 1);

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
    public void SetToggleRepeatMode()
    {
        base.ToggleRepeatMode();        
    }


    public double CurrentPositionInSec
    {
        get
        {
            if (AudioService.IsPlaying)
            {
                return AudioService.CurrentPosition;
            }
            else
            {
                return 0;
            }
        }
        set; } = 0;
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


 
    public void ClearQueue()
    {
        
    }
    

    #region Helpers
    

    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (AudioService.IsPlaying)
        {
            double totalDurationInSeconds = CurrentlyPlayingSong.DurationInSeconds;
            double percentagePlayed = CurrentPositionInSec / totalDurationInSeconds;
            CurrentPositionInSec = AudioService.CurrentPosition;
            

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
