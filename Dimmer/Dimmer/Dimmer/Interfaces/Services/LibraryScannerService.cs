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

            // FIX #1: Create unmanaged copies to prevent cross-thread exceptions.
            var unmanagedArtists = existingArtists.ConvertAll(a => new ArtistModelView { Id = a.Id, Name = a.Name });
            var unmanagedAlbums = existingAlbums.ConvertAll(a => new AlbumModelView { Id = a.Id, Name = a.Name, ImagePath = a.ImagePath });
            var unmanagedGenres = existingGenres.ConvertAll(g => new GenreModelView { Id = g.Id, Name = g.Name });
            var unmanagedSongs = existingSongs.ConvertAll(s => new SongModelView { Title = s.Title, DurationInSeconds = s.DurationInSeconds });

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
                        Log = $"Processed: {fileProcessingResult.ProcessedSong.Title} by {Environment.NewLine} {fileProcessingResult.ProcessedSong.OtherArtistsName} " +
                        $"" +
                        $"({processedFileCount}/{totalFiles})",
                        AppScanLogModel = new AppScanLogModel { TotalFiles = totalFiles, CurrentFilePosition = processedFileCount },
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

            _logger.LogInformation("Found {SongCount} new songs, {ArtistCount} new artists, {AlbumCount} new albums, {GenreCount} new genres to persist.",
                newOrUpdatedSongs.Count, newOrUpdatedArtists.Count, newOrUpdatedAlbums.Count, newOrUpdatedGenres.Count);

            if (newOrUpdatedSongs.Any())
            {
                _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, newOrUpdatedSongs, newOrUpdatedSongs[0].ToModelView(_mapper), newOrUpdatedSongs[0]));
            }

            if (newOrUpdatedArtists.Any() || newOrUpdatedAlbums.Any() || newOrUpdatedGenres.Any() || newOrUpdatedSongs.Any())
            {
                _logger.LogInformation("Persisting metadata changes to database...");

                await realm.WriteAsync(() =>
                {
                    foreach (var artist in newOrUpdatedArtists)
                        realm.Add(artist, update: true);
                    foreach (var album in newOrUpdatedAlbums)
                        realm.Add(album, update: true);
                    foreach (var genre in newOrUpdatedGenres)
                        realm.Add(genre, update: true);
                    foreach (var song in newOrUpdatedSongs)
                        realm.Add(song, update: true);
                }).ConfigureAwait(false);

                _logger.LogInformation("Metadata changes persisted.");

                await realm.WriteAsync(() =>
                {
                    var curState = realm.All<AppStateModel>().First();
                    var distinctFolders = curState.UserMusicFoldersPreference.Union(folderPaths).Distinct();
                    curState.UserMusicFoldersPreference.Clear();
                    foreach (var item in distinctFolders)
                    {
                        curState.UserMusicFoldersPreference.Add(item);
                    }
                });
            }
            else
            {
                _logger.LogInformation("No new or modified entities to save to database.");
            }

            var finalSongListFromDb = _songRepo.GetAll(false).ToList();
            _state.LoadAllSongs(finalSongListFromDb.AsReadOnly());

            _logger.LogInformation("Global state updated with {SongCount} songs from database after scan.", finalSongListFromDb.Count);
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, folderPaths, null, finalSongListFromDb.FirstOrDefault()));

            return new LoadSongsResult
            {
                Artists = newOrUpdatedArtists.ToList(),
                Albums = newOrUpdatedAlbums.ToList(),
                Songs = newOrUpdatedSongs.ToList(),
                Genres = newOrUpdatedGenres.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during the library scan: {Message}", ex.Message);
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