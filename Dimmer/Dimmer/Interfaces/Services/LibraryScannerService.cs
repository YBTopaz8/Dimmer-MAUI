using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;


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
    private readonly ICoverArtService _coverArtService;

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

    public async Task<LoadSongsResult> ScanLibrary(List<string>? folderPaths)
    {
        try
        {
            using var realm = _realmFactory.GetRealmInstance();
            if (folderPaths == null || folderPaths.Count == 0)
            {
                _logger.LogWarning("ScanLibrary called with no folder paths.");
                _state.SetCurrentLogMsg(new AppLogModel { Log = "No folders selected for scanning." });

                var existingState = realm.All<AppStateModel>().FirstOrDefault();
                if (existingState is not null)
                {
                    folderPaths = [.. existingState.UserMusicFoldersPreference];
                }
            }

            if (folderPaths == null || folderPaths.Count == 0)
            {
                _logger.LogWarning("No folder paths found to scan.");
                return new LoadSongsResult { NewSongsAddedCount = 0 };

            }

            _logger.LogInformation("Starting library scan for paths: {Paths}", string.Join(", ", folderPaths));
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg(new AppLogModel { Log = "Starting music scan..." });

            MusicMetadataService currentScanMetadataService = new();

            List<string> allFiles = await TaggingUtils.GetAllAudioFilesFromPathsAsync(folderPaths, _config.SupportedAudioExtensions);

            if (allFiles.Count == 0)
            {
                _state.SetCurrentLogMsg(new AppLogModel { Log = "No audio files found in the selected paths." });
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No audio files found.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };


            }
            using (var realmm = _realmFactory.GetRealmInstance())
            {
                _logger.LogDebug("Loading existing metadata from database and detaching for processing...");
                var existingArtists = realmm.All<ArtistModel>().ToList().Select(x =>
                {
                    return x.Freeze().ToArtistModelView();
                }).ToList();
                var existingAlbums = realmm.All<AlbumModel>().ToList().Select(x => x.Freeze().ToAlbumModelView()).ToList();
                var existingGenres = realmm.All<GenreModel>().ToList().Select(x => x.Freeze().ToGenreModelView()).ToList();
                var existingSongs = realmm.All<SongModel>().ToList().Select(x => x.Freeze().ToSongModelView()).ToList();



                _logger.LogDebug("Loaded {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres, {SongCount} songs.",
                    existingArtists.Count(), existingAlbums.Count(), existingGenres.Count(), existingSongs.Count());





                currentScanMetadataService.LoadExistingData(existingArtists, existingAlbums, existingGenres, existingSongs);
            }
            var newFilesToProcess = allFiles.Where(file => !currentScanMetadataService.HasFileBeenProcessed(file)).ToList();

            int totalFilesToProcess = newFilesToProcess.Count;
            if (totalFilesToProcess == 0)
            {
                _logger.LogInformation("Scan complete. No new music found.");
                _state.SetCurrentLogMsg(new AppLogModel { Log = "Your library is up-to-date." });
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No new files.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };

            }

            _logger.LogInformation("Found {TotalFiles} new audio files to process.", totalFilesToProcess);
            _state.SetCurrentLogMsg(new AppLogModel { Log = $"Found {totalFilesToProcess} new songs. Starting import..." });

            // --- 4. Process ONLY the new files ---
            var audioFileProcessor = new AudioFileProcessor( currentScanMetadataService, _config);
            int progress = 0;
            var processedResults =  await audioFileProcessor.ProcessFilesInParallelForEachAsync(newFilesToProcess);


            foreach (var result in processedResults)
            {
                progress++;
                if (progress % 50 == 0 || progress == processedResults.Count)
                {
                    _state.SetCurrentLogMsg(new AppLogModel
                    {
                        Log = $"Processed {progress}/{processedResults.Count}...",
                        AppScanLogModel = new AppScanLogModel
                        {
                            TotalFiles = processedResults.Count,
                            CurrentFilePosition = progress
                        }
                    });
                }
            }


            _logger.LogInformation("File processing complete. Consolidating metadata changes.");

            var newArtists = currentScanMetadataService.NewArtists;
            var newAlbums = currentScanMetadataService.NewAlbums;
            var newGenres = currentScanMetadataService.NewGenres;
            var newSongs = processedResults.Where(r => r.Success && r.ProcessedSong != null).Select(r => r.ProcessedSong).ToList();

            if (newSongs.Count!=0)
            {
                _logger.LogInformation("Found {SongCount} new songs, {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres to persist.",
                    newSongs.Count, newArtists.Count, newAlbums.Count, newGenres.Count);
                _logger.LogInformation("Persisting metadata changes to database...");

                // STEP 1: Map all new VIEW objects to new MODEL objects OUTSIDE the transaction.
                var artistModelsToUpsert = newArtists.Select(x => x.ToArtistModel());
                var albumModelsToUpsert = newAlbums.Select(x => x.ToAlbumModel());
                var genreModelsToUpsert = newGenres.Select(x => x.ToGenreModel());
                var songModelsToUpsert = newSongs.Select(x => x.ToSongModel());

                // This is a dictionary to easily find the MODEL version of a song later.
                var songModelDict = songModelsToUpsert.ToDictionary(s => s.Id);

                using (var realmInserts = _realmFactory.GetRealmInstance())
                {
                    await realmInserts.WriteAsync(() =>
                    {
                        // STEP 1: Add all new parent entities to the Realm.
                        // After this, they become MANAGED objects.
                        foreach (var artistView in newArtists)
                            realmInserts.Add(artistView.ToArtistModel());

                        foreach (var albumView in newAlbums)
                        {
                            if(albumView.Artists==null || albumView.Artists.Count==0)
                            {
                                _logger.LogWarning("Album {AlbumName} has no associated artists. Skipping.", albumView.Name);
                                continue;
                            }
                            var album = albumView.ToAlbumModel();

                            // Link album to its artist(s)
                            foreach (var artView in albumView.Artists)
                            {
                                var managedArtist = realmInserts.Find<ArtistModel>(artView.Id);
                                if (managedArtist != null)
                                    album.Artists.Add(managedArtist);
                            }

                            realmInserts.Add(album, update: true);
                        }
                        foreach (var genreView in newGenres)
                            realmInserts.Add(genreView.ToGenreModel());

                        foreach (var chunk in newSongs.Chunk(500))
                        {
                            foreach (var newSongView in chunk)
                            {
                                // Create an UNMANAGED model instance.
                                var songToPersist = newSongView.ToSongModel();

                                // Find the now-MANAGED versions of its relationships.
                                if (newSongView?.Album?.Id != null)
                                {
                                    var alb = realmInserts.Find<AlbumModel>(newSongView.Album.Id);
                                    if (alb is not null)
                                    {
                                        songToPersist.Album = alb;
                                    }
                                }
                                if (newSongView?.Genre?.Id != null)
                                {
                                    var gnr = realmInserts.Find<GenreModel>(newSongView.Genre.Id);
                                    if (gnr is not null)
                                    {
                                        songToPersist.Genre = gnr;
                                    }
                                }
                                if (newSongView?.ArtistToSong != null && newSongView.ArtistToSong.Count > 0)
                                {
                                    foreach (var artistView in newSongView.ArtistToSong)
                                    {
                                        if (artistView is not null)
                                        {

                                            var managedArtist = realmInserts.Find<ArtistModel>(artistView.Id);
                                            if (managedArtist != null)
                                                songToPersist.ArtistToSong.Add(managedArtist);
                                        }
                                    }
                                    songToPersist.Artist = songToPersist.ArtistToSong[0];
                                }
                                songToPersist.ArtistName = songToPersist.Artist?.Name;
                                songToPersist.IsNew = false;
                                songToPersist.DeviceModel = DeviceInfo.Current.Model.ToString();
                                songToPersist.DeviceManufacturer = DeviceInfo.Current.Platform.ToString();
                                songToPersist.DeviceVersion = DeviceInfo.Current.VersionString;
                                songToPersist.DeviceFormFactor = DeviceInfo.Current.Idiom.ToString();
                                // Finally, upsert the fully-linked song model.
                                realmInserts.Add(songToPersist, update: true);
                            }
                        }
                    });
                }

                _logger.LogInformation("Metadata changes persisted.");
            }
            else
            {
                _logger.LogInformation("No new music data changes to persist after scan.");
            }

            // --- Update App State (Your code is good here, but needs a fresh Realm instance) ---
            using (var realmAppState = _realmFactory.GetRealmInstance())
            {
                await realmAppState.WriteAsync(() =>
                {
                    var appState = realmAppState.All<AppStateModel>().FirstOrDefault();
                    if (appState != null)
                    {
                        var distinctFolders = appState.UserMusicFoldersPreference.Union(folderPaths).Distinct().ToList();
                        appState.UserMusicFoldersPreference.Clear();
                        foreach (var folder in distinctFolders)
                        {
                            appState.UserMusicFoldersPreference.Add(folder);
                        }
                    }
                });
            }

            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, extParam: newSongs,null, null));


            // clear up and clean memory 
            currentScanMetadataService.ClearAll();
            //audioFileProcessor.Cleanup();
            
            _state.SetCurrentLogMsg(new AppLogModel { Log = "Music scan complete." });
            _logger.LogInformation("Library scan completed successfully.");


            return new LoadSongsResult
            {
                
                NewSongsAddedCount = newSongs.Count,
                NewSongsAdded = newSongs,
                Albums = newAlbums,
                Artists = newArtists,
                Genres = newGenres,
                ProcessingResults = processedResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An unhandled exception occurred during ScanLibrary.{ex.Message}");
            return new LoadSongsResult
            {
                IsError = true,
                ErrorMessage = ex.Message,
                NewSongsAddedCount = 0
            };
        }
    }
  

    public async Task ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));

        await ScanLibrary(pathsToScan);
    }
    public void RemoveDupesFromDB()
    {
        
    }
}