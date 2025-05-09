using Dimmer.Utilities.FileProcessorUtils;
using Syncfusion.Maui.Toolkit.NavigationDrawer;
using System.Reactive.Concurrency;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{

    public static ParseUser? CurrentUserOnline { get; set; }
    public static UserModel? CurrentUser { get; set; }

    public readonly IDimmerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _aagslRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<UserModel> _userRepo;
    private readonly ISettingsService _settings;
    private readonly IFolderMgtService folderMgt;
    public readonly IMapper _mapper;
    private bool _disposed;

    public SongModelView CurrentlyPlayingSong { get; set; } = new();
    

    public bool  IsShuffleOn
        => _settings.ShuffleOn;

    public RepeatMode CurrentRepeatMode
        => _settings.RepeatMode;
    // enforce a single instance of the app flow
    public BaseAppFlow(
        IDimmerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<UserModel> userRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMgtService folderMgt, SubscriptionManager subs,
        IMapper mapper)
    {
        _state = state;
        _songRepo = songRepo;
        _pdlRepo = pdlRepo;
        _playlistRepo = playlistRepo;
        _artistRepo = artistRepo;
        _albumRepo = albumRepo;
        _genreRepo = genreRepo;
        _aagslRepo = aagslRepo;
        _settings = settings;
        this.folderMgt=folderMgt;
        _mapper = mapper;
        _userRepo = userRepo;

        Initialize();
        
    }
    public static IReadOnlyCollection<SongModel> MasterList { get; private set; }
    
    private void Initialize()
    {

        MasterList = [.. _songRepo
            .GetAll(true)];
        if (MasterList.Count <1)
        {
            AppUtils.IsUserFirstTimeOpening=true;
            return;
        }


        // 3) live updates, on UI‑thread if available
        var syncCtx = SynchronizationContext.Current;
        IScheduler scheduler = syncCtx != null
            ? new SynchronizationContextScheduler(syncCtx)
            : TaskPoolScheduler.Default;

        _state.SetSecondSelectdSong(MasterList.First());
        _state.SetCurrentSong(MasterList.First());
        _state.SetCurrentPlaylist([], null);
        SubscribeToStateChanges();

        folderMgt.RestartWatching();

        LoadUser();
         _songRepo.WatchAll().ObserveOn(scheduler)
      .DistinctUntilChanged(new SongListComparer())
      .Subscribe(list =>
      {
          if (list.Count == MasterList.Count)
                {
                    return;
                }
                MasterList = [.. list];
            });
        

    }

    private void LoadUser()
    {
        var user = _userRepo.GetAll().FirstOrDefault();
        if (user is null)
        {
            CurrentUser = null;
        }
        else
        {
            CurrentUser=user;
            if (!string.IsNullOrEmpty(CurrentUser.UserPassword))
            {
                Task.Run(async () =>
                {
                    CurrentUserOnline =  await ParseClient.Instance.LogInWithAsync(CurrentUser.UserName, CurrentUser.UserPassword);
                });
            }
        }
    }

    static List<string> listofPathsAddedInSession = new();
    private void SubscribeToStateChanges()
    {
        _state.CurrentPlayBackState.
            DistinctUntilChanged()
            .Subscribe(state =>
            {
                //IsPlaying = state.State == DimmerPlaybackState.Playing;
                switch (state.State)
                {
                   
                    case DimmerPlaybackState.FolderAdded:

                        string? folder = state.ExtraParameter as string;
                        if (folder is null)
                        {
                            return;
                        }
                        if (listofPathsAddedInSession.Contains(folder))
                        {
                            return;
                        }
                        listofPathsAddedInSession.Add(folder);
                        Task.Run(()=> LoadSongs([.. listofPathsAddedInSession.Distinct()]));
                        break;
                    case DimmerPlaybackState.FolderRemoved:
                        break;
                    case DimmerPlaybackState.FileChanged:
                        break;
                    case DimmerPlaybackState.FolderNameChanged:
                        break;
                    case DimmerPlaybackState.FolderScanCompleted:
                        break;
                    case DimmerPlaybackState.FolderScanStarted:
                        break;
                    case DimmerPlaybackState.FolderWatchStarted:
                        break;
                    default:
                        break;
                }
            });
    }


    public void SeekedTo(double? position)
    {
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Seeked, position);
    }

    public void PlaySong()
    {
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Play);
    }

    public void AddPauseSongEventToDB()
    {
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Pause);
    }

    public void AddResumeSongToDB()
    {
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Resume);
    }

    public void PlayEnded()
    {
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Completed);
    }

    public void UpdatePlaybackState(
        string? songId,
        PlayType type,
        double? position = null)
    {
        if(string.IsNullOrEmpty(songId))
        {
            songId = CurrentlyPlayingSong.LocalDeviceId;
        }


        var link = new PlayDateAndCompletionStateSongLink
        {
            LocalDeviceId = Guid.NewGuid().ToString(),
            SongId = songId,
            PlayType = (int)type,
            DatePlayed = DateTime.Now,
            PositionInSeconds = position ?? 0,
            WasPlayCompleted = type == PlayType.Completed
        };

        _pdlRepo.AddOrUpdate(link);
        AppLogModel log = new()
        {
            Log = $"Play type was {type} at {DateTime.Now.ToLocalTime()}",
            ViewSongModel = CurrentlyPlayingSong,
        };
        _state.SetCurrentLogMsg(log);
    }


    public void UpSertUser(UserModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _userRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert User {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }


    public void UpSertPlaylist(PlaylistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _playlistRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert Playlist {model} at {DateTime.Now.ToLocalTime()}",            
        };
        _state.SetCurrentLogMsg(log);
    }

    public void UpSertArtist(ArtistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _artistRepo.AddOrUpdate(model);

        AppLogModel log = new()
        {
            Log = $"UpSert Artist {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }

    public void UpSertAlbum(AlbumModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _albumRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert Album {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }
    
    public void UpSertSong(SongModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _songRepo.AddOrUpdate(model);

        AppLogModel log = new()
        {
            Log = $"UpSert Song {model} at {DateTime.Now.ToLocalTime()}",
            AppSongModel = model,
        };
        _state.SetCurrentLogMsg(log);
        
    }
    public void UpSertSongNote(SongModel model, UserNoteModel note)
    {
        // 1) Ensure the song has a primary key
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();

        // 2) Do everything in one Realm transaction
        _songRepo.BatchUpdate(realm =>
        {
            // 3) Fetch or add the song itself
            var song = realm.Find<SongModel>(model.LocalDeviceId)
                       ?? realm.Add(model, update: true);

          
                    // 5b) New note: give it an Id (if missing) and add it
                    if (string.IsNullOrEmpty(note.LocalDeviceId))
                        note.LocalDeviceId = Guid.NewGuid().ToString();

                    song.UserNotes.Add(note);
               
        });

        // 6) Log after the write completes
        var log = new AppLogModel
        {
            Log          = $"UpSertSongNote on {model.LocalDeviceId} at {DateTime.Now:O}",
            AppSongModel = model,
        };
        _state.SetCurrentLogMsg(log);
    }


    public void ToggleShuffle(bool isOn)
    {
        _settings.ShuffleOn = isOn;
    }

    public RepeatMode ToggleRepeatMode()
    {
        var next = (RepeatMode)(((int)_settings.RepeatMode + 1) % 3);
        _settings.RepeatMode = next;
        return next;
    }

    #region Settings Region

    List<AlbumModel>? realmAlbums { get; set; }
    List<SongModel>? realmSongs { get; set; }
    List<GenreModel>? realGenres { get; set; }
    List<ArtistModel>? realmArtists { get; set; }
    List<AlbumArtistGenreSongLink>? realmAAGSL { get; set; }
    void GetInitialValues()
    {
        MasterList = [.. _songRepo
            .GetAll()
            .OrderBy(x => x.DateCreated)];
        
        realmSongs = [.. MasterList];
        realmAlbums = [.. _albumRepo.GetAll()];
        realGenres = [..  _genreRepo.GetAll()];
        realmArtists = [.. _artistRepo.GetAll()];
        realmAAGSL = [.. _aagslRepo.GetAll()];

    }

    public LoadSongsResult? LoadSongs(List<string> folderPaths)
    {
        List<string> allFiles = MusicFileProcessor.GetAllFiles(folderPaths);
        Debug.WriteLine("Got All Files");

         if (allFiles.Count == 0)
        {
            return null;
        }

        GetInitialValues();

        // Use existing data or empty lists if null.
        List<ArtistModel> existingArtists = realmArtists ?? new List<ArtistModel>();
        List<AlbumArtistGenreSongLink> existingLinks = realmAAGSL ?? new List<AlbumArtistGenreSongLink>();
        List<AlbumModel> existingAlbums = realmAlbums ?? new List<AlbumModel>();
        List<GenreModel> existingGenres = realGenres ?? new List<GenreModel>();
        List<SongModel> oldSongs = realmSongs ?? new List<SongModel>();

        List<ArtistModel> newArtists = new List<ArtistModel>();
        List<AlbumModel> newAlbums = new List<AlbumModel>();
        List<AlbumArtistGenreSongLink> newLinks = new List<AlbumArtistGenreSongLink>();
        List<GenreModel> newGenres = new List<GenreModel>();
        List<SongModel> allSongs = new List<SongModel>();

        // Dictionaries to prevent duplicate processing.
        Dictionary<string, ArtistModel> artistDict = new Dictionary<string, ArtistModel>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, AlbumModel> albumDict = new Dictionary<string, AlbumModel>();
        Dictionary<string, GenreModel> genreDict = new Dictionary<string, GenreModel>();

        int totalFiles = allFiles.Count;
        int processedFiles = 0;

        foreach (string file in allFiles)
        {
            processedFiles++;
            if (MusicFileProcessor.IsValidFile(file))
            {
                var songData = MusicFileProcessor.ProcessFile(
                    file,
                    existingAlbums, albumDict, newAlbums, oldSongs,
                    newArtists, artistDict, newLinks, existingLinks, existingArtists,
                    newGenres, genreDict, existingGenres);

                if (songData.song != null)
                {
                    allSongs.Add(songData.song);


                    var ProcessedFiles = processedFiles;
                    var TotalFiles = totalFiles;
                    var ProgressPercent = (double)processedFiles / totalFiles * 100.0;
                    
                }
            }
        }


        Debug.WriteLine("All files processed.");

        if (allSongs.Count < 1 )
        {
            return null;
        }
        MasterList= [.. allSongs];

        _state.SetCurrentPlaylist([], null); 

        _songRepo.AddOrUpdate(allSongs);

    _genreRepo.AddOrUpdate(newGenres);
    _aagslRepo.AddOrUpdate(newLinks);
    _albumRepo.AddOrUpdate(newAlbums);
    _artistRepo.AddOrUpdate(newArtists);
        return new LoadSongsResult
        {
            Artists = newArtists,
            Albums = newAlbums,
            Links = newLinks,
            Songs = allSongs,
            Genres = newGenres
        };
    }

    #endregion

    // Public implementation of Dispose pattern callable by consumers.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources here.
            _state.Dispose();
        }

        // Free unmanaged resources here (if any).

        _disposed = true;
    }

    // Finalizer to ensure resources are released if Dispose is not called.
    ~BaseAppFlow()
    {
        Dispose(false);
    }
}
