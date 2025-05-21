//using Dimmer.DimmerLive.Models;


using Dimmer.Interfaces.Services;
using System.Diagnostics;
using static Dimmer.Utilities.AppUtils;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{

    //public static ParseUser? CurrentUserOnline { get; set; }
    
    public static UserModel CurrentUser{ get; set; }
    public static UserModelView CurrentUserView { get; set; }

    public readonly IDimmerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRepository<DimmerPlayEvent> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<UserModel> _userRepo;
    private readonly IRepository<AppStateModel> _appstateRepo;
    private readonly ISettingsService _settings;
    IRealmFactory realmFactory;
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
        IRepository<DimmerPlayEvent> pdlRepo,
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
        //folderMgt.RestartWatching();
        IsAppInitialized = isAppInit; // Assigning to the static field only when the condition is met
        MasterList = [.. _songRepo
            .GetAll(true)];



        SubscribeToStateChanges();

        _state.SetCurrentPlaylist([], null);
       
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
                        if (state.ExtraParameter is null )
                        {
                            _settings.ClearAllFolders();
                            return;
                        }
                        _settings.RemoveMusicFolder((string)state.ExtraParameter);
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
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Seeked, position);
    }

    public void PlaySong()
    {
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Play);
    }

    public void AddPauseSongEventToDB()
    {
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Pause);
    }

    public void AddResumeSongToDB()
    {
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Resume);
    }

    public void PlayEnded()
    {
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Completed);
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
        SongModelView? song,
        PlayType type,
        double? position = null)
    {
        var songDb = _mapper.Map<SongModel>(CurrentlyPlayingSong);


        var link = new DimmerPlayEvent
        {
            SongId = song.Id,
            Song= songDb,
            PlayType = (int)type,
            DatePlayed = DateTime.Now,
            PositionInSeconds = position ?? 0,
            WasPlayCompleted = type == PlayType.Completed
        };

        _pdlRepo.AddOrUpdate(link);

        // Generate the user-friendly log message
        string userMessage = UserFriendlyLogGenerator.GetPlaybackStateMessage(type, CurrentlyPlayingSong, position);


        AppLogModel log = new()
        {
            Log = userMessage,
            ViewSongModel = CurrentlyPlayingSong,
            
        };
        _state.SetCurrentLogMsg(log);
    }


    public void UpSertUser(UserModel model)
    {
        
        
        _userRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert User {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }


    public void UpSertPlaylist(PlaylistModel model)
    {
        _playlistRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert Playlist {model} at {DateTime.Now.ToLocalTime()}",            
        };
        _state.SetCurrentLogMsg(log);
    }

    public void UpSertArtist(ArtistModel model)
    {
        _artistRepo.AddOrUpdate(model);

        AppLogModel log = new()
        {
            Log = $"UpSert Artist {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }

    public void UpSertAlbum(AlbumModel model)
    {
        _albumRepo.AddOrUpdate(model);
        AppLogModel log = new()
        {
            Log = $"UpSert Album {model} at {DateTime.Now.ToLocalTime()}",
        };
        _state.SetCurrentLogMsg(log);
    }
    
    public void UpSertSong(SongModel model)
    { 
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
     
        // 2) Do everything in one Realm transaction
        _songRepo.BatchUpdate(realm =>
        {
            // 3) Fetch or add the song itself
            var song = realm.Find<SongModel>(model.Id)
                       ?? realm.Add(model, update: true);

          
                   

                    song.UserNotes.Add(note);
               
        });

        // 6) Log after the write completes
        var log = new AppLogModel
        {
            Log          = $"UpSertSongNote on {model.Id} at {DateTime.Now:O}",
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
    void GetInitialValues()
    {
        MasterList = [.. _songRepo
            .GetAll()
            .OrderBy(x => x.DateCreated)];
        
        realmSongs = [.. MasterList];
        realmAlbums = [.. _albumRepo.GetAll()];
        realGenres = [..  _genreRepo.GetAll()];
        realmArtists = [.. _artistRepo.GetAll()];

    }

    public async Task<LoadSongsResult?> LoadSongs(List<string> folderPaths)
    {
        var _config = new ProcessingConfig(); // Use default config or load from settings
        ICoverArtService coverArtService = new CoverArtService(_config);


        MusicMetadataService currentScanMetadataService = new();
        AudioFileProcessor audioFileProcessor = new AudioFileProcessor(
            coverArtService,
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
        foreach (var sng in currentScanMetadataService.GetAllSongs().Take(5)) // Check first few
        {
            Debug.WriteLine($"Service Song: {sng.Title}, IsManaged: {sng.IsManaged}, Album IsManaged: {(sng.Album?.IsManaged.ToString() ?? "N/A")}");
        }

        IReadOnlyList<SongModel> newCoreSongsFromService = currentScanMetadataService.GetAllSongs();
        Debug.WriteLine("--- Before Realm.Write - Checking newCoreSongsFromService ---");
        foreach (var sng in newCoreSongsFromService.Take(20)) // Check more songs
        {
            Debug.WriteLine($"Pre-Write Song: {sng.Title}, IsManaged: {sng.IsManaged}, Album IsManaged: {(sng.Album?.IsManaged.ToString() ?? "N/A")}");
            if (sng.Album != null && sng.Album.IsManaged)
            {
                Debug.WriteLine($"   Pre-Write Album {sng.Album.Name} Realm Hash: {sng.Album.Realm?.GetHashCode()}");
            }
            // Log artists and genre too
        }
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
                    AppSongModel = fileProcessingResult.ProcessedSong ,
                    AppScanLogModel = new AppScanLogModel() { TotalFiles = totalFiles, CurrentFilePosition = processedFileCount },
                    ViewSongModel = _mapper.Map<SongModelView>(fileProcessingResult.ProcessedSong)
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

        IReadOnlyList<SongModel>? newCoreSongs = currentScanMetadataService.GetAllSongs();
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
        realmFactory = IPlatformApplication.Current!.Services.GetService<IRealmFactory>()!;
        using (var realm = realmFactory.GetRealmInstance()) // Get ONE Realm instance
        {
            Debug.WriteLine($"Current Realm instance for Write block: HashCode={realm.GetHashCode()}");
            realm.Write(() =>
            {
                // Add related entities first. They become managed by THIS 'realm' instance.
                if (newCoreAlbums.Any())
                {
                    foreach (var album in newCoreAlbums)
                    {
                        if (!album.IsManaged)
                        {
                            realm.Add(album, update: true);
                        }
                    }
                     
                }
                if (newCoreArtists.Any())
                {
                    foreach (var artist in newCoreArtists)
                    {
                        if (!artist.IsManaged)
                        {
                            realm.Add(artist, update: true);
                        }
                    }
                }
                if (newCoreGenres.Any())
                {
                    foreach (var genre in newCoreGenres)
                    {
                        if (!genre.IsManaged)
                        { 
                            realm.Add(genre, update: true);
                        }
                    }
                }
                // Now add/update songs
                if (newCoreSongs.Any())
                {
                    foreach (var song in newCoreSongs)
                    {
                        Debug.WriteLine($"Processing song: {song.Title}, IsManaged: {song.IsManaged}");
                        if (song.Album != null)
                        {
                            Debug.WriteLine($"  Song's Album: {song.Album.Name}, IsManaged: {song.Album.IsManaged}, Album.Id: {song.Album.Id}, Realm Hash: {(song.Album.IsManaged ? song.Album.Realm?.GetHashCode().ToString() : "N/A")}");
                        }

                        if (!song.IsManaged) // Song itself is new
                        {
                            // Fixup related objects to be from the current realm or unmanaged with PK
                            if (song.Album != null)
                            {
                                if (song.Album.IsManaged && song.Album.Realm != realm)
                                {
                                    // Album is managed by another realm, find it in the current realm
                                    var managedAlbum = realm.Find<AlbumModel>(song.Album.Id);
                                    if (managedAlbum != null)
                                    {
                                        song.Album = managedAlbum;
                                    }
                                    else
                                    {
                                        // This case means the album (with song.Album.Id) isn't in the current realm.
                                        // If it was supposed to be new, it should have been in newCoreAlbums and added.
                                        // If it's an existing album by ID but not found, it's a data consistency issue.
                                        // For now, we'll assume it should either be found or was unmanaged to begin with.
                                        // If `song.Album` was unmanaged, Realm would link it by PK when `song` is added,
                                        // provided `song.Album` (or an equivalent by PK) was added via `newCoreAlbums`.
                                        Debug.WriteLine($"WARNING: Album '{song.Album.Name}' (ID: {song.Album.Id}) for song '{song.Title}' was managed by a different realm but not found in the current realm. This might cause issues or indicate it should have been an unmanaged object.");
                                        // Potentially set song.Album = null or handle as error if it must exist.
                                        // Or, if song.Album itself (the instance) was meant to be added, it should have
                                        // been unmanaged.
                                        // The crucial part for THIS exception is that `song.Album` must not be
                                        // an instance managed by another realm. If `managedAlbum` is null,
                                        // and `song.Album` was indeed from another realm, we've now "detached" it by
                                        // effectively trying to re-assign. If `song.Album` was *not* from another realm,
                                        // this block wasn't entered.
                                        // If `Find` returns null, the original `song.Album` (from other realm) remains.
                                        // THIS IS THE PROBLEM: we MUST reassign.
                                        // A better strategy if not found:
                                        // 1. If it's an error, throw.
                                        // 2. If it's okay for the link to be broken, set song.Album = null.
                                        // 3. If the album was supposed to be new and represented by song.Album,
                                        //    then song.Album should have been unmanaged.
                                        // For the exception, we *must* change song.Album if it's foreign-managed.
                                        song.Album = managedAlbum; // This will be null if not found
                                                                   // If it's null, the link will be broken unless `realm.Add` can match by PK
                                                                   // from an unmanaged `song.Album` instance that was already part of `newCoreAlbums`.
                                                                   // Safest for this specific error is to ensure it's not foreign-managed.
                                    }
                                }
                                // If song.Album is unmanaged, its PK will be used for lookup by realm.Add(song, update:true)
                                // If song.Album is managed by current 'realm', it's fine.
                            }

                            if (song.Genre != null)
                            {
                                if (song.Genre.IsManaged && song.Genre.Realm != realm)
                                {
                                    var managedGenre = realm.Find<GenreModel>(song.Genre.Id);
                                    song.Genre = managedGenre; // Assign, even if null
                                }
                            }

                            if (song.ArtistIds != null && song.ArtistIds.Any())
                            {
                                var newArtistListForSong = new List<ArtistModel>();
                                bool artistsModified = false;
                                foreach (var artistRef in song.ArtistIds)
                                {
                                    if (artistRef.IsManaged && artistRef.Realm != realm)
                                    {
                                        artistsModified = true;
                                        var managedArtist = realm.Find<ArtistModel>(artistRef.Id);
                                        if (managedArtist != null)
                                        {
                                            newArtistListForSong.Add(managedArtist);
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"WARNING: Artist '{artistRef.Name}' (ID: {artistRef.Id}) for song '{song.Title}' was managed by a different realm but not found in the current realm. Omitting from this song.");
                                        }
                                    }
                                    else
                                    {
                                        // Artist is unmanaged (OK, will be linked by PK if added via newCoreArtists)
                                        // or managed by current realm (OK)
                                        newArtistListForSong.Add(artistRef);
                                    }
                                }

                                if (artistsModified)
                                {
                                    // Assuming song.ArtistIds is a standard List<T> as song is unmanaged
                                    song.ArtistIds.Clear();
                                    foreach (var art in newArtistListForSong)
                                    {
                                        song.ArtistIds.Add(art);
                                    }
                                }
                            }

                            // The redundant try-catch block has been removed.
                            // One try-catch for the add operation is sufficient.
                            try
                            {
                                Debug.WriteLine($"Attempting to add/update song: {song.Title}. song.IsManaged is {song.IsManaged}");
                                realm.Add(song, update: true);
                            }
                            catch (Realms.Exceptions.RealmObjectManagedByAnotherRealmException rre)
                            {
                                Debug.WriteLine($"STILL FAILED for song {song.Title}: {rre.Message}");
                                Debug.WriteLine($"  Song: {song.Title}, IsManaged: {song.IsManaged}");
                                if (song.Album != null)
                                    Debug.WriteLine($"  Album: {song.Album.Name}, IsManaged: {song.Album.IsManaged}, Realm: {(song.Album.IsManaged ? song.Album.Realm?.GetHashCode().ToString() : "N/A")}, CurrentRealm: {realm.GetHashCode()}");
                                if (song.Genre != null)
                                    Debug.WriteLine($"  Genre: {song.Genre.Name}, IsManaged: {song.Genre.IsManaged}, Realm: {(song.Genre.IsManaged ? song.Genre.Realm?.GetHashCode().ToString() : "N/A")}");
                                foreach (var art in song.ArtistIds)
                                {
                                    if (art != null)
                                        Debug.WriteLine($"  Artist: {art.Name}, IsManaged: {art.IsManaged}, Realm: {(art.IsManaged ? art.Realm?.GetHashCode().ToString() : "N/A")}");
                                }
                                throw; // Rethrow to ensure transaction rollback and signal error
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"ERROR adding song {song.Title}: {ex}");
                                // Log details of song and its relations here to debug further if needed
                                throw; // Rethrow to ensure transaction rollback
                            }
                        }
                        else // Song is already managed
                        {
                            // If song.IsManaged is true, it should ideally be managed by the current 'realm'.
                            // If song.Realm != realm, you might have issues modifying it.
                            // For now, we assume if it's managed, it's by the correct realm or it's a read-only scenario for it.
                            // No realm.Add() needed for managed objects. Modifications are automatically persisted.
                            // If you needed to ensure it's part of *this* transaction's realm:
                            // if(song.Realm != realm) { song = realm.Find<SongModel>(song.Id); /* then modify this 'song' instance */ }
                        }
                    }
                }
            }); // End realm.Write
            MasterList = _songRepo.GetAll(false);
            _state.SetCurrentState(new(DimmerPlaybackState.FolderScanCompleted, newCoreSongs));
            Debug.WriteLine($"All Folders Scanned Now Count is {MasterList.Count}");
            return new LoadSongsResult
            {
                Artists = [.. newCoreArtists],
                Albums = [.. newCoreAlbums],
                Songs = [.. newCoreSongs],
                Genres = [.. newCoreGenres]
            };
        }
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
