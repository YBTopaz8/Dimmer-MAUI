using Dimmer.Interfaces.Services.Interfaces;


namespace Dimmer.Interfaces.Services;

public class LibraryScannerService : ILibraryScannerService
{
    private readonly IRepository<DimmerPlayEvent> _playEventsRepo;
    private readonly IDimmerStateService _state;
    private readonly IMapper _mapper;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<GenreModel> _genreRepo;
    private readonly IRealmFactory _realmFactory;
    private readonly ILogger<LibraryScannerService> _logger;
    private readonly ProcessingConfig _config;
    private readonly ICoverArtService _coverArtService;

    public LibraryScannerService(
        IDimmerStateService state, IMapper mapper,
        IRepository<SongModel> songRepo, IRepository<AlbumModel> albumRepo,
        IRepository<ArtistModel> artistRepo, IRepository<GenreModel> genreRepo,
        IRealmFactory realmFactory, ILogger<LibraryScannerService> logger
        , IRepository<DimmerPlayEvent> playEventsRepo,
        ProcessingConfig? config = null)
    {
        _playEventsRepo=playEventsRepo;
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

    public async Task<LoadSongsResult?> ScanLibraryAsync(List<string> folderPaths)
    {
        if (folderPaths == null || folderPaths.Count==0)
        {
            _logger.LogWarning("ScanLibraryAsync called with no folder paths.");
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No folders selected for scanning." });
            return null;
        }

        _logger.LogInformation("Starting library scan for paths: {Paths}", string.Join(", ", folderPaths));

        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanStarted, string.Join(";", folderPaths), null, null));

        _state.SetCurrentLogMsg(new AppLogModel { Log = "Starting music scan..." });

        MusicMetadataService currentScanMetadataService = new();
        AudioFileProcessor audioFileProcessor = new AudioFileProcessor(
            _coverArtService,
            currentScanMetadataService,
            _config);

        List<string> allFiles = AudioFileUtils.GetAllAudioFilesFromPaths(folderPaths, _config.SupportedAudioExtensions);

