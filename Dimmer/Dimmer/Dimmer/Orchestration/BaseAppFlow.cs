//using Dimmer.DimmerLive.Models;


using Dimmer.Interfaces.Services;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{

    //public static ParseUser? CurrentUserOnline { get; set; }
    
    public static UserModel CurrentUser{ get; set; }
    public static UserModelView CurrentUserView { get; set; }

    public readonly IDimmerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _aagslRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<AppStateModel> _appstateRepo;
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
        IRepository<AppStateModel> appstateRepo,
        ISettingsService settings,
        IFolderMgtService folderMgt, SubscriptionManager subs,
        IMapper mapper)
    {
        _appstateRepo = appstateRepo;
        _state = state;
        _songRepo = songRepo;
        _pdlRepo = pdlRepo;
        _playlistRepo = playlistRepo;
        _artistRepo = artistRepo;
        _albumRepo = albumRepo;
        _genreRepo = genreRepo;
        _aagslRepo = aagslRepo;
        //settings.LoadSettings();
        _settings = settings;
        this.folderMgt=folderMgt;
        _mapper = mapper;
        _userRepo = userRepo;

        Initialize();

    }
    public static AppStateModelView DimmerAppState { get; set; }
    public static IReadOnlyCollection<SongModel> MasterList { get; internal set; }

    public static bool IsAppInitialized;
    public void Initialize(bool isAppInit=false)
    {
       var DimmerAppStates = _appstateRepo.GetAll().ToList();
        var usrs = _userRepo.GetAll().ToList();
        if (usrs is null || usrs.Count <1)
        {
            CurrentUser=new();

            CurrentUserView = _mapper.Map<UserModelView>(CurrentUser);
            _userRepo.AddOrUpdate(CurrentUser);

        }
        else
        {
            CurrentUser = usrs[0];
            CurrentUserView = _mapper.Map<UserModelView>(CurrentUser);
        }
        if (DimmerAppStates is null || DimmerAppStates.Count < 1)
        {
            DimmerAppState= new AppStateModelView()
            {
                MinimizeToTrayPreference = true,

            };
            _appstateRepo.AddOrUpdate(_mapper.Map<AppStateModel>(DimmerAppState));
            
        }
        else
        {
            DimmerAppState = _mapper.Map<AppStateModelView>(DimmerAppStates[0]);
            

        }
        if (isAppInit)
        {
            return;
        }
        folderMgt.RestartWatching();
        IsAppInitialized = isAppInit; // Assigning to the static field only when the condition is met
        MasterList = [.. _songRepo
            .GetAll(true)];



        // 3) live updates, on UI‑thread if available
        var syncCtx = SynchronizationContext.Current;
        IScheduler scheduler = syncCtx != null
            ? new SynchronizationContextScheduler(syncCtx)
            : TaskPoolScheduler.Default;
     
        SubscribeToStateChanges();

        _state.SetCurrentPlaylist([], null);
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
        if (MasterList.Count < 1 && !isAppInit)
        {

            AppUtils.IsUserFirstTimeOpening = true;
            return;
        }
        
        _state.SetSecondSelectdSong(MasterList.First());
        _state.SetCurrentSong(MasterList.First());
        
       
    }

    private void LoadUser()
    {
        var user = _userRepo.GetAll().FirstOrDefault();
        CurrentUser = user;
        
        CurrentUserView = _mapper.Map<UserModelView>(CurrentUserView);
        if (user != null
            && !string.IsNullOrWhiteSpace(user.UserPassword)
            && user.UserPassword != "Unknown Password")
        {
            // fire-and-forget, but handle everything inside
            _ = Task.Run(async () =>
            {
                try
                {
                    //if (user.SessionToken is not null)
                    //{
                    //    await ParseClient.Instance.BecomeAsync(user.SessionToken);
                    //    return;
                    //}
                    //var online = await ParseClient.Instance
                    //    .LogInWithAsync(user.UserName, user.UserPassword)
                    //    .ConfigureAwait(false);

                    // marshal back to UI thread
                    //MainThread.BeginInvokeOnMainThread(() =>
                    //    CurrentUserOnline = online
                    //);
                }
                catch (Exception pe) 
                {
                    // bad credentials → ignore
                }
                
            });
        }
    }

    static List<string> listofPathsAddedInSession = new();
    private void SubscribeToStateChanges()
    {
        _state.CurrentPlayBackState
            .Subscribe(state =>
            {
                //IsPlaying = state.State == DimmerPlaybackState.Playing;
                switch (state.State)
                {
                   
                    case DimmerPlaybackState.FolderAdded:
                        if (state.ExtraParameter is not string folder)
                        {
                            return;
                        }
                        if (listofPathsAddedInSession.Contains(folder))
                        {
                            return;
                        }
                        listofPathsAddedInSession.Add(folder);
                        AddFolderToPath(folder);
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

    public void AddFolderToPath(string? path=null)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        var exist = _appstateRepo.GetAll();


        if (exist != null)
        {
            var appStates = exist.ToList();
            var rappState= appStates.First();
            var appState = new AppStateModel(rappState);



            foreach (var item in DimmerAppState.UserMusicFoldersPreference)
            {
                if (!appState.UserMusicFoldersPreference.Contains(item, StringComparer.OrdinalIgnoreCase))
                {
                    appState.UserMusicFoldersPreference.Add(item);
                }
            }
            
            appState.UserMusicFoldersPreference.Add(path);
            DimmerAppState .UserMusicFoldersPreference.Add(path);
            _appstateRepo.AddOrUpdate(appState);
        }

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
            Log = $"{CurrentlyPlayingSong.Title} - Dimmer State : {type}",
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

    public async Task<LoadSongsResult?> LoadSongs(List<string> folderPaths)
    {
        var _config = new ProcessingConfig(); // Use default config or load from settings
        ICoverArtService coverArtService = new CoverArtService(_config);


        IMusicMetadataService currentScanMetadataService = new MusicMetadataService();
        IAudioFileProcessor audioFileProcessor = new AudioFileProcessor(
            new CoverArtService(_config),
            currentScanMetadataService,
            _config);

        _state.SetCurrentLogMsg(new AppLogModel { Log = "Starting music scan..." });

        List<string> allFiles = AudioFileUtils.GetAllAudioFilesFromPaths(folderPaths, _config.SupportedAudioExtensions);
        Debug.WriteLine($"[MANAGER]: Found {allFiles.Count} audio files to process.");


        if (allFiles.Count == 0)
        {
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No audio files found in the selected paths." });
            return null;
        }

        GetInitialValues();

        currentScanMetadataService.LoadExistingData(realmArtists, realmAlbums, realGenres, realmSongs);

        int totalFiles = allFiles.Count;
        int processedFileCount = 0;

        foreach (string file in allFiles)
        {
            processedFileCount++;
            var fileProcessingResult = await audioFileProcessor.ProcessFileAsync(file);

            if (fileProcessingResult.Success && fileProcessingResult.ProcessedSong != null)
            {
                _state.SetCurrentLogMsg(new AppLogModel
                {
                    Log = $"Processed: {fileProcessingResult.ProcessedSong.Title} ({processedFileCount}/{totalFiles})",
                    AppSongModel = fileProcessingResult.ProcessedSong // Or map to YourAppSongModel
                });
            }
            else if (fileProcessingResult.Skipped)
            {
                _state.SetCurrentLogMsg(new AppLogModel { Log = $"Skipped: {fileProcessingResult.SkipReason} ({processedFileCount}/{totalFiles})" });
            }
            else
            {
                string errors = string.Join("; ", fileProcessingResult.Errors);
                _state.SetCurrentLogMsg(new AppLogModel { Log = $"Error processing {Path.GetFileName(file)}: {errors} ({processedFileCount}/{totalFiles})" });
            }
        }




        Debug.WriteLine("[MANAGER]: All files processing loop completed.");


        var newCoreSongs = currentScanMetadataService.GetAllSongs();
        var newCoreArtists = currentScanMetadataService.GetAllArtists();
        var newCoreAlbums = currentScanMetadataService.GetAllAlbums();
        var newCoreGenres = currentScanMetadataService.GetAllGenres();


        if (!newCoreSongs.Any() && !newCoreArtists.Any() && !newCoreAlbums.Any() && !newCoreGenres.Any())
        {
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No new music data was found or processed." });
            // Still check if only links need to be updated, though less likely if no new core entities
            // For now, returning null if no new core entities.
            return null;
        }

        // --- Create New Link Objects ---
        var newAppLinks = new List<AlbumArtistGenreSongLink>();
        foreach (var coreSong in newCoreSongs)
        {
            if (coreSong.AlbumId == null || coreSong.GenreId == null)
            {
                Debug.WriteLine($"[WARNING] Song '{coreSong.Title}' missing AlbumId or GenreId, cannot create full links.");
                continue; // Skip if essential IDs for the link are missing
            }

            foreach (var artistId in coreSong.ArtistIds)
            {
                // Check if this specific link already exists in your DB (realmAAGSL)
                // This is important to avoid duplicate link entries.
                bool linkExists = realmAAGSL?.Any(l =>
                    l.SongId == coreSong.Id &&
                    l.ArtistId == artistId &&
                    l.AlbumId == coreSong.AlbumId && // If AlbumId/GenreId are part of the link's uniqueness
                    l.GenreId == coreSong.GenreId) ?? false;

                // Also check if we've already created this link in the current batch
                bool linkAlreadyInNewBatch = newAppLinks.Any(l =>
                    l.SongId == coreSong.Id &&
                    l.ArtistId == artistId &&
                    l.AlbumId == coreSong.AlbumId &&
                    l.GenreId == coreSong.GenreId);

                if (!linkExists && !linkAlreadyInNewBatch)
                {
                    newAppLinks.Add(new AlbumArtistGenreSongLink
                    {
                        // LinkId is auto-generated by default in the model
                        SongId = coreSong.Id, // This is the ID from Dimmer.Core.Models.Song
                        ArtistId = artistId,  // This is the ID from Dimmer.Core.Models.Artist
                        AlbumId = coreSong.AlbumId,
                        GenreId = coreSong.GenreId
                    });
                }
            }
        }
        Debug.WriteLine($"[MANAGER]: Generated {newAppLinks.Count} new link objects.");


        if (newCoreSongs.Any())
            _songRepo.AddOrUpdate(newCoreSongs);
        if (newCoreGenres.Any())
            _genreRepo.AddOrUpdate(newCoreGenres);
        if (newAppLinks.Any())
            _aagslRepo.AddOrUpdate(newAppLinks);
        if (newCoreAlbums.Any())
            _albumRepo.AddOrUpdate(newCoreAlbums);
        if (newCoreArtists.Any())
            _artistRepo.AddOrUpdate(newCoreArtists);
            return new LoadSongsResult
        {
            Artists = [.. newCoreArtists],
            Albums = [.. newCoreAlbums],
            Links = [.. newAppLinks],
            Songs = [.. newCoreSongs],
            Genres = [.. newCoreGenres]
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
