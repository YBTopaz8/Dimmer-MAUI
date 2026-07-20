namespace Dimmer.Interfaces.Services;

public class LibraryScannerService : ILibraryScannerService
{
    private readonly IRepository<DimmerPlayEvent> _playEventsRepo;
    private readonly ISettingsService settingsService;
    private readonly IDimmerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<AppStateModel> _appStateRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRealmFactory _realmFactory;
    private readonly ILogger<LibraryScannerService> _logger;
    private readonly ProcessingConfig _config;

    public LibraryScannerService(
        IDimmerStateService state, IRepository<AppStateModel> appStateRepo,
        IRepository<SongModel> songRepo, IRepository<AlbumModel> albumRepo,
        IRepository<ArtistModel> artistRepo, IRepository<GenreModel> genreRepo,
        IRealmFactory realmFactory, ILogger<LibraryScannerService> logger
        , IRepository<DimmerPlayEvent> playEventsRepo,
        ISettingsService settingsService,
        ProcessingConfig? config = null)
    {
        _playEventsRepo=playEventsRepo;
        this.settingsService=settingsService;
        _appStateRepo=appStateRepo;
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _songRepo = songRepo ?? throw new ArgumentNullException(nameof(songRepo));
        _albumRepo = albumRepo ?? throw new ArgumentNullException(nameof(albumRepo));
        _artistRepo = artistRepo ?? throw new ArgumentNullException(nameof(artistRepo));
        _genreRepo = genreRepo ?? throw new ArgumentNullException(nameof(genreRepo));
        _realmFactory = realmFactory ?? throw new ArgumentNullException(nameof(realmFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? new ProcessingConfig();

    }

    public async Task<LoadSongsResult> ScanLibrary(List<string>? folderPaths, bool isIncremental = false)
    {
        try
        {
            // --- REALM BLOCK 1: GET FOLDERS ---
            // Open, read, and close Realm entirely before we do any async work.
            using (var realm = _realmFactory.GetRealmInstance())
            {
                if (folderPaths == null || folderPaths.Count == 0)
                {
                    var existingState = realm.All<AppStateModel>().FirstOrDefault();
                    folderPaths = existingState?.UserMusicFolders.Select(x => x.SystemFolderPath).ToList() ?? new List<string>();
                }
            }

            if (folderPaths == null || folderPaths.Count == 0) return new LoadSongsResult { NewSongsAddedCount = 0 };

            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg("Starting music scan...", DimmerLogLevel.Info);

            // --- ASYNC WORK: NO REALM ALLOWED HERE ---
            List<string> allDiskFiles = await TaggingUtils.GetAllAudioFilesFromPathsAsync(folderPaths, _config.SupportedAudioExtensions).ConfigureAwait(false);

            HashSet<string> existingPaths = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> existingKeys = new(StringComparer.OrdinalIgnoreCase);

            // --- REALM BLOCK 2: CACHE DB STATE & HANDLE DELETIONS ---
            using (var realm = _realmFactory.GetRealmInstance())
            {
                // Iterating through Realm is fast. We extract ONLY the strings we need and detach them from Realm.
                // This prevents out-of-memory errors and thread issues.
                foreach (var s in realm.All<SongModel>())
                {
                    if (!string.IsNullOrEmpty(s.FilePath)) existingPaths.Add(s.FilePath);
                    if (!string.IsNullOrEmpty(s.TitleDurationKey)) existingKeys.Add(s.TitleDurationKey);
                }

                if (isIncremental)
                {
                    // Find songs in DB that belong to the scanned folders, but are NO LONGER on the disk
                    var filesToDelete = new List<SongModel>();
                    foreach (var dbSong in realm.All<SongModel>())
                    {
                        if (string.IsNullOrEmpty(dbSong.FilePath)) continue;

                        bool inScannedFolder = folderPaths.Any(fp => dbSong.FilePath.StartsWith(fp, StringComparison.OrdinalIgnoreCase));
                        bool missingFromDisk = !allDiskFiles.Contains(dbSong.FilePath, StringComparer.OrdinalIgnoreCase);

                        if (inScannedFolder && missingFromDisk)
                        {
                            filesToDelete.Add(dbSong);
                        }
                    }

                    if (filesToDelete.Count != 0)
                    {
                        _logger.LogInformation("Detected {Count} deleted files. Removing from database...", filesToDelete.Count);
                       await realm.WriteAsync(() => // Synchronous Write guarantees we stay on the safe thread
                        {
                            foreach (var songToDelete in filesToDelete)
                            {
                                realm.Remove(songToDelete);
                            }
                        });
                    }
                }
            }

            // --- ASYNC WORK 2: METADATA PARSING (NO REALM ALLOWED HERE) ---
            var newFilesToProcess = allDiskFiles.Where(file => !existingPaths.Contains(file)).ToList();

            if (newFilesToProcess.Count == 0)
            {
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No new files.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };
            }

            MusicMetadataService currentScanMetadataService = new();
            var audioFileProcessor = new AudioFileProcessor(currentScanMetadataService, _config);

            var processedResults = await audioFileProcessor.ProcessFilesInParallelForEachAsync(newFilesToProcess).ConfigureAwait(false);

            var successFullyProcessed = processedResults
                .Where(r => r.Success && r.ProcessedSong != null);

            var notnullProcessedSong = successFullyProcessed
                .Select(r => r.ProcessedSong!);
            var distinctSongsByTitleDur = notnullProcessedSong
                .DistinctBy(s => s.TitleDurationKey);
            var doubleCheckList = distinctSongsByTitleDur
                .Where(s => !existingKeys.Contains(s.TitleDurationKey)) // Double check against DB
                ;
            var newSongs = doubleCheckList.ToList();

            // --- REALM BLOCK 3: INSERT NEW SONGS ---
            if (newSongs.Count > 0)
            {
                using (var realm = _realmFactory.GetRealmInstance())
                {
                  await  realm.WriteAsync(() =>
                    {
                        // Cache DB lookups into Dictionaries to prevent thousands of slow DB queries inside the loop

                        foreach (var songView in newSongs)
                        {
                            var songModel = songView.ToSongModel();
                            if (songModel is null) continue;

                            var genreName = songView.GenreName;
                            // Link Genre
                            if (!string.IsNullOrEmpty(genreName))
                            {
                                var genreInDb = realm.All<GenreModel>().Filter("Name == $0", genreName);
                                var countgenre = genreInDb.Count();
                                var genre = new GenreModel { Id = ObjectId.GenerateNewId(), Name = genreName };
                         
                                if (countgenre == 1)
                                {
                                        genre = genreInDb.First();
                                }
                           
                                if (countgenre == 0)
                                {

                                    genre = realm.Add(genre, update: true);
                                }
                                else if(countgenre > 1) 
                                {
                                    var dupgenre = genreInDb.AsEnumerable().OrderByDescending(x => x.DateCreated).Skip(1).ToList();
                                  foreach (var item in dupgenre)
                                  {
                                        
                                        realm.Remove(item);
                                  }

                                }
                                songModel.Genre = genre;
                            }

                            // Link Artist
                            if (!string.IsNullOrEmpty(songView.OtherArtistsName))
                            {
                                var artName = songView.OtherArtistsName.Split(",");
                                foreach (var art in artName)
                                {

                                    var ArtsInDb = realm.All<ArtistModel>().Filter("Name == $0", art);
                                    var countArtInDb = ArtsInDb.Count();
                                    var artist = new ArtistModel { Id = ObjectId.GenerateNewId(), Name = art };
                                    if (countArtInDb == 0)
                                    {

                                        artist = realm.Add(artist, update: true);
                                    }

                                    else if (countArtInDb == 1)
                                    {
                                        artist = ArtsInDb.First();
                                    }
                                    else if (countArtInDb > 1)
                                    {
                                        var dupArtists = ArtsInDb.AsEnumerable().OrderByDescending(x => x.DateCreated).Skip(1).ToList();
                                     
                                        foreach (var item in dupArtists)
                                        {
                                            realm.Remove(item);
                                        }
                                    }

                                    songModel.Artist = artist;
                                    songModel.ArtistToSong.Add(artist);
                                    artist.TotalSongsByArtist++;

                                }
                            }

                            var alb = songView.AlbumName;
                            // Link Album
                            if (!string.IsNullOrEmpty(alb))
                            {

                                var albsInDb = realm.All<AlbumModel>().Filter("Name == $0", alb);
                                var countArtInDb = albsInDb.Count();
                                var album = new AlbumModel { Id = ObjectId.GenerateNewId(), Name = alb };
                                if (countArtInDb ==0)
                                {

                                    album = realm.Add(album);

                                   
                                }
                                else if (countArtInDb == 1)
                                {
                                    album = albsInDb.First();
                                }
                                else if (countArtInDb > 1)
                                {
                                    var dupAlbs = albsInDb.AsEnumerable().OrderByDescending(x => x.DateCreated).Skip(1).ToList();
                                    foreach (var item in dupAlbs)
                                    {
                                        realm.Remove(item);
                                    }

                                }
                                if (songModel.Artist != null && !album.Artists.Contains(songModel.Artist))
                                    album.Artists.Add(songModel.Artist);

                                songModel.Album = album;

                            }

                            songModel.IsNew = false;
                            realm.Add(songModel, update: true);
                        }
                    });
                }
            }

            // Cleanup & Return
            currentScanMetadataService.ClearAll();
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, extParam: newSongs, null, null));

            return new LoadSongsResult
            {
                NewSongsAddedCount = newSongs.Count,
                ProcessingResults = processedResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScanLibrary Exception: {Message}", ex.Message);
            return new LoadSongsResult { IsError = true, ErrorMessage = ex.Message };
        }
    }

    public async Task ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));

        await ScanLibrary(pathsToScan,isIncremental);
    }
    public void RemoveDupesFromDB()
    {
        
    }
}