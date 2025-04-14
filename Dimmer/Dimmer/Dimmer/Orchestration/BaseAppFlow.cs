using Dimmer.Data;
using Dimmer.Utilities.Events;
using DimmerPlaybackState = Dimmer.Utilities.Enums.DimmerPlaybackState;

namespace Dimmer.Orchestration;
public class BaseAppFlow : IDisposable
{


    #region FolderWatcher Region






    #endregion


    public IObservable<bool> IsPlaying => IsPlayingSubj.AsObservable();
    public IObservable<double> CurrentSongPosition => CurrentPosSubj.AsObservable();
    public IObservable<double> CurrentSongVolume => CurrentVolSubj.AsObservable();
    public IObservable<DimmerPlaybackState> CurrentAppState => CurrentStateSubj.AsObservable();


    #region static behavior subjects 
    public static BehaviorSubject<SongModel> CurrentSong { get; } = new(new SongModel());
    public static BehaviorSubject<List<SongModel>> AllSongs { get; } = new([]);
    public static BehaviorSubject<List<AlbumArtistGenreSongLink>> AllLinks { get; } = new([]);
    public static BehaviorSubject<List<GenreModel>> AllGenre { get; } = new([]);
    public static BehaviorSubject<List<ArtistModel>> AllArtists { get; } = new([]);
    public static BehaviorSubject<List<AlbumModel>> AllAlbums { get; } = new([]);
    public static BehaviorSubject<List<PlaylistModel>> AllPlaylists { get; } = new([]);

    #endregion

    #region private fields

    readonly BehaviorSubject<bool> IsPlayingSubj = new(false);
    readonly BehaviorSubject<double> CurrentPosSubj = new(0);
    readonly BehaviorSubject<double> CurrentVolSubj = new(0);
    readonly BehaviorSubject<DimmerPlaybackState> CurrentStateSubj = new(DimmerPlaybackState.Stopped);
    

    private Realm? Db;
    
    private readonly IDimmerAudioService AudioService;
    private readonly IRealmFactory RealmFactory;
    private readonly IMapper Mapper;

    #endregion

    #region public properties
    public SongModelView CurrentlyPlayingSong { get; set; } 
    public SongModel CurrentlyPlayingSongDB { get; set; } = new();
    public SongModelView? PreviouslyPlayingSong { get; }
    public SongModelView? NextPlayingSong { get; }
    public bool IsShuffleOn { get; set; }
    

    public RepeatMode CurrentRepeatMode { get; set; } = RepeatMode.All;
    public int CurrentRepeatCount { get; set; }
    #endregion

    public BaseAppFlow(IRealmFactory _realmFactory, IDimmerAudioService dimmerAudioService, IMapper mapper)
    {
        RealmFactory = _realmFactory;
        Mapper= mapper;
        LoadRealm();
        Initialize();
        LoadAppData();        
        this.AudioService = dimmerAudioService;
        
        this.AudioService.PlayPrevious += AudioService_PlayPrevious;
        this.AudioService.PlayNext += AudioService_PlayNext;
        this.AudioService.IsPlayingChanged += AudioService_PlayingChanged;
        this.AudioService.PositionChanged +=AudioService_PositionChanged;
        this.AudioService.PlayEnded += AudioService_PlayEnded;
        CurrentlyPlayingSong = new();
    }

    private void AudioService_PositionChanged(object? sender, double e)
    {
        CurrentPosSubj.OnNext(AudioService.CurrentPosition);
    }

    private void LoadAppData()
    {
        List<SongModel>? dbb = Db?.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
        if (dbb == null)
        {
            dbb = new List<SongModel>();
        }
        AllSongs.OnNext(dbb);
    }

