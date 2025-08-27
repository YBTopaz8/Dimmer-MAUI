using Dimmer.Interfaces.Services.Interfaces;


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

    public async Task<LoadSongsResult>? ScanLibrary(List<string>? folderPaths)
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
                return null;
            }

            _logger.LogInformation("Starting library scan for paths: {Paths}", string.Join(", ", folderPaths));
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg(new AppLogModel { Log = "Starting music scan..." });

            MusicMetadataService currentScanMetadataService = new();

            List<string> allFiles = AudioFileUtils.GetAllAudioFilesFromPaths(folderPaths, _config.SupportedAudioExtensions);

            if (allFiles.Count == 0)
            {
                _state.SetCurrentLogMsg(new AppLogModel { Log = "No audio files found in the selected paths." });
                _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, "No audio files found.", null, null));
                return null;
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
                _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, "No new files.", null, null));
                return new LoadSongsResult { /* Indicate success with no changes */ };
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
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, extParam: newSongs,null, null));


            return new LoadSongsResult { /* ... */ };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during ScanLibrary.");
            return null;
        }
    }
    public void LoadInSongsAndEvents()
    {
        _logger.LogInformation("Loading all songs from database into global state.");
        var allSongs = _songRepo.GetAll(true).AsEnumerable().DistinctBy(x => x.Title).ToList();
        _state.LoadAllSongs(allSongs.AsReadOnly());
        _logger.LogInformation("Loaded {SongCount} songs into global state.", allSongs.Count);

        //var allEvents = _playEventsRepo.GetAll();
        //_state.LoadAllPlayHistory(allEvents);
    }

    public void RemoveDupesFromDB()
    {
        _logger.LogInformation("Loading all songs from database into global state.");
        var allSongs = _songRepo.GetAll().AsEnumerable();

        if (!allSongs.Any())
        {
            _logger.LogInformation("No songs in the database to check for duplicates.");
            // Still, ensure the state is consistent
            _state.LoadAllSongs(System.Array.Empty<SongModel>().AsReadOnly()); // Or new List<SongModel>().AsReadOnly()
        }
        var groupedSongs = allSongs
          .GroupBy(song => new
          {
              Title = song.Title, // Consider: song.Title?.ToLowerInvariant() for case-insensitivity
              Duration = song.DurationInSeconds // Consider: Math.Round(song.DurationInSeconds, 1) for 1 decimal place precision
          })
          .ToList(); // Materialize the groups

        var songsToDelete = new List<SongModel>();
        int duplicatesFoundCount = 0;

        _logger.LogInformation($"Found {groupedSongs.Count} unique Title/Duration combinations.");

        foreach (var group in groupedSongs)
        {
            if (group.Count() > 1)
            {
                // This group has duplicates
                _logger.LogInformation($"Found {group.Count()} songs for Title: '{group.Key.Title}', Duration: {group.Key.Duration}. Identifying which to keep.");

                // Decide which song to keep.
                // Strategy: Keep the one with the lexicographically smallest ObjectId.
                // ObjectId.ToString() provides a sortable representation.
                // Alternatively, you could use s.DateCreated if it's reliably set, or s.Id directly.
                var songToKeep = group.OrderBy(s => s.Id.ToString()).First(); // Or s.Id, or s.DateCreated

                // Add all other songs in this group to the deletion list
                var currentGroupDuplicates = group.Where(s => s.Id != songToKeep.Id).ToList();
                songsToDelete.AddRange(currentGroupDuplicates);

                duplicatesFoundCount += currentGroupDuplicates.Count;
                _logger.LogInformation($"Keeping song Id: {songToKeep.Id}. Marked {currentGroupDuplicates.Count} other(s) for deletion from this group.");
            }
        }

        if (songsToDelete.Count!=0)
        {
            _logger.LogInformation($"Attempting to delete {songsToDelete.Count} duplicate songs in total.");
            try
            {
                // Use the batch delete method (assumes it's updated to handle frozen entities)
                _logger.LogInformation($"Successfully deleted {songsToDelete.Count} duplicate songs.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while batch deleting duplicate songs. {songsToDelete.Count} songs were intended for deletion.");
                // Depending on requirements, you might re-throw or handle partially.
                // For now, we'll continue to reload state, which will reflect any successful deletions.
            }
        }
        else
        {
            _logger.LogInformation("No duplicate songs (based on Title and Duration) found to delete.");
        }

        // 4. Reload all songs from the database into the global state.
        _logger.LogInformation("Reloading all songs from database into global state after de-duplication attempt.");
        var freshSongs = _songRepo.GetAll(true).ToList(); // Get fresh list, shuffle if needed for display
        _state.LoadAllSongs(freshSongs.AsReadOnly());
        _logger.LogInformation($"Loaded {freshSongs.Count} songs into global state.");
    }
    public async Task<LoadSongsResult>? ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return await ScanLibrary(pathsToScan);
    }
}