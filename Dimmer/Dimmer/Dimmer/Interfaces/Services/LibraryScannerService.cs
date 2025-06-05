using System.Diagnostics;

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
        ISettingsService settingsService,
        ProcessingConfig? config = null)
    {
        _playEventsRepo=playEventsRepo;
        this.settingsService=settingsService;
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

    public async Task<LoadSongsResult?> ScanLibraryAsync(List<string>? folderPaths)
    {
        if (folderPaths == null || folderPaths.Count==0)
        {
            _logger.LogWarning("ScanLibraryAsync called with no folder paths.");
            _state.SetCurrentLogMsg(new AppLogModel { Log = "No folders selected for scanning." });

            folderPaths = settingsService.UserMusicFoldersPreference.ToList();

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
        //List<SongModel> newOrUpdatedSongs = new();
        //List<ArtistModel> newOrUpdatedArtists = new();
        //List<AlbumModel> newOrUpdatedAlbums = new();
        //List<GenreModel> newOrUpdatedGenres = new();

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


        IReadOnlyList<SongModel> newOrUpdatedSongs = currentScanMetadataService.GetAllSongs().Where(s => s.IsNew).ToList();
        IReadOnlyList<ArtistModel> newOrUpdatedArtists = currentScanMetadataService.GetAllArtists().Where(a => a.IsNew).ToList();
        IReadOnlyList<AlbumModel> newOrUpdatedAlbums = currentScanMetadataService.GetAllAlbums().Where(a => a.IsNew).ToList();
        IReadOnlyList<GenreModel> newOrUpdatedGenres = currentScanMetadataService.GetAllGenres().Where(g => g.IsNew).ToList();

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
                    {
                        foreach (var album in newOrUpdatedAlbums)
                        {
                            if (!album.IsManaged)
                            {
                                realm.Add(album, update: true);

                            }
                            else if (album.IsManaged)
                            {
                                var mged = realm.Find<AlbumModel>(album.Id);
                                if (mged != null)
                                {
                                    realm.Add(mged, update: true);
                                }
                                else
                                {
                                    Debug.WriteLine("skipped...album in newOrUpdatedAlbums");
                                }

                            }
                        }
                    }

                    if (newOrUpdatedArtists.Any())
                    {
                        foreach (var artist in newOrUpdatedArtists)
                        {
                            if (!artist.IsManaged)
                            {
                                realm.Add(artist, update: true);

                            }
                            else if (artist.IsManaged)
                            {
                                var mged = realm.Find<ArtistModel>(artist.Id);
                                if (mged != null)
                                {
                                    realm.Add(mged, update: true);
                                }
                                else
                                {
                                    Debug.WriteLine("skipped...album in newOrUpdatedAlbums");
                                }

                            }


                        }
                    }

                    if (newOrUpdatedGenres.Any())
                    {
                        foreach (var genre in newOrUpdatedGenres)
                        {
                            if (!genre.IsManaged)
                            {
                                realm.Add(genre, update: true);

                            }
                            else if (genre.IsManaged)
                            {
                                var mged = realm.Find<GenreModel>(genre.Id);
                                if (mged != null)
                                {
                                    realm.Add(mged, update: true);
                                }
                                else
                                {
                                    Debug.WriteLine("skipped...album in newOrUpdatedAlbums");
                                }

                            }
                        }
                    }

                    if (newOrUpdatedSongs.Any())
                    {
                        foreach (var song in newOrUpdatedSongs)
                        {

                            if (song.Album != null)
                            {

                                if (!song.Album.IsManaged)
                                {
                                    // If the album is not managed, add it to the realm
                                    realm.Add(song.Album, true);
                                }
                                else if (song.Album.IsManaged)
                                {
                                    var managedAlbum = realm.Find<AlbumModel>(song.Album.Id);

                                    if (managedAlbum != null)
                                    {

                                        //song.Album = managedAlbum;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("skipped...album in song");
                                }
                            }

                            if (song.Genre != null)
                            {
                                if (!song.Genre.IsManaged)
                                {
                                    realm.Add(song.Genre, true);
                                }
                                else if (song.Genre.IsManaged)
                                {
                                    var managedGenre = realm.Find<GenreModel>(song.Genre.Id);
                                    if (managedGenre != null)
                                    {
                                        //song.Genre = managedGenre;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("skipped...Genre in song");
                                }
                            }

                            if (song.ArtistIds != null && song.ArtistIds.Any())
                            {
                                var updatedArtistListForSong = new List<ArtistModel>();
                                foreach (var artistRef in song.ArtistIds)
                                {
                                    if (artistRef is not null)
                                    {

                                        if (!artistRef.IsManaged)
                                        {
                                            realm.Add(song.Genre, true);
                                        }
                                        else if (artistRef.IsManaged)
                                        {
                                            var managedArtist = realm.Find<ArtistModel>(artistRef.Id);
                                            if (managedArtist != null)
                                            {
                                                updatedArtistListForSong.Add(managedArtist);
                                            }
                                        }
                                        else
                                        {
                                            updatedArtistListForSong.Add(artistRef);
                                        }
                                    }
                                }



                                if (!song.IsManaged)
                                {
                                    song.ArtistIds.Clear();
                                    foreach (var art in updatedArtistListForSong)
                                    {
                                        song.ArtistIds.Add(art);
                                    }
                                }

                                else
                                {
                                    Debug.WriteLine("skipped...art  in song");
                                }
                            }
                            //realm.Add(song, update: true);
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
            Artists = newOrUpdatedArtists.ToList(),
            Albums = newOrUpdatedAlbums.ToList(),
            Songs = newOrUpdatedSongs.ToList(),
            Genres = newOrUpdatedGenres.ToList()
        };
    }

    public void LoadInSongsAndEvents()
    {
        _logger.LogInformation("Loading all songs from database into global state.");
        var allSongs = _songRepo.GetAll(true).AsEnumerable().DistinctBy(x => x.Title).ToList();
        _state.LoadAllSongs(allSongs.AsReadOnly());
        _logger.LogInformation("Loaded {SongCount} songs into global state.", allSongs.Count);

        var allEvents = _playEventsRepo.GetAll();
        _state.LoadAllPlayHistory(allEvents);
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
    public async Task<LoadSongsResult?> ScanSpecificPathsAsync(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return await ScanLibraryAsync(pathsToScan).ConfigureAwait(false);
    }
}