    private void LoadRealm()
    {
        Db = RealmFactory.GetRealmInstance();
        var AppSettingss = Db.All<AppStateModel>().ToList();
        if (AppSettingss.Count == 0)
        {
            AppStateModel appStateModel = new ();
            Db.Write(() =>
            {
                Db.Add(appStateModel);
            });
        }
        else
        {
            AppStateModel appStateModel = AppSettingss[0];

            List<string>? ListOfFolders = [.. appStateModel.UserMusicFoldersPreference];
            
            SetupFolderMonitoring(ListOfFolders);
        }
    }

    static void SetupFolderMonitoring(List<string>? ListOfFolders)
    {
        if (ListOfFolders == null || ListOfFolders.Count == 0)
        {
            return;
        }
        foreach (var path in ListOfFolders)
        {
            SingleFolderMonitor? newSingleFolderMonitor = new SingleFolderMonitor(path);
            newSingleFolderMonitor.StartMonitoring();            
        }
    }
    #region Audio Service Event Handlers
   public void SetCurrentSong(SongModelView song)
   {
        SongModel songs = Mapper.Map<SongModel>(song);
        CurrentSong.OnNext(songs);
   }
    public void PlaySong()
    {
        CurrentPosSubj.OnNext(0);
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Play, true);        
        IsPlayedCompletely = false;
    }
    public void PauseSong()
    {
        IsPlayedCompletely = false;

        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Pause, true);
    }
    public void ResumeSong()
    {
        IsPlayedCompletely = false;
        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Resume, true);
        
    }
    public bool IsPlayedCompletely { get; set; }

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
    public void UpdateSongPlaybackState(SongModelView? currentlyPlayingSong, PlayType playType, bool IsAdd, double? position = null)
    {
        currentlyPlayingSong??=CurrentlyPlayingSong;
        var songDb = Mapper.Map<SongModel>(currentlyPlayingSong);
        PlayDateAndCompletionStateSongLink link = new ()
        {
            DatePlayed = DateTime.Now,
            PlayType= (int)playType,
            Song = songDb,
            SongId = songDb.LocalDeviceId,
            PositionInSeconds = position is null ? 0 : (double)position,
            WasPlayCompleted = playType == PlayType.Completed,

        };
        AddPDaCStateLink(link,IsAdd);

    }
    public void AddPDaCStateLink(PlayDateAndCompletionStateSongLink model, bool IsAdd)
    {
        try
        {
            Db = RealmFactory.GetRealmInstance();
            model.LocalDeviceId ??= DbUtils.GenerateLocalDeviceID("PDL");
            DbUtils.AddOrUpdateSingleRealmItem(Db, model, IsAdd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

        }

    }
    public void UpSertPlaylistData(PlaylistModel model, bool IsAdd)
    {
        try
        {
            Db = RealmFactory.GetRealmInstance();
            model.LocalDeviceId ??= DbUtils.GenerateLocalDeviceID("PL");
            DbUtils.AddOrUpdateSingleRealmItem(Db, model, IsAdd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

        }

    }
    
    #endregion


  

    /// <summary>
    /// Toggles repeat mode between 0, 1, and 2
    ///  0 for repeat OFF
    ///  1 for repeat ALL
    ///  2 for repeat ONE
    /// </summary>
    public RepeatMode ToggleRepeatMode()
    {
        CurrentRepeatMode = (RepeatMode)(((int)CurrentRepeatMode + 1) % 3); // Cycle through enum values 0, 1, 2
        
        AppSettingsService.RepeatModePreference.ToggleRepeatState((int)CurrentRepeatMode); // Store as int
        return CurrentRepeatMode;
    }
    
    public static void ToggleStickToTop(bool isSticktoTop)
    {
        AppSettingsService.IsSticktoTopPreference.ToggleIsSticktoTopState(isSticktoTop);        
    }


    public static void Initialize()
    {
        Debug.WriteLine("Db.GetType()");
    }
    private bool _disposed;

    // Other members...

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources.
                
                IsPlayingSubj?.Dispose();
            }

            // Dispose unmanaged resources.
            // Set large fields to null.

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseAppFlow()
    {
        Dispose(false);
    }
}

