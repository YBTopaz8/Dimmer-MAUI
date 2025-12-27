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
                    existingArtists.Count, existingAlbums.Count, existingGenres.Count, existingSongs.Count);

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

            var newArtists = currentScanMetadataService.NewArtists.DistinctBy(x=>x.Name).ToList();
            var newAlbums = currentScanMetadataService.NewAlbums.DistinctBy(x => x.Name).ToList();
            var newGenres = currentScanMetadataService.NewGenres.DistinctBy(x => x.Name).ToList();
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
                    if (realmInserts is not null)
                    {
                        await realmInserts.WriteAsync(() =>
                        {
                            // --- Step 1: Managed Artist Lookup ---
                            var managedArtists = new Dictionary<ObjectId, ArtistModel>();
                            foreach (var artView in newArtists)
                            {
                                var model = artView.ToArtistModel();
                                // realmInserts.Add returns the MANAGED version of the object
                                var managed = realmInserts.Add(model, update: true);
                                managedArtists[artView.Id] = managed;
                            }

                            // --- Step 2: Managed Genre Lookup ---
                            var managedGenres = new Dictionary<ObjectId, GenreModel>();
                            foreach (var gnrView in newGenres)
                            {
                                var managed = realmInserts.Add(gnrView.ToGenreModel(), update: true);
                                managedGenres[gnrView.Id] = managed;
                            }

                            // --- Step 3: Upsert Albums and link to Managed Artists ---
                            var managedAlbums = new Dictionary<ObjectId, AlbumModel>();
                            foreach (var albView in newAlbums)
                            {
                                var albumModel = albView.ToAlbumModel();

                                if (albView.Artists != null)
                                {
                                    foreach (var artView in albView.Artists)
                                    {
                                        if (managedArtists.TryGetValue(artView.Id, out var managedArt))
                                        {
                                            if (!albumModel.Artists.Contains(managedArt))
                                                albumModel.Artists.Add(managedArt);
                                        }
                                    }
                                }
                                var managedAlbum = realmInserts.Add(albumModel, update: true);
                                managedAlbums[albView.Id] = managedAlbum;
                            }

                            // --- Step 4: Upsert Songs and link everything ---
                            foreach (var songView in newSongs)
                            {
                                var songModel = songView.ToSongModel();
                                songModel.Artist = songView.Artist.ToArtistModel();
                                
                                // LINK ALBUM (Using our dictionary)
                                if (songView.Album != null && managedAlbums.TryGetValue(songView.Album.Id, out var mAlb))
                                {
                                    songModel.Album = mAlb;
                                }

                                // LINK GENRE (Using our dictionary)
                                if (songView.Genre != null && managedGenres.TryGetValue(songView.Genre.Id, out var mGnr))
                                {
                                    songModel.Genre = mGnr;
                                }

                                // LINK ARTISTS
                                if (songView.ArtistToSong != null)
                                {
                                    foreach (var artView in songView.ArtistToSong)
                                    {
                                        if (managedArtists.TryGetValue(artView.Id, out var mArt))
                                        {
                                            songModel.ArtistToSong.Add(mArt);
                                        }
                                    }

                                    if (songModel.ArtistToSong.Count > 0)
                                        songModel.Artist = songModel.ArtistToSong[0];
                                }
                                songModel.Album = songView.Album.ToAlbumModel();
                                songModel.Genre = songView.Genre.ToGenreModel();
                                songModel.ArtistName = songModel.Artist?.Name ?? "Unknown Artist";
                                songModel.IsNew = false;

                                // Finally, add the song
                                realmInserts.Add(songModel, update: true);
                            }
                        });
                    }
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

            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, extParam: null,null, null));


            // clear up and clean memory 
            currentScanMetadataService.ClearAll();
            
            
            _state.SetCurrentLogMsg(new AppLogModel { Log = "Music scan complete." });
            _logger.LogInformation("Library scan completed successfully.");
            newSongs.Clear();
            newArtists.Clear();
            newAlbums.Clear();
            newGenres.Clear();
            processedResults.Clear();


            return new LoadSongsResult
            {
                
                NewSongsAddedCount = newSongs.Count,
                AlbumsCount = newAlbums.Count,
                ArtistsCount = newArtists.Count,
                GenresCount = newGenres.Count,
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