        if (allFiles.Count == 0)
        {
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No audio files found in the selected paths." });
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, "No audio files found.", null, null));
            return null;
        }


        _logger.LogDebug("Loading existing metadata from database...");
        var existingArtists = _artistRepo.GetAll(false).ToList();
        var existingAlbums = _albumRepo.GetAll(false).ToList();
        var existingGenres = _genreRepo.GetAll(false).ToList();
        var existingSongs = _songRepo.GetAll(false).ToList();
        _logger.LogDebug("Loaded {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres, {SongCount} songs.",
            existingArtists.Count, existingAlbums.Count, existingGenres.Count, existingSongs.Count);

        currentScanMetadataService.LoadExistingData(existingArtists, existingAlbums, existingGenres, existingSongs);




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

            var fileProcessingResult = await audioFileProcessor.ProcessFileAsync(file).ConfigureAwait(false);

            if (fileProcessingResult.Success && fileProcessingResult.ProcessedSong != null)
            {
                _state.SetCurrentLogMsg(new AppLogModel
                {
                    Log = $"Processed: {fileProcessingResult.ProcessedSong.Title} ({processedFileCount}/{totalFiles})",
                    AppSongModel = fileProcessingResult.ProcessedSong,
                    AppScanLogModel = new AppScanLogModel() { TotalFiles = totalFiles, CurrentFilePosition = processedFileCount },
                    ViewSongModel = _mapper.Map<SongModelView>(fileProcessingResult.ProcessedSong)
                });
            }
            else if (fileProcessingResult.Skipped)
            {
                _state.SetCurrentLogMsg(new AppLogModel { Log = $"Skipped (File: {Path.GetFileName(file)}): {fileProcessingResult.SkipReason} ({processedFileCount}/{totalFiles})" });
                _logger.LogDebug("Skipped file {FileName}: {Reason}", Path.GetFileName(file), fileProcessingResult.SkipReason);
            }
            else
            {
                string errors = string.Join("; ", fileProcessingResult.Errors);
                _state.SetCurrentLogMsg(new AppLogModel { Log = $"Error processing {Path.GetFileName(file)}: {errors} ({processedFileCount}/{totalFiles})" });
                _logger.LogWarning("Error processing file {FileName}: {Errors}", Path.GetFileName(file), errors);
            }
        }
        _logger.LogInformation("File processing complete. Consolidating metadata changes.");


        IReadOnlyList<SongModel> newOrUpdatedSongs = [.. currentScanMetadataService.GetAllSongs().Where(s => s.IsNewOrModified)];
        IReadOnlyList<ArtistModel> newOrUpdatedArtists = [.. currentScanMetadataService.GetAllArtists().Where(a => a.IsNewOrModified)];
        IReadOnlyList<AlbumModel> newOrUpdatedAlbums = [.. currentScanMetadataService.GetAllAlbums().Where(a => a.IsNewOrModified)];
        IReadOnlyList<GenreModel> newOrUpdatedGenres = [.. currentScanMetadataService.GetAllGenres().Where(g => g.IsNewOrModified)];

        _logger.LogInformation("Found {SongCount} new/updated songs, {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres to persist.",
            newOrUpdatedSongs.Count, newOrUpdatedArtists.Count, newOrUpdatedAlbums.Count, newOrUpdatedGenres.Count);


        if (!newOrUpdatedSongs.Any() && !newOrUpdatedArtists.Any() && !newOrUpdatedAlbums.Any() && !newOrUpdatedGenres.Any())
        {
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No new music data changes to persist after scan." });
            _logger.LogInformation("No new or modified entities to save to database.");



        }

        if (newOrUpdatedArtists.Any() || newOrUpdatedAlbums.Any() || newOrUpdatedGenres.Any() || newOrUpdatedSongs.Any())
        {
            _logger.LogInformation("Persisting metadata changes to database...");
            using (var realm = _realmFactory.GetRealmInstance())
            {
                await realm.WriteAsync(() =>
                {
                    if (newOrUpdatedAlbums.Any())
                        foreach (var album in newOrUpdatedAlbums)
                            realm.Add(album, update: true);
                    if (newOrUpdatedArtists.Any())
                        foreach (var artist in newOrUpdatedArtists)
                            realm.Add(artist, update: true);
                    if (newOrUpdatedGenres.Any())
                        foreach (var genre in newOrUpdatedGenres)
                            realm.Add(genre, update: true);

                    if (newOrUpdatedSongs.Any())
                    {
                        foreach (var song in newOrUpdatedSongs)
                        {

                            if (song.Album != null)
                            {


                                if (!song.Album.IsManaged || song.Album.IsManaged && song.Album.Realm != realm)
                                {
                                    var managedAlbum = realm.Find<AlbumModel>(song.Album.Id);
                                    if (managedAlbum != null)
                                        song.Album = managedAlbum;




                                }
                            }

                            if (song.Genre != null)
                            {
                                if (!song.Genre.IsManaged || song.Genre.IsManaged && song.Genre.Realm != realm)
                                {
                                    var managedGenre = realm.Find<GenreModel>(song.Genre.Id);
                                    if (managedGenre != null)
                                        song.Genre = managedGenre;
                                }
                            }

                            if (song.ArtistIds != null && song.ArtistIds.Any())
                            {
                                var updatedArtistListForSong = new List<ArtistModel>();
                                foreach (var artistRef in song.ArtistIds)
                                {
                                    if (!artistRef.IsManaged || artistRef.IsManaged && artistRef.Realm != realm)
                                    {
                                        var managedArtist = realm.Find<ArtistModel>(artistRef.Id);
                                        if (managedArtist != null)
                                            updatedArtistListForSong.Add(managedArtist);

                                    }
                                    else
                                    {
                                        updatedArtistListForSong.Add(artistRef);
                                    }
                                }



                                if (!song.IsManaged)
                                {
                                    song.ArtistIds.Clear();
                                    foreach (var art in updatedArtistListForSong)
                                        song.ArtistIds.Add(art);
                                }
                                else
                                {




                                }
                            }
                            realm.Add(song, update: true);
                        }
                    }
                }).ConfigureAwait(false);
                _logger.LogInformation("Metadata changes persisted.");
            }
        }



        var finalSongListFromDb = _songRepo.GetAll(false).ToList();
        _state.LoadAllSongs(finalSongListFromDb.AsReadOnly());
        _logger.LogInformation("Global state updated with {SongCount} songs from database after scan.", finalSongListFromDb.Count);
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderScanCompleted, null, null, null));




        return new LoadSongsResult
        {
            Artists = [.. newOrUpdatedArtists],
            Albums = [.. newOrUpdatedAlbums],
            Songs = [.. newOrUpdatedSongs],
            Genres = [.. newOrUpdatedGenres]
        };
    }

    public void LoadInSongsAndEvents()
    {
        _logger.LogInformation("Loading all songs from database into global state.");
        var allSongs = _songRepo.GetAll(true).ToList().DistinctBy(x => x.Title).ToList();
        _state.LoadAllSongs(allSongs.AsReadOnly());
        _logger.LogInformation("Loaded {SongCount} songs into global state.", allSongs.Count);

        var allEvents = _playEventsRepo.GetAll();
        _state.LoadAllPlayHistory(allEvents);
    }
    public async Task<LoadSongsResult?> ScanSpecificPathsAsync(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return await ScanLibraryAsync(pathsToScan).ConfigureAwait(false);
    }
}