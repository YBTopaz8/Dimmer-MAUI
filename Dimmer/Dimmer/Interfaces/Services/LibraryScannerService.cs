using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;


namespace Dimmer.Interfaces.Services;

public class LibraryScannerService : ILibraryScannerService
{
    private readonly IRepository<DimmerPlayEvent> _playEventsRepo;
    private readonly ISettingsService settingsService;
    private readonly IDimmerStateService _state;
    private readonly IMapper _mapper;
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
        IDimmerStateService state, IMapper mapper, IRepository<AppStateModel> appStateRepo,
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
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _songRepo = songRepo ?? throw new ArgumentNullException(nameof(songRepo));
        _albumRepo = albumRepo ?? throw new ArgumentNullException(nameof(albumRepo));
        _artistRepo = artistRepo ?? throw new ArgumentNullException(nameof(artistRepo));
        _genreRepo = genreRepo ?? throw new ArgumentNullException(nameof(genreRepo));
        _realmFactory = realmFactory ?? throw new ArgumentNullException(nameof(realmFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? new ProcessingConfig();

        _coverArtService = new CoverArtService(_config);
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
                return default!;
            }

            _logger.LogInformation("Starting library scan for paths: {Paths}", string.Join(", ", folderPaths));
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg(new AppLogModel { Log = "Starting music scan..." });

            MusicMetadataService currentScanMetadataService = new();

            List<string> allFiles = TaggingUtils.GetAllAudioFilesFromPaths(folderPaths, _config.SupportedAudioExtensions);

            if (allFiles.Count == 0)
            {
                _state.SetCurrentLogMsg(new AppLogModel { Log = "No audio files found in the selected paths." });
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No audio files found.", null, null));
                return default!;
            }

            _logger.LogDebug("Loading existing metadata from database and detaching for processing...");
            var existingArtists = _mapper.Map<List<ArtistModelView>>(realm.All<ArtistModel>().ToList());
            var existingAlbums = _mapper.Map<List<AlbumModelView>>(realm.All<AlbumModel>().ToList());
            var existingGenres = _mapper.Map<List<GenreModelView>>(realm.All<GenreModel>().ToList());
            var existingSongs = _mapper.Map<List<SongModelView>>(realm.All<SongModel>().ToList());



            _logger.LogDebug("Loaded {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres, {SongCount} songs.",
                existingArtists.Count, existingAlbums.Count, existingGenres.Count, existingSongs.Count);





            currentScanMetadataService.LoadExistingData(existingArtists, existingAlbums, existingGenres, existingSongs);

            var newFilesToProcess = allFiles.Where(file => !currentScanMetadataService.HasFileBeenProcessed(file)).ToList();

            int totalFilesToProcess = newFilesToProcess.Count;
            if (totalFilesToProcess == 0)
            {
                _logger.LogInformation("Scan complete. No new music found.");
                _state.SetCurrentLogMsg(new AppLogModel { Log = "Your library is up-to-date." });
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No new files.", null, null));

                return default!;
            }

            _logger.LogInformation("Found {TotalFiles} new audio files to process.", totalFilesToProcess);
            _state.SetCurrentLogMsg(new AppLogModel { Log = $"Found {totalFilesToProcess} new songs. Starting import..." });

            // --- 4. Process ONLY the new files ---
            var audioFileProcessor = new AudioFileProcessor(_coverArtService, currentScanMetadataService, _config);
            var processedResults = new List<FileProcessingResult>();
            int processedFileCount = 0;

            foreach (string file in newFilesToProcess)
            {
                processedFileCount++;
                var fileProcessingResult = audioFileProcessor.ProcessFile(file); // Use ProcessFile directly
                processedResults.Add(fileProcessingResult);

                if (fileProcessingResult.Success && fileProcessingResult.ProcessedSong != null)
                {
                    _state.SetCurrentLogMsg(new AppLogModel
                    {
                        Log = $"Processed: {fileProcessingResult.ProcessedSong.Title} ({processedFileCount}/{newFilesToProcess.Count})",
                        AppScanLogModel = new AppScanLogModel() { TotalFiles = newFilesToProcess.Count, CurrentFilePosition = processedFileCount, },
                        TotalScanFiles = newFilesToProcess.Count,
                        CurrentScanFile = processedFileCount,
                        ViewSongModel = _mapper.Map<SongModelView>(fileProcessingResult.ProcessedSong)
                    });
                }
                else if (fileProcessingResult.Skipped)
                {
                    _logger.LogDebug("Skipped file {FileName}: {Reason}", Path.GetFileName(file), fileProcessingResult.SkipReason);
                }
                else
                {
                    string errors = string.Join("; ", fileProcessingResult.Errors);
                    _logger.LogWarning("Error processing file {FileName}: {Errors}", Path.GetFileName(file), errors);
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
                var artistModelsToUpsert = _mapper.Map<List<ArtistModel>>(newArtists);
                var albumModelsToUpsert = _mapper.Map<List<AlbumModel>>(newAlbums);
                var genreModelsToUpsert = _mapper.Map<List<GenreModel>>(newGenres);
                var songModelsToUpsert = _mapper.Map<List<SongModel>>(newSongs);

                // This is a dictionary to easily find the MODEL version of a song later.
                var songModelDict = songModelsToUpsert.ToDictionary(s => s.Id);

                using (var realmb = _realmFactory.GetRealmInstance())
                {
                    await realmb.WriteAsync(() =>
                    {
                        // STEP 1: Add all new parent entities to the Realm.
                        // After this, they become MANAGED objects.
                        foreach (var artistView in newArtists)
                            realmb.Add(_mapper.Map<ArtistModel>(artistView));
                        foreach (var albumView in newAlbums)
                            realmb.Add(_mapper.Map<AlbumModel>(albumView));
                        foreach (var genreView in newGenres)
                            realmb.Add(_mapper.Map<GenreModel>(genreView));

                        // STEP 2: Now process the new songs.
                        foreach (var newSongView in newSongs)
                        {
                            // Create an UNMANAGED model instance.
                            var songToPersist = _mapper.Map<SongModel>(newSongView);

                            // Find the now-MANAGED versions of its relationships.
                            if (newSongView.Album?.Id != null)
                            {
                                var alb = realmb.Find<AlbumModel>(newSongView.Album.Id);
                                if (alb is not null)
                                {
                                    songToPersist.Album=alb;
                                }
                            }
                            if (newSongView.Genre?.Id != null)
                            {
                                var gnr = realmb.Find<GenreModel>(newSongView.Genre.Id);
                                if (gnr is not null)
                                {
                                    songToPersist.Genre=gnr;
                                }
                            }
                            if (newSongView.ArtistToSong != null && newSongView.ArtistToSong.Count>0)
                            {
                                foreach (var artistView in newSongView.ArtistToSong)
                                {
                                    if (artistView is not null)
                                    {

                                        var managedArtist = realmb.Find<ArtistModel>(artistView.Id);
                                        if (managedArtist != null)
                                            songToPersist.ArtistToSong.Add(managedArtist);
                                    }
                                }
                                songToPersist.Artist = songToPersist.ArtistToSong[0];
                            }

                            songToPersist.IsNew=false;
                            songToPersist.DeviceModel=DeviceInfo.Current.Model.ToString();
                            songToPersist.DeviceManufacturer=DeviceInfo.Current.Platform.ToString();
                            songToPersist.DeviceVersion=DeviceInfo.Current.VersionString;
                            songToPersist.DeviceFormFactor=DeviceInfo.Current.Idiom.ToString();
                            // Finally, upsert the fully-linked song model.
                            realmb.Add(songToPersist, update: true);
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
            using (var realmm = _realmFactory.GetRealmInstance())
            {
                await realmm.WriteAsync(() =>
                {
                    var appState = realm.All<AppStateModel>().FirstOrDefault();
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

            realm.Dispose();
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, extParam: newSongs,null, null));


            // clear up and clean memory 
            currentScanMetadataService.ClearAll();
            //audioFileProcessor.Cleanup();
            GC.Collect();
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
            return null;
        }
    }
  

    public void RemoveDupesFromDB()
    {
        
    }
    public async Task<LoadSongsResult> ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return await ScanLibrary(pathsToScan);
    }
}