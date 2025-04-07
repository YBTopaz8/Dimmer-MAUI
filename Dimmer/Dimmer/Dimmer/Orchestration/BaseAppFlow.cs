using Dimmer.Data;
using Dimmer.Interfaces;
using Dimmer.Utilities;
using Dimmer.Utilities.Events;

namespace Dimmer.Orchestration;
public class BaseAppFlow : IDisposable
{


    #region FolderWatcher Region
    
    private bool _isDisposed = false; // To prevent multiple disposals




    #endregion

    #region static behavior subjects 
    public static BehaviorSubject<SongModel> CurrentSong { get; } = new(new SongModel());
    public static BehaviorSubject<List<SongModel>> AllSongs { get; } = new([]);
    public static BehaviorSubject<List<AlbumArtistGenreSongLink>> AllLinks { get; } = new([]);
    public static BehaviorSubject<List<GenreModel>> AllGenre { get; } = new([]);
    public static BehaviorSubject<List<ArtistModel>> AllArtists { get; } = new([]);
    public static BehaviorSubject<List<AlbumModel>> AllAlbums { get; } = new([]);


    #endregion

    #region private fields

    BehaviorSubject<bool> IsPlayingSubj = new(false);
    System.Timers.Timer? PositionTimer;

    private Realm Db;
    
    private readonly IDimmerAudioService AudioService;
    private readonly IRealmFactory RealmFactory;
    private readonly IMapper Mapper;
    
    #endregion

    #region public properties
    public SongModelView CurrentlyPlayingSong { get; set; }
    public SongModelView PreviouslyPlayingSong { get; }
    public SongModelView NextPlayingSong { get; }
    public bool IsShuffleOn { get; set; }

    public IObservable<bool> IsPlaying => IsPlayingSubj.AsObservable();

    public RepeatMode CurrentRepeatMode { get; set; }
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
        this.AudioService.PlayEnded += AudioService_PlayEnded;
        this.AudioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

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
    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        //currentPositionInSec = e/1000;
    }

    public void StartPositionTimer()
    {

    }

    private void AudioService_PlayingChanged(object? sender, PlaybackEventArgs e)
    {
        IsPlayingSubj.OnNext(e.IsPlaying);
        
        if (IsPlayingSubj.Value)
        {
            PositionTimer?.Start();
            
        }
        else
        {
            PositionTimer?.Stop();            
        }
    }
    public void PlaySong()
    {
        CurrentlyPlayingSong.IsCurrentPlayingHighlight = true;

        UpdateSongPlaybackState(CurrentlyPlayingSong, PlayType.Play);
    }

    public void StopPositionTimer()
    {
        PositionTimer?.Stop();
    }
    private void AudioService_PlayEnded(object? sender, PlaybackEventArgs e)
    {
        StopPositionTimer();

        UpdateSongPlaybackState(e.MediaSong, PlayType.Completed);  
    }


    public void UpdateSongPlaybackState(SongModelView? CurrentlyPlayingSong, PlayType playType, double? position = null)
    {
        var songDb = Mapper.Map<SongModel>(CurrentlyPlayingSong);
        PlayDateAndCompletionStateSongLink link = new ()
        {
            DatePlayed = DateTime.Now,
            PlayType = (int)playType,
            Song = songDb,
            SongId = songDb.LocalDeviceId,
            PositionInSeconds = position is null ? 0 : (double)position,
            WasPlayCompleted = playType == PlayType.Completed,

        };
        AddPDaCStateLink(link);

    }
    public void AddPDaCStateLink(PlayDateAndCompletionStateSongLink model)
    {
        try
        {
            model.LocalDeviceId ??= DbUtils.GenerateLocalDeviceID("PDL");
            DbUtils.AddOrUpdateSingleRealmItem(Db, model, link => link.LocalDeviceId == model.LocalDeviceId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

        }

    }
    private void AudioService_PlayNext(object? sender, EventArgs e)
    {
        
    }
    
    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        
    }
    #endregion

    private void LoadAppData()
    {
        var dbb = Db.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
        AllSongs.OnNext(dbb);        
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


    public void Initialize()
    {
        Debug.WriteLine(Db.GetType());
    }
    private bool _disposed = false;

    // Other members...

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources.
                PositionTimer?.Dispose();
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

