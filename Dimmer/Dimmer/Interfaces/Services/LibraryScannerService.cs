using Dimmer.Data.Models;
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
                _state.SetCurrentLogMsg("No folders selected for scanning." ,DimmerLogLevel.Info);

                var existingState = realm.All<AppStateModel>().FirstOrDefault();
                if (existingState is not null)
                {
                    folderPaths = [.. existingState.UserMusicFolders.Select(x=>x.SystemFolderPath)];
                }
            }

            if (folderPaths == null || folderPaths.Count == 0)
            {
                _logger.LogWarning("No folder paths found to scan.");
                return new LoadSongsResult { NewSongsAddedCount = 0 };

            }

            _logger.LogInformation("Starting library scan for paths: {Paths}", string.Join(", ", folderPaths));
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanStarted, string.Join(";", folderPaths), null, null));
            _state.SetCurrentLogMsg("Starting music scan...", DimmerLogLevel.Info);

            MusicMetadataService currentScanMetadataService = new();

            List<string> allFiles = await TaggingUtils.GetAllAudioFilesFromPathsAsync(folderPaths, _config.SupportedAudioExtensions);

            if (allFiles.Count == 0)
            {
                _state.SetCurrentLogMsg("No audio files found in the selected paths." ,DimmerLogLevel.Info);
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
                List<AlbumModelView?> existingAlbums = realmm.All<AlbumModel>().ToList().Select(x => x.Freeze().ToAlbumModelView()).ToList();
                List<GenreModelView?> existingGenres = realmm.All<GenreModel>().ToList().Select(x => x.Freeze().ToGenreModelView()).ToList();
                List<SongModelView?> existingSongs = realmm.All<SongModel>().ToList().Select(x => x.Freeze().ToSongModelView()).ToList();



                _logger.LogDebug("Loaded {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres, {SongCount} songs.",
                    existingArtists.Count, existingAlbums.Count, existingGenres.Count, existingSongs.Count);

                currentScanMetadataService.LoadExistingData(existingArtists, existingAlbums, existingGenres, existingSongs);
            }
            var newFilesToProcess = allFiles.Where(file => !currentScanMetadataService.HasFileBeenProcessed(file)).ToList();

            int totalFilesToProcess = newFilesToProcess.Count;
            if (totalFilesToProcess == 0)
            {
                _logger.LogInformation("Scan complete. No new music found.");
                _state.SetCurrentLogMsg("Your library is up-to-date.", DimmerLogLevel.Info);
                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, "No new files.", null, null));
                return new LoadSongsResult { NewSongsAddedCount = 0 };

            }

            _logger.LogInformation("Found {TotalFiles} new audio files to process.", totalFilesToProcess);
            _state.SetCurrentLogMsg($"Found {totalFilesToProcess} new songs. Starting import...", DimmerLogLevel.Info);

            // --- 4. Process ONLY the new files ---
            var audioFileProcessor = new AudioFileProcessor( currentScanMetadataService, _config);
            int progress = 0;
            var processedResults =  await audioFileProcessor.ProcessFilesInParallelForEachAsync(newFilesToProcess);


            foreach (var result in processedResults)
            {
                progress++;
                if (progress % 50 == 0 || progress == processedResults.Count)
                {
                    _state.LogProgress($"Processed {progress}/{processedResults.Count}...",
progress,processedResults.Count);
                }
            }


            _logger.LogInformation("File processing complete. Consolidating metadata changes.");

            var newArtists = currentScanMetadataService.NewArtists.DistinctBy(x=>x.Name).ToList();
            var newAlbums = currentScanMetadataService.NewAlbums.DistinctBy(x => x.Name).ToList();
            var newGenres = currentScanMetadataService.NewGenres.DistinctBy(x => x.Name).ToList();
            List<SongModelView> newSongs = processedResults.Where(r => r.Success && r.ProcessedSong != null).Select(r => r.ProcessedSong).ToList()!;

            if (newSongs.Count!=0)
            {
                _logger.LogInformation("Found {SongCount} new songs, {ArtistCount} artists, {AlbumCount} albums, {GenreCount} genres to persist.",
                    newSongs.Count, newArtists.Count, newAlbums.Count, newGenres.Count);
                _logger.LogInformation("Persisting metadata changes to database...");


                IEnumerable<ArtistModel> artistModelsToUpsert = newArtists.Select(x =>
                {
                    var toModel = x.ToArtistModel();
                     
                    return toModel;
                })!;
                IEnumerable<AlbumModel> albumModelsToUpsert = newAlbums.Select(x => x.ToAlbumModel()!)!;
                IEnumerable<GenreModel> genreModelsToUpsert = newGenres.Select(x => x.ToGenreModel()!)!;
                IEnumerable<SongModel> songModelsToUpsert = newSongs.Select(x => x.ToSongModel()!)!;


                var songModelDict = songModelsToUpsert.ToDictionary(s => s.Id);

                using (Realm realmInserts = _realmFactory.GetRealmInstance())
                {
                    if (realmInserts is not null)
                    {
                        await realmInserts.WriteAsync(() =>
                        {
                            // --- Step 1: Managed Artist Lookup ---
                            var managedArtists = new Dictionary<ObjectId, ArtistModel>();
                            
                            foreach (ArtistModel artView in artistModelsToUpsert)
                            {
                                
                                ArtistModel? managed = realmInserts.Add(artView, update: true);
                                if (managed is not null)
                                {                                    
                                    managedArtists[artView.Id] = managed;
                                }                                    
                              
                            }
                        

                            // --- Step 2: Managed Genre Lookup ---
                            var managedGenres = new Dictionary<ObjectId, GenreModel>();
                            foreach (var gnrView in genreModelsToUpsert)
                            {
                               
                                var managed = realmInserts.Add(gnrView, update: true);
                                if (managed is not null)
                                {
                                    managedGenres[gnrView.Id] = managed;
                                } 
                                
                                
                            }

                            // --- Step 3: Upsert Albums and link to Managed Artists ---
                            var managedAlbums = new Dictionary<ObjectId, AlbumModel>();

                            foreach (var albumModel in albumModelsToUpsert)
                            {

                                AlbumModelView albView = newAlbums.Find(x => x.Id == albumModel.Id)!;
                                if (albView.Artists != null)
                                {
                                    foreach (var artView in albView.Artists)
                                    {
                                        
                                        if (managedArtists.TryGetValue(artView.Id, out var managedArt))
                                        {
                                            if (!albumModel.Artists.Contains(managedArt))
                                            {
                                                albumModel.Artists.Add(managedArt);
                                            } 
                                        }
                                        else
                                        {
                                            var artistFromDB = realmInserts.All<ArtistModel>().FirstOrDefaultNullSafe(x => x.Name == artView.Name);
                                            if (artistFromDB is not null)
                                            {
                                                albumModel.Artists.Add(artistFromDB);
                                            }
                                            
                                        }
                                    }
                                }
                                else
                                {

                                }
                                albumModel.Artist = albumModel.Artists.FirstOrDefault();
                                if(albumModel.Artists.Count<1)
                                {

                                }
                                var managedAlbum = realmInserts.Add(albumModel, update: true);
                                managedAlbums[albView.Id] = managedAlbum; 

                            }
                            var albumLookup = new Dictionary<(string AlbumName, string ArtistName), AlbumModel>();
                            foreach (var managedAlbum in managedAlbums.Values)
                            {
                                string albName = managedAlbum.Name ?? "";

                                string artName = managedAlbum.Artist?.Name ?? managedAlbum.Artists.FirstOrDefault()?.Name ?? "";
                                if(artName =="")
                                {

                                }
                                albumLookup[(albName, artName)] = managedAlbum;
                            }
                            // --- Step 4: Upsert Songs and link everything ---
                            foreach (var songModel in songModelsToUpsert)
                            {


                                if (songModel is not null)
                                {


                                    AlbumModel? albIfAny = null;
                                    var songView = newSongs.Find(x => x.Id == songModel.Id);
                                    if (songView is not null)
                                    {
                                        var lookupKey = (songView.AlbumName ?? "", songView.ArtistName ?? "");

                                        if (albumLookup.TryGetValue(lookupKey, out var foundAlbum))
                                        {
                                            songModel.Album = foundAlbum;
                                        }
                                        else
                                        {
                                           
                                            var alb = managedAlbums.Values.Where(a => a.Name == songView.AlbumName).FirstOrDefault();
                                            if(alb is not null)
                                            {                                                
                                                songModel.Album = alb;
                                              
                                            }
                                        }
                                        if(songModel.Album is null)
                                        {

                                        }
                                        var gnrIfAny = realmInserts.All<GenreModel>().FirstOrDefaultNullSafe(x => x.Name == songView.GenreName);
                                        if (gnrIfAny is not null)
                                        {
                                            songModel.Genre = gnrIfAny;
                                        }
                                    
                                        if (songView.ArtistToSong != null)
                                        {
                                            foreach (var artView in songView.ArtistToSong)
                                            {
                                                if (artView is not null)
                                                {
                                                    var artIfAny = realmInserts.All<ArtistModel>().FirstOrDefaultNullSafe(x => x.Name == artView.Name);
                                                    if (artIfAny is not null)
                                                    {
                                                        songModel.ArtistToSong.Add(artIfAny);
                                                    }
                                                }
                                            }

                                            if (songModel.ArtistToSong.Count > 0)
                                            {
                                                songModel.Artist = songModel.ArtistToSong[0];

                                                songModel.ArtistName = songModel.Artist?.Name ?? "Unknown Artist";

                                            }
                                        }

                                    }
                                    songModel.IsNew = false;

                                   
                                    if (songModel.Genre is null)
                                    {

                                    }
                                    if (songModel.Artist is null)
                                    {

                                    }

                                
                                    if (songModel!.ArtistToSong.Count < 1)
                                    {

                                    }
                                    if(songModel.Album is null)
                                    {

                                    }
                                    else
                                    {

                                        if (songModel.Album.Artists.Count < 1)
                                        {
                                            songModel.Album.Artists.AddRange(songModel.ArtistToSong);
                                        }
                                    }
                                    if (songModel!.Album!.Artists.Count < 1)
                                    {
                                        
                                    }
                                
                                realmInserts.Add(songModel, update: true);
                                    artistModelsToUpsert = Enumerable.Empty<ArtistModel>();
                                    albumModelsToUpsert = Enumerable.Empty<AlbumModel>();
                                    genreModelsToUpsert = Enumerable.Empty<GenreModel>();
                                    songModelsToUpsert = Enumerable.Empty<SongModel>();
                                }
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


            using (var realmAppState = _realmFactory.GetRealmInstance())
            {
                await realmAppState.WriteAsync(() =>
                {
                    var appState = realmAppState.All<AppStateModel>().FirstOrDefault();
                    if (appState != null)
                    {
                        var distinctFolders = appState.UserMusicFolders.AsEnumerable().Select(x=>x.SystemFolderPath).Union(folderPaths).Distinct().ToList();
                        appState.UserMusicFolders.Clear();
                        foreach (var folder in distinctFolders)
                        {
                            appState.UserMusicFolders.Add(new() { SystemFolderPath = folder, ReadableFolderPath = folder });
                        }
                    }
                });
            }

            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderScanCompleted, extParam: newSongs,null, null));


            // clear up and clean memory 
            currentScanMetadataService.ClearAll();
            
            
            _state.SetCurrentLogMsg("Music scan complete.", DimmerLogLevel.Info);
            _logger.LogInformation("Library scan completed successfully.");
            newSongs.Clear();
            newArtists.Clear();
            newAlbums.Clear();
            newGenres.Clear();
            allFiles.Clear();
            newFilesToProcess.Clear();
           
            allFiles.Clear();
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