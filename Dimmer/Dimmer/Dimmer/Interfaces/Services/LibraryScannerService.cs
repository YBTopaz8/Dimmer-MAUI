using System.Diagnostics;

using Dimmer.Interfaces.Services.Interfaces;

using Realms;


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
            if (folderPaths == null || folderPaths.Count==0)
            {
                _logger.LogWarning("ScanLibrary called with no folder paths.");
                _state.SetCurrentLogMsg(new AppLogModel { Log = "No folders selected for scanning." });

                var existingState = realm.All<AppStateModel>().ToList();
                var eState = existingState.FirstOrDefault();
                if (eState is not null)
                {

                    folderPaths = eState.UserMusicFoldersPreference.ToList();
                }

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
            var existingArtists = realm.All<ArtistModel>().ToList();
            var existingAlbums = realm.All<AlbumModel>().ToList();
            var existingGenres = realm.All<GenreModel>().ToList();
            var existingSongs = realm.All<SongModel>().ToList();
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

                var fileProcessingResult = audioFileProcessor.ProcessFile(file);

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


                await realm.WriteAsync(() =>
                {
                    // --- STEP 1: BRING ALL "PARENT" ENTITIES INTO THE CURRENT REALM ---
                    // This ensures that when we need to link a song to an album, that album
                    // already exists as a managed object within THIS specific transaction.
                    T EnsureManaged<T>(T entity) where T : class, IRealmObject, new()
                    {
                        if (entity == null)
                            return null;
                        // WHY THIS IS SAFE: If the object is from another realm, mapper.Map creates an
                        // unmanaged copy. realm.Add then creates a new managed object in THIS realm.
                        // If it's already in this realm, it's a no-op. This normalizes everything.
                        if (entity.IsManaged && entity.Realm == realm)
                            return entity;
                        return realm.Add(mapper.Map<T>(entity), update: true);
                    }

                    foreach (var album in newOrUpdatedAlbums)
                    { EnsureManaged(album); }
                    foreach (var artist in newOrUpdatedArtists)
                    { EnsureManaged(artist); }
                    foreach (var genre in newOrUpdatedGenres)
                    { EnsureManaged(genre); }

                    // --- STEP 2: PROCESS EACH SONG INDIVIDUALLY ---
                    if (newOrUpdatedSongs.Any())
                    {
                        foreach (var incomingSongData in newOrUpdatedSongs)
                        {
                            // WHY THIS IS SAFE: This is the first and most important step. We use the ID
                            // from the foreign data to find the object's representation *within this realm*.
                            // After this line, `songToPersist` is either a live, managed object from this
                            // realm, or it is null. It is NEVER a foreign object.
                            var songToPersist = realm.Find<SongModel>(incomingSongData.Id);

                            if (songToPersist == null) // The song is brand new to this realm.
                            {
                                var unmanagedSong = mapper.Map<SongModel>(incomingSongData);

                                // WHY THIS IS SAFE: This is the "air gap". We are explicitly destroying any
                                // potential links to foreign-managed objects before adding the new song.
                                // This prevents Realm from trying to follow a link to another realm instance.
                                //unmanagedSong.Album = null;
                                //unmanagedSong.Genre = null;
                                //unmanagedSong.ArtistIds.Clear();

                                // Now we add the clean, unlinked song. It becomes managed by THIS realm.
                                songToPersist = realm.Add(unmanagedSong, update: true);
                            }
                            else // The song already exists in this realm.
                            {
                                // WHY THIS IS SAFE: We are mapping primitive properties from the data bag
                                // onto the LIVE, managed object. This requires an AutoMapper profile
                                // that ignores relationship properties during this mapping operation.
                                mapper.Map(incomingSongData, songToPersist);
                            }

                            // --- STEP 3: RE-BUILD RELATIONSHIPS USING ONLY LOCAL OBJECTS ---
                            // At this point, `songToPersist` is GUARANTEED to be managed by `realm`.

                            if (incomingSongData.Album != null)
                            {
                                // WHY THIS IS SAFE: We use the ID to `Find` the album that is also managed
                                // by THIS SAME `realm` instance. We are linking local-to-local. This is
                                // the core principle that makes the whole operation valid.
                                songToPersist.Album = realm.Find<AlbumModel>(incomingSongData.Album.Id);
                            }
                            else
                            {
                                songToPersist.Album = null;
                            }

                            // (Same logic applies to Genre)
                            if (incomingSongData.Genre != null)
                            {
                                songToPersist.Genre = realm.Find<GenreModel>(incomingSongData.Genre.Id);
                            }
                            else
                            {
                                songToPersist.Genre = null;
                            }
                            if (incomingSongData.Artist != null)
                            {
                                // Find the MANAGED version of the primary artist and link it.
                                songToPersist.Artist = realm.Find<ArtistModel>(incomingSongData.Artist.Id);
                            }
                            // (Same logic applies to the Artists list)
                            songToPersist.ArtistIds.Clear();
                            if (incomingSongData.ArtistIds != null && incomingSongData.ArtistIds.Any())
                            {
                                foreach (var artistData in incomingSongData.ArtistIds)
                                {
                                    // Find the local version of the artist.
                                    var managedArtist = realm.Find<ArtistModel>(artistData.Id);
                                    if (managedArtist != null)
                                    {
                                        // Add the local artist to the local song's list. Perfectly safe.
                                        songToPersist.ArtistIds.Add(managedArtist);
                                    }
                                }
                            }
                        }
                    }
                }).ConfigureAwait(false);

                _logger.LogInformation("Metadata changes persisted.");



                //var song = _songRepo.GetAll().First();



                await realm.WriteAsync(() =>
            {

                var statee = realm.All<AppStateModel>().ToList();
                var curState = statee[0];
                var listofPref = curState.UserMusicFoldersPreference.ToList();

                listofPref.AddRange(folderPaths);
                var disTinctList = listofPref.Distinct();
                curState.UserMusicFoldersPreference.Clear();
                foreach (var item in disTinctList)
                {
                    curState.UserMusicFoldersPreference.Add(item);
                }
                realm.Add(curState, update: true); // Update the app state with new folder paths
            });

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
            _logger.LogError(ex, ex.Message);
            return null;
        }

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
    public Task<LoadSongsResult>? ScanSpecificPaths(List<string> pathsToScan, bool isIncremental = true)
    {
        _logger.LogInformation("Starting specific path scan (currently full scan of paths): {Paths}", string.Join(", ", pathsToScan));
        return Task.Run(() => ScanLibrary(pathsToScan));
    }
}