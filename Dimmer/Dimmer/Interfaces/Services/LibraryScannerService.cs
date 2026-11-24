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
            // 1. Get Folders
            using (var realm = _realmFactory.GetRealmInstance())
            {
                if (folderPaths == null || folderPaths.Count == 0)
                {
                    var existingState = realm.All<AppStateModel>().FirstOrDefault();
                    folderPaths = existingState?.UserMusicFolders
                        .Select(x => x.SystemFolderPath)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList() ?? new List<string>();
                }
            }

            if (folderPaths.Count == 0)
                return new LoadSongsResult { NewSongsAddedCount = 0 };

            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg("Starting music scan...", DimmerLogLevel.Info);



            // 2. Discover disk files
            List<string> allDiskFiles = await TaggingUtils.GetAllAudioFilesFromPathsAsync(folderPaths, _config.SupportedAudioExtensions).ConfigureAwait(false);

            if (allDiskFiles.Count == 0)
            {
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No files found.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };
            }

            var diskFilesSet = new HashSet<string>(allDiskFiles, StringComparer.OrdinalIgnoreCase);
            var existingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 3. Handle Deletions & Cache DB state
            using (var realm = _realmFactory.GetRealmInstance())
            {
                foreach (var s in realm.All<SongModel>())
                {
                    if (!string.IsNullOrEmpty(s.FilePath)) existingPaths.Add(s.FilePath);
                    if (!string.IsNullOrEmpty(s.TitleDurationKey)) existingKeys.Add(s.TitleDurationKey);
                }

                if (isIncremental)
                {
                    var filesToDelete = realm.All<SongModel>()
                        .AsEnumerable()
                        .Where(dbSong => !string.IsNullOrEmpty(dbSong.FilePath) &&
                                         folderPaths.Any(fp => dbSong.FilePath.StartsWith(fp, StringComparison.OrdinalIgnoreCase)) &&
                                         !diskFilesSet.Contains(dbSong.FilePath)) // O(1) Lookup
                        .ToList();

                    if (filesToDelete.Count > 0)
                    {
                        _logger.LogInformation("Removing {Count} deleted files from database...", filesToDelete.Count);
                        await realm.WriteAsync(() =>
                        {
                            foreach (var song in filesToDelete) realm.Remove(song);
                        });
                    }
                }
            }

            // 4. Extract only new files
            var newFilesToProcess = allDiskFiles.Where(f => !existingPaths.Contains(f)).ToList();

            if (newFilesToProcess.Count == 0)
            {
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No new files.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };
            }

            // 5. Parse Audio Metadata in Parallel
            MusicMetadataService currentScanMetadataService = new();
            var audioFileProcessor = new AudioFileProcessor(currentScanMetadataService, _config);
            var processedResults = await audioFileProcessor.ProcessFilesInParallelForEachAsync(newFilesToProcess).ConfigureAwait(false);

            var newSongs = processedResults
                .Where(r => r.Success && r.ProcessedSong != null)
                .Select(r => r.ProcessedSong!)
                .DistinctBy(s => s.TitleDurationKey)
                .Where(s => !existingKeys.Contains(s.TitleDurationKey))
                .ToList();

            // 6. FAST BATCH INSERT TO REALM
            if (newSongs.Count > 0)
            {
                using (var realm = _realmFactory.GetRealmInstance())
                {
                    await realm.WriteAsync(() =>
                    {
                        // Pre-cache existing DB models into Dictionaries (O(1) lookups!)
                        var genreCache = realm.All<GenreModel>().ToDictionary(g => g.Name, g => g, StringComparer.OrdinalIgnoreCase);
                        var artistCache = realm.All<ArtistModel>().ToDictionary(a => a.Name, a => a, StringComparer.OrdinalIgnoreCase);
                        var albumCache = realm.All<AlbumModel>().ToDictionary(al => al.Name, al => al, StringComparer.OrdinalIgnoreCase);

                        foreach (var songView in newSongs)
                        {
                            var songModel = songView.ToSongModel();
                            if (songModel is null) continue;

                            // Link Genre
                            if (!string.IsNullOrWhiteSpace(songView.GenreName))
                            {
                                if (!genreCache.TryGetValue(songView.GenreName, out var genre))
                                {
                                    genre = realm.Add(new GenreModel { Id = ObjectId.GenerateNewId(), Name = songView.GenreName });
                                    genreCache[songView.GenreName] = genre;
                                }
                                songModel.Genre = genre;
                            }

                            // Link Artists
                            if (!string.IsNullOrWhiteSpace(songView.OtherArtistsName))
                            {
                                var artNames = songView.OtherArtistsName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                foreach (var art in artNames)
                                {
                                    if (!artistCache.TryGetValue(art, out var artist))
                                    {
                                        artist = realm.Add(new ArtistModel { Id = ObjectId.GenerateNewId(), Name = art });
                                        artistCache[art] = artist;
                                    }

                                    songModel.Artist = artist;
                                    songModel.ArtistToSong.Add(artist);
                                    artist.TotalSongsByArtist++;
                                }
                            }

                            // Link Album
                            if (!string.IsNullOrWhiteSpace(songView.AlbumName))
                            {
                                if (!albumCache.TryGetValue(songView.AlbumName, out var album))
                                {
                                    album = realm.Add(new AlbumModel { Id = ObjectId.GenerateNewId(), Name = songView.AlbumName });
                                    albumCache[songView.AlbumName] = album;
                                }

                                if (songModel.Artist != null && !album.Artists.Contains(songModel.Artist))
                                {
                                    album.Artists.Add(songModel.Artist);
                                }
                                songModel.Album = album;
                            }

                            songModel.IsNew = false;
                            realm.Add(songModel, update: true);
                        }
                    });
                }
            }

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