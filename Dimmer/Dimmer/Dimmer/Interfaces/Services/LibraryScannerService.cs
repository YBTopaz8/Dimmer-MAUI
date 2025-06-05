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
            var mapper = _mapper ?? throw new ArgumentNullException(nameof(_mapper)); // Ensure you have an IMapper instance
            _logger.LogInformation("Persisting metadata changes to database...");

            using (var realm = _realmFactory.GetRealmInstance())
            {
                await realm.WriteAsync(() =>
                {
                    // --- Helper function to process an individual item (Album, Artist, Genre) ---
                    // This ensures it's correctly added/updated in the current realm.
                    // Returns the managed instance from the CURRENT realm, or null if it couldn't be processed.
                    T ProcessTopLevelEntity<T>(T entityData) where T : class, IRealmObject, new()
                    {
                        if (entityData == null)
                            return null;

                        // 1. If it’s genuinely unmanaged, just add/update it directly.
                        if (!entityData.IsManaged)
                        {
                            return realm.Add(entityData, update: true);
                        }

                        // 2. If it’s already managed by THIS realm, re-add/update is a no-op (or explicit Add is fine).
                        if (entityData.Realm == realm)
                        {
                            return realm.Add(entityData, update: true);
                        }

                        // 3. Otherwise it’s managed by a different live realm (regardless of frozen or not).
                        //    We must create an unmanaged copy and then add that.
                        var unmanagedCopy = mapper.Map<T>(entityData);


                        return realm.Add(unmanagedCopy, update: true);
                    }

                    // --- Process top-level entities first to ensure they exist for relationships ---
                    if (newOrUpdatedAlbums.Any())
                    {
                        foreach (var albumData in newOrUpdatedAlbums)
                        {
                            ProcessTopLevelEntity(albumData); // We don't strictly need the return value here for now
                        }
                    }

                    if (newOrUpdatedArtists.Any())
                    {
                        foreach (var artistData in newOrUpdatedArtists)
                        {
                            ProcessTopLevelEntity(artistData);
                        }
                    }

                    if (newOrUpdatedGenres.Any())
                    {
                        foreach (var genreData in newOrUpdatedGenres)
                        {
                            ProcessTopLevelEntity(genreData);
                        }
                    }

                    // --- Process Songs ---
                    if (newOrUpdatedSongs.Any())
                    {
                        foreach (var song in newOrUpdatedSongs) // Assuming 'song' itself is usually unmanaged or from foreign realm
                        {
                            AlbumModel? finalAlbumForSong = null;
                            if (song.Album != null)
                            {
                                finalAlbumForSong = ProcessTopLevelEntity(song.Album); // Use helper
                            }

                            GenreModel? finalGenreForSong = null;
                            if (song.Genre != null)
                            {
                                finalGenreForSong = ProcessTopLevelEntity(song.Genre); // Use helper
                            }

                            var finalArtistsForSong = new List<ArtistModel>();
                            if (song.ArtistIds != null)
                            {
                                foreach (var artistRef in song.ArtistIds)
                                {
                                    var processedArtist = ProcessTopLevelEntity(artistRef);
                                    if (processedArtist != null)
                                    {
                                        finalArtistsForSong.Add(processedArtist);
                                    }
                                }
                            }

                            // Now, prepare the song instance itself for adding/updating.
                            // If 'song' is unmanaged or from a foreign realm, we use AutoMapper to get a clean unmanaged copy.
                            // If 'song' is already managed by THIS realm, we find it and update its properties.

                            SongModel songToPersist;
                            if (!song.IsManaged || song.Realm != realm || song.IsFrozen)
                            {
                                // Song is unmanaged, or from another realm, or frozen.
                                // Create/get an unmanaged version with the song's current data.
                                // If song is already unmanaged, Map will just return a new unmanaged instance with copied data.
                                songToPersist = mapper.Map<SongModel>(song); // Create an unmanaged representation

                                // Assign the correctly processed related objects (which are now managed by current realm)
                                songToPersist.Album = finalAlbumForSong;
                                songToPersist.Genre = finalGenreForSong;
                                songToPersist.ArtistIds.Clear();
                                foreach (var art in finalArtistsForSong)
                                {
                                    songToPersist.ArtistIds.Add(art);
                                }
                                realm.Add(songToPersist, update: true);
                            }
                            else // Song is already managed by THIS realm (song.Realm == realm && !song.IsFrozen)
                            {
                                songToPersist = song; // It's the live object

                                // Update its properties directly
                                songToPersist.Album = finalAlbumForSong;
                                songToPersist.Genre = finalGenreForSong;
                                songToPersist.ArtistIds.Clear();
                                foreach (var art in finalArtistsForSong)
                                {
                                    songToPersist.ArtistIds.Add(art);
                                }
                                // No explicit realm.Add(songToPersist) needed here, changes to managed object are saved.
                                // However, calling realm.Add(songToPersist, update:true) is also safe and can be explicit.
                                // For consistency with the other branch, let's keep it if you prefer:
                                // realm.Add(songToPersist, update: true);
                            }
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