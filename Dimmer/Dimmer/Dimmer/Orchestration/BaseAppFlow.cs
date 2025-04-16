using Dimmer.Data;
using Dimmer.Utilities.Events;
using DimmerPlaybackState = Dimmer.Utilities.Enums.DimmerPlaybackState;

namespace Dimmer.Orchestration;
public class BaseAppFlow : IDisposable
{


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



    private Realm? Db;
    
    private readonly IRealmFactory RealmFactory;
    private readonly IMapper Mapper;

    #endregion

    #region public properties
    public SongModelView CurrentlyPlayingSong { get; set; } 
    public SongModel CurrentlyPlayingSongDB { get; set; } =  new();
    public SongModelView? PreviouslyPlayingSong { get; }
    public SongModelView? NextPlayingSong { get; }
    public bool IsShuffleOn { get; set; }
    public RepeatMode CurrentRepeatMode { get; set; } = RepeatMode.All;
    public int CurrentRepeatCount { get; set; }
    #endregion

    public static AppStateModel AppSettings{ get; set; } = new ();
    public BaseAppFlow(IRealmFactory _realmFactory, IMapper mapper)
    {
        RealmFactory = _realmFactory;
        Mapper= mapper;
        LoadRealm();
        Initialize();
        LoadAppData();
        CurrentlyPlayingSong = new();
    }

    public void LoadApplicationSettings()
    {
        var e= Db?.All<AppStateModel>().ToList();
        AppSettings = e?[0] ?? new AppStateModel();
    }

    private void LoadAppData()
    {
        List<SongModel>? dbb = Db?.All<SongModel>().OrderBy(x => x.DateCreated).AsEnumerable()
            .Select(x=>x.Freeze())
            .ToList();
        dbb ??= new List<SongModel>();
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
    
   public void SetCurrentSong(SongModelView song)
   {
        SongModel songDb = Mapper.Map<SongModel>(song);
        CurrentlyPlayingSongDB = songDb;
        CurrentSong.OnNext(songDb);
   }
    public void PlaySong()
    {
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


    public void UpdateSongPlaybackState(SongModelView? currentlyPlayingSong, PlayType playType, bool IsAdd, double? position = null)
    {
        currentlyPlayingSong??=CurrentlyPlayingSong;
        var songDb = Mapper.Map<SongModel>(currentlyPlayingSong);
        PlayDateAndCompletionStateSongLink link = new ()
        {
            DatePlayed = DateTime.Now,
            PlayType= (int)playType,
            SongId = songDb.LocalDeviceId,
            PositionInSeconds = position is null ? 0 : (double)position,
            WasPlayCompleted = playType == PlayType.Completed,

        };
        UpSertPDaCStateLink(link,IsAdd);

    }
    public void UpSertPDaCStateLink(PlayDateAndCompletionStateSongLink model, bool IsAdd)
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
    
    public void UpSertArtist(ArtistModel model, bool IsAdd)
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

    public void UpSertAlbumData(AlbumModel albumModel, bool IsAdd)
    {
        try
        {
            Db = RealmFactory.GetRealmInstance();
            albumModel.LocalDeviceId ??= DbUtils.GenerateLocalDeviceID("AL");
            DbUtils.AddOrUpdateSingleRealmItem(Db, albumModel, IsAdd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


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

