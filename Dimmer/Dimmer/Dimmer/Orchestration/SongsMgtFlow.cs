using ATL;
using Dimmer.Data;
using Dimmer.Utilities.Events;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : BaseAppFlow
{
    private readonly IRealmFactory _realmFactory;
    private readonly IMapper Mapper; 
    readonly BehaviorSubject<bool> IsPlayingSubj = new(false);
    readonly BehaviorSubject<double> CurrentPosSubj = new(0);
    readonly BehaviorSubject<double> CurrentVolSubj = new(0);
    readonly BehaviorSubject<DimmerPlaybackState> CurrentStateSubj = new(DimmerPlaybackState.Stopped);

    public IObservable<bool> IsPlaying => IsPlayingSubj.AsObservable();
    public IObservable<double> CurrentSongPosition => CurrentPosSubj.AsObservable();
    public IObservable<double> CurrentSongVolume => CurrentVolSubj.AsObservable();
    public IObservable<DimmerPlaybackState> CurrentAppState => CurrentStateSubj.AsObservable();
    public IDimmerAudioService AudioService { get; }

    public SongsMgtFlow(IRealmFactory realmFactory, IDimmerAudioService dimmerAudioService, IMapper mapper) : base(realmFactory, dimmerAudioService, mapper)
    {
        _realmFactory = realmFactory;
        AudioService=dimmerAudioService;
        this.AudioService.PlayPrevious += AudioService_PlayPrevious;
        this.AudioService.PlayNext += AudioService_PlayNext;
        this.AudioService.IsPlayingChanged += AudioService_PlayingChanged;
        this.AudioService.PositionChanged +=AudioService_PositionChanged;
        this.AudioService.PlayEnded += AudioService_PlayEnded;
        Mapper=mapper;
        _realmFactory.GetRealmInstance();
        SubscribeToAppCurrentState();
    }

    private void SubscribeToAppCurrentState()
    {
        CurrentAppState.DistinctUntilChanged()
            .Subscribe(async state =>
            {
                switch (state)
                {
                    case DimmerPlaybackState.Playing:

                        break;
                    case DimmerPlaybackState.Paused:

                        break;
                    case DimmerPlaybackState.Ended:
                        await PlayNextSong();
                        break;
                    case DimmerPlaybackState.PlayNext:
                        await PlayNextSong();
                        break;
                    case DimmerPlaybackState.RepeatSame:
                        await PlaySongInAudioService();
                        break;
                    default:
                        break;
                }
            });
    }

    #region Playback Control Region

    public async Task<bool> PlaySelectedSongsOutsideApp(List<string> filePaths)
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

        await ReplaceAndPlayQueue(allSongs[0], allSongs, PlaybackSource.External);
        
        return true;
    }


    public async Task<bool> PlaySelectedSongsOutsideAppDebounced(List<string> filePaths)
    {
       
        try
        {
            return await PlaySelectedSongsOutsideApp(filePaths);
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    #region Audio Service Events Region

    private void AudioService_PositionChanged(object? sender, double e)
    {
        CurrentPosSubj.OnNext(AudioService.CurrentPosition);
    }
    private void AudioService_PlayingChanged(object? sender, PlaybackEventArgs e)
    {
        IsPlayingSubj.OnNext(e.IsPlaying);

    }
    private void AudioService_PlayNext(object? sender, EventArgs e)
    {
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        IsPlayedCompletely = false;
        CurrentPosSubj.OnNext(0);
        CurrentStateSubj.OnNext(DimmerPlaybackState.PlayNext);
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Skipped, true);
    }

    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        IsPlayedCompletely = false;
        CurrentPosSubj.OnNext(0);
        CurrentStateSubj.OnNext(DimmerPlaybackState.PlayPrevious);
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Skipped, true);
    }

    private void AudioService_PlayEnded(object? sender, PlaybackEventArgs e)
    {
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = false;

        if (e.EventType == DimmerPlaybackState.Ended)
        {
            CurrentStateSubj.OnNext(DimmerPlaybackState.Ended);
            CurrentPosSubj.OnNext(0);
            IsPlayingSubj.OnNext(false);
            IsPlayedCompletely = true;

            UpdateSongPlaybackState(e.MediaSong, PlayType.Completed, true);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.Off:
                break;
            case RepeatMode.All:
                CurrentStateSubj.OnNext(DimmerPlaybackState.PlayNext);

                break;
            case RepeatMode.One:

                CurrentStateSubj.OnNext(DimmerPlaybackState.RepeatSame);
                break;
            case RepeatMode.Custom:
                break;
            default:
                break;
        }
    }
    #endregion


    public async Task<bool> PlaySongInAudioService()
    {
        CurrentPosSubj.OnNext(0);
        base.PlaySong();
        byte[]? coverImage = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        CurrentlyPlayingSong.ImageBytes = coverImage;
        await AudioService.InitializeAsync(CurrentlyPlayingSong);
        await AudioService.PlayAsync();
        
        
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Play, true);

        CurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        CurrentlyPlayingSong.IsPlaying = true; // Update playing status
        
        return true;
    }


    public async Task<bool> PauseResumeSongAsync(double currentPosition, bool isPause = false)
    {

        if (isPause)
        {
            await AudioService.PauseAsync();
            base.PauseSong();
        }
        else
        {
            
            await AudioService.SeekAsync(currentPosition);
            await AudioService.PlayAsync();
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

    public async Task SeekTo(double positionInSec)
    {        
        if (CurrentlyPlayingSong is null)
        {
            return;
        }
        double CurrentPercentage = CurrentPositionInSec / CurrentlyPlayingSong.DurationInSeconds * 100;

        if (AudioService.IsPlaying)
        {
            await AudioService.SeekAsync(positionInSec);

            PlayDateAndCompletionStateSongLink linkss = new()
            {
                DatePlayed = DateTime.Now,
                PlayType= (int)PlayType.Seeked,
                SongId = CurrentlyPlayingSong.LocalDeviceId,
                PositionInSeconds = CurrentPositionInSec
            };
            if (CurrentPercentage >= 80)
            {
                linkss.PlayType= (int)PlayType.SeekRestarted;
            }

            UpSertPDaCStateLink(linkss, true);
        }
    }
    


    Random _random { get; } = new Random();

    #region Audio Service Events Region

   

    #endregion
    
    public async Task PlayNextSong(bool isUserInitiated = false)
    {
        

        if (isUserInitiated)
        {
            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Skipped, true);
        }

        switch (CurrentRepeatMode)
        {
            case RepeatMode.One: // Repeat One
                await PlaySongInAudioService();
                return;
            case RepeatMode.Custom: // Custom Repeat
                if (CurrentRepeatCount < repeatCountMax) // still on repeat one for same CurrentlyPlayingSong (later can be same PL/album etc)
                {
                    CurrentRepeatCount++;
                    UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.CustomRepeat, true);
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
            await PlaySongInAudioService();

        }
        else
        {
            if (CurrentRepeatMode == RepeatMode.All)
            {
                CurrentlyPlayingSongDB = AllSongs.Value[0];
                CurrentIndexInMasterList = 0;
                await PlaySongInAudioService();
            }
        }
        // If not repeat all and at the end, do nothing or stop playback
    }

    int prevCounter;
    readonly int repeatCountMax =0;

    public void PlayPreviousSong(bool isUserInitiated = true)
    {
        if (CurrentlyPlayingSong == null)
            return;

        if (prevCounter == 1)
        {
            UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Previous, true);

            prevCounter = 0;
            return;
        }
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Restarted, true);
        
        if (CurrentRepeatMode == RepeatMode.One)
        {
            return;
        }
        prevCounter++;
    }

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

    double _currentPositionInSec => AudioService.CurrentPosition;

    public double CurrentPositionInSec
    {
        get => _currentPositionInSec;
    }

    public int CurrentIndexInMasterList { get; private set; }
    #endregion




    public async Task ReplaceAndPlayQueue(SongModelView currentlyPlayingSong, List<SongModelView> songs, PlaybackSource source)
    {
        CurrentlyPlayingSong = currentlyPlayingSong;
        base.PlaySong();
        await PlaySongInAudioService();

        

        
    }


    #region Helpers
    

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
