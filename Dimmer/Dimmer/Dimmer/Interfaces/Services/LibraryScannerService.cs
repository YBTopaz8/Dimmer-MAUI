using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Extensions;


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

    public async Task<LoadSongsResult?> ScanLibrary(List<string>? folderPaths)
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
            AudioFileProcessor audioFileProcessor = new AudioFileProcessor(_coverArtService, currentScanMetadataService, _config);

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

            _logger.LogDebug("Loading existing metadata from database...");
            var existingArtists = _mapper.Map<List<ArtistModelView>>(realm.All<ArtistModel>().ToList());
            var existingAlbums = _mapper.Map<List<AlbumModelView>>(realm.All<AlbumModel>().ToList());
            var existingGenres = _mapper.Map<List<GenreModelView>>(realm.All<GenreModel>().ToList());
            var existingSongs = _mapper.Map<List<SongModelView>>(realm.All<SongModel>().ToList());
            _logger.LogDebug("Loaded {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres, {SongCount} songs.",
                existingArtists.Count, existingAlbums.Count, existingGenres.Count, existingSongs.Count);

            currentScanMetadataService.LoadExistingData(unmanagedArtists, unmanagedAlbums, unmanagedGenres, unmanagedSongs);

            int totalFiles = allFiles.Count;
            int processedFileCount = 0;
            _logger.LogInformation("Processing {TotalFiles} audio files...", totalFiles);

            foreach (string file in allFiles)
            {
                processedFileCount++;
                if (processedFileCount % 50 == 0 || processedFileCount == totalFiles)
                {
                    _logger.LogInformation("Scanning progress: {ProcessedCount}/{TotalCount} files.", processedFileCount, totalFiles);
                }

                var fileProcessingResult = await audioFileProcessor.ProcessFile(file);

                if (fileProcessingResult.Success && fileProcessingResult.ProcessedSong != null)
                {
                    _state.SetCurrentLogMsg(new AppLogModel
                    {
                        Log = $"Processed: {fileProcessingResult.ProcessedSong.Title} ({processedFileCount}/{totalFiles})",
                        AppScanLogModel = new AppScanLogModel() { TotalFiles = totalFiles, CurrentFilePosition = processedFileCount, },
                        TotalScanFiles = totalFiles,
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

            IReadOnlyList<SongModel> newOrUpdatedSongs = _mapper.Map<IReadOnlyList<SongModel>>(currentScanMetadataService.GetAllSongs());
            IReadOnlyList<ArtistModel> newOrUpdatedArtists = _mapper.Map<IReadOnlyList<ArtistModel>>(currentScanMetadataService.GetAllArtists());
            IReadOnlyList<AlbumModel> newOrUpdatedAlbums = _mapper.Map<IReadOnlyList<AlbumModel>>(currentScanMetadataService.GetAllAlbums());
            IReadOnlyList<GenreModel> newOrUpdatedGenres = _mapper.Map<IReadOnlyList<GenreModel>>(currentScanMetadataService.GetAllGenres());

            IReadOnlyList<SongModelView> newOrUpdatedSongs = currentScanMetadataService.GetAllSongs().Where(s => s.IsNew).ToList();
            IReadOnlyList<ArtistModelView> newOrUpdatedArtists = currentScanMetadataService.GetAllArtists().Where(a => a.IsNew).ToList();
            IReadOnlyList<AlbumModelView> newOrUpdatedAlbums = currentScanMetadataService.GetAllAlbums().Where(a => a.IsNew).ToList();
            IReadOnlyList<GenreModelView> newOrUpdatedGenres = currentScanMetadataService.GetAllGenres().Where(g => g.IsNew).ToList();

            _logger.LogInformation("Found {SongCount} new/updated songs, {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres to persist.",
                newOrUpdatedSongs.Count, newOrUpdatedArtists.Count, newOrUpdatedAlbums.Count, newOrUpdatedGenres.Count);

            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, newOrUpdatedSongs, newOrUpdatedSongs[0], null));


            if (newOrUpdatedArtists.Any() || newOrUpdatedAlbums.Any() || newOrUpdatedGenres.Any() || newOrUpdatedSongs.Any())
            {
                _logger.LogInformation("Persisting metadata changes to database...");

                // STEP 1: Map all new VIEW objects to new MODEL objects OUTSIDE the transaction.
                var artistModelsToUpsert = _mapper.Map<List<ArtistModel>>(newOrUpdatedArtists);
                var albumModelsToUpsert = _mapper.Map<List<AlbumModel>>(newOrUpdatedAlbums);
                var genreModelsToUpsert = _mapper.Map<List<GenreModel>>(newOrUpdatedGenres);
                var songModelsToUpsert = _mapper.Map<List<SongModel>>(newOrUpdatedSongs);

                // This is a dictionary to easily find the MODEL version of a song later.
                var songModelDict = songModelsToUpsert.ToDictionary(s => s.Id);

                using (var realmb = _realmFactory.GetRealmInstance())
                {
                    await realmb.WriteAsync(() =>
                    {
                        // STEP 2: Upsert all "parent" entities first (Artists, Albums, Genres).
                        // This is simple and clean. 'Add' with 'update:true' is an upsert.
                        foreach (var artist in artistModelsToUpsert)
                            realmb.Add(artist, update: true);
                        foreach (var album in albumModelsToUpsert)
                            realmb.Add(album, update: true);
                        foreach (var genre in genreModelsToUpsert)
                            realmb.Add(genre, update: true);

                        // STEP 3: Process the songs and link their relationships.
                        foreach (var incomingSongView in newOrUpdatedSongs)
                        {
                            // Get the corresponding SongModel we created earlier.
                            if (!songModelDict.TryGetValue(incomingSongView.Id, out var songToPersist))
                            {
                                continue; // Should not happen, but a good safeguard.
                            }

                            // We are now working with a pure, unmanaged SongModel.
                            // Now, find the MANAGED versions of its relationships within THIS realmb.

                            // Link Album
                            if (incomingSongView.Album?.Id != null)
                            {
                                songToPersist.Album = realmb.Find<AlbumModel>(incomingSongView.Album.Id);
                            }

                            // Link Genre
                            if (incomingSongView.Genre?.Id != null)
                            {
                                songToPersist.Genre = realmb.Find<GenreModel>(incomingSongView.Genre.Id);
                            }

                            // Link Artists
                            songToPersist.ArtistToSong.Clear();
                            if (incomingSongView.ArtistIds != null)
                            {
                                foreach (var artistView in incomingSongView.ArtistIds)
                                {
                                    var managedArtist = realmb.Find<ArtistModel>(artistView.Id);
                                    if (managedArtist != null)
                                    {
                                        songToPersist.ArtistToSong.Add(managedArtist);
                                    }
                                }
                            }

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
                _songRepo.Delete(songsToDelete);
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
    public Task<LoadSongsResult>? ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return Task.Run(() => ScanLibrary(pathsToScan));
    }
}