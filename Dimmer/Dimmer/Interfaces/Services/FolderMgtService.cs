// --- START OF FILE FolderMgtService.cs ---
// Add other necessary using statements
using System.Reactive;

namespace Dimmer.Interfaces.Services;

public class FolderMgtService : IFolderMgtService
{
    private readonly IDimmerStateService _state;
    private readonly IRealmFactory realmFactory;
    private readonly IFolderMonitorService _folderMonitor;
    private readonly ILibraryScannerService _libraryScanner;
    private readonly ILogger<FolderMgtService> _logger;
    private readonly ProcessingConfig _config;

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFoldersBehaviorSubject = new(Array.Empty<FolderModel>());
    private readonly CompositeDisposable _monitorSubscriptions = new();
    private bool _disposed;
    private bool _isCurrentlyWatching;

    private readonly CompositeDisposable _coreSubscriptions = new();
    private readonly CompositeDisposable _watcherSubscriptions = new();

    private readonly Subject<string> _incrementalScanPathsSubject = new();
    private readonly Subject<Unit> _fullScanRequestedSubject = new();

    public FolderMgtService(
        IRealmFactory _realmFact,
        IDimmerStateService state,
        ISettingsService settingsService,
        IFolderMonitorService folderMonitor,
        ILibraryScannerService libraryScanner,
        ILogger<FolderMgtService> logger)
    {
        realmFactory = _realmFact ?? throw new ArgumentNullException(nameof(_realmFact));
        _state = state ?? throw new ArgumentNullException(nameof(state));

        _folderMonitor = folderMonitor ?? throw new ArgumentNullException(nameof(folderMonitor));
        _libraryScanner = libraryScanner ?? throw new ArgumentNullException(nameof(libraryScanner));
        _config =  new ProcessingConfig();
        _logger = logger ?? NullLogger<FolderMgtService>.Instance;



        SetupReactivePipelines();
    }


    public IObservable<IReadOnlyList<FolderModel>> AllWatchedFolders => _allFoldersBehaviorSubject.AsObservable();

    private void SetupReactivePipelines()
    {
        // 1. INCREMENTAL SCANS: Batches multiple file events into a single scan execution
        var batchIncrementalScans = _incrementalScanPathsSubject
            // This is the magic: wait for a 1.5-second pause in events before emitting the accumulated paths
            .Buffer(_incrementalScanPathsSubject.Throttle(TimeSpan.FromSeconds(1.5)))
            .Where(paths => paths.Any())
            .Select(paths => paths.Distinct().Where(p => !string.IsNullOrEmpty(p)).ToList())
            // Concat guarantees that if another batch arrives while scanning, it waits its turn (no concurrency issues)
            .Select(distinctPaths => Observable.FromAsync(async () =>
            {
                _logger.LogInformation("Batched execution: Scanning {Count} modified directories incrementally.", distinctPaths.Count);
                await _libraryScanner.ScanSpecificPaths(distinctPaths, isIncremental: true);
            }))
            .Concat()
            .Subscribe(
                _ => { },
                ex => _logger.LogError(ex, "Error in incremental scan pipeline")
            );

        // 2. FULL SCANS: Debounces massive deletes into a single full library refresh
        var batchFullScans = _fullScanRequestedSubject
            .Throttle(TimeSpan.FromSeconds(2))
            .Select(_ => Observable.FromAsync(async () =>
            {
                _logger.LogInformation("Batched execution: Triggering full library refresh.");
                var realm = realmFactory.GetRealmInstance();
                var appModel = realm.All<AppStateModel>().FirstOrDefault();
                var currentFolders = appModel?.UserMusicFolders.Select(x => x.SystemFolderPath).ToList() ?? new List<string>();
                await _libraryScanner.ScanLibrary(currentFolders);
            }))
            .Concat()
            .Subscribe(
                _ => { },
                ex => _logger.LogError(ex, "Error in full scan pipeline")
            );

        _coreSubscriptions.Add(batchIncrementalScans);
        _coreSubscriptions.Add(batchFullScans);
    }
    public async Task StartWatchingConfiguredFoldersAsync(List<string>? paths = null)
    {
        try
        {
            if (_isCurrentlyWatching)
            {
                StopWatching();
            }

            var realm = realmFactory.GetRealmInstance();
            var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
            var foldersToWatchPaths = appModel?.UserMusicFolders.Freeze().AsEnumerable().Select(x => x.SystemFolderPath).ToList() ?? new List<string>();

            if (foldersToWatchPaths.Count <= 0)
                foldersToWatchPaths = paths ?? new List<string>();

            if (foldersToWatchPaths.Count == 0)
            {
                _logger.LogInformation("No folders configured to watch.");
                _allFoldersBehaviorSubject.OnNext(Array.Empty<FolderModel>());
                return;
            }

            _logger.LogInformation("Starting to watch folders: {Folders}", string.Join(", ", foldersToWatchPaths));

            var sanitizedPaths = new List<string>();
            foreach (var path in foldersToWatchPaths)
            {
                if (!sanitizedPaths.Any(existing => path.StartsWith(existing, StringComparison.OrdinalIgnoreCase)))
                {
                    sanitizedPaths.RemoveAll(existing => existing.StartsWith(path, StringComparison.OrdinalIgnoreCase));
                    sanitizedPaths.Add(path);
                }
            }

            await realm.WriteAsync(() =>
            {
                appModel?.UserMusicFolders.Clear();
                foreach (var p in sanitizedPaths)
                    appModel?.UserMusicFolders.Add(new() { SystemFolderPath = p, ReadableFolderPath = p });
            });

            var folderModels = sanitizedPaths.Select(p => new FolderModel { Path = p }).ToList();
            _allFoldersBehaviorSubject.OnNext(folderModels.AsReadOnly());

            _watcherSubscriptions.Clear();

            // Hook up Rx Subscriptions
            _watcherSubscriptions.Add(_folderMonitor.FileCreated.Subscribe(HandleFileOrFolderCreated));
            _watcherSubscriptions.Add(_folderMonitor.FileChanged.Subscribe(HandleFileOrFolderChanged));
            _watcherSubscriptions.Add(_folderMonitor.FileRenamed.Subscribe(HandleFileOrFolderRenamed));
            _watcherSubscriptions.Add(_folderMonitor.FileDeleted.Subscribe(HandleFileOrFolderDeleted));

            await _folderMonitor.StartAsync(sanitizedPaths);

            _isCurrentlyWatching = true;
            _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderWatchStarted, null, null, null));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    public void StopWatching()
    {
        if (!_isCurrentlyWatching) return;

        _logger.LogInformation("Stopping folder watching.");
        _folderMonitor.Stop();

        _watcherSubscriptions.Clear(); // Cleanly disposes the monitor Rx hooks
        _isCurrentlyWatching = false;
    }

    public async Task UpdateFolderInWatchListAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogWarning("Attempted to update non-existent folder in watch list: {Path}", path);
            return;
        }
        _logger.LogInformation("Updating folder in watch list: {Path}", path);
        var realm = realmFactory.GetRealmInstance();
        var foldersToWatchPaths = realm.All<AppStateModel>().FirstOrDefaultNullSafe()?.UserMusicFolders;
        
        if (foldersToWatchPaths != null)
        {
            var fold = foldersToWatchPaths.First(x => x.SystemFolderPath == path);

            var indexx = foldersToWatchPaths.IndexOf(fold);
            if (indexx != -1)
            {
                realm.Write(() => foldersToWatchPaths[indexx] = fold);
            }
            await StartWatchingConfiguredFoldersAsync();
        }
        
    }
    public async Task AddFolderToWatchListAndScan(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Attempted to add null or empty path to watch list.");
            return;
        }
        if (!Directory.Exists(path))
        {
            _logger.LogError("Directory not found: {Path}. Cannot add to watch list.", path);
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }
        var realm = realmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefault();
        await realm.WriteAsync(() =>
        {

            var foldersToWatchPaths = appModel?.UserMusicFolders ;

            var firstOfDef = foldersToWatchPaths?.FirstOrDefault(x => x.SystemFolderPath == path);
            if (firstOfDef is null)
            {
                foldersToWatchPaths?.Add(new() { SystemFolderPath = path, ReadableFolderPath = path });
            }
        });

                await StartWatchingConfiguredFoldersAsync();
        _logger.LogInformation("Adding folder to watch list and settings: {Path}", path);


        _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderAdded, path, null, null));

        _logger.LogInformation("Triggering scan for newly added folder: {Path}", path);
        await _libraryScanner.ScanSpecificPaths(new List<string> { path }, isIncremental: false);
    }

    public async Task AddManyFoldersToWatchListAndScan(List<string> paths)
    {
        var newPathsToAdd = new List<string>();
        var realm = realmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();


        if (appModel == null)
        {
            return;
        }
        var knowPaths = appModel.UserMusicFolders ;
        await realm.WriteAsync(async () =>
        {
            foreach (var path in paths)
            {
                var firstOfDef = knowPaths?.FirstOrDefault(x => x.SystemFolderPath == path);
                if (firstOfDef is null)
                {
                    appModel.UserMusicFolders?.Add(new() { SystemFolderPath = path, ReadableFolderPath = path });
                    newPathsToAdd.Add(path);
                }
            }
            realm.Add<AppStateModel>(appModel,true);
        });
        if (newPathsToAdd.Count!=0)
        {
            _logger.LogInformation("Adding {Count} new folders to watch list.", newPathsToAdd.Count);

            // 1. Restart the watcher ONCE with the full new list of folders.
          await  StartWatchingConfiguredFoldersAsync();

            // 2. Scan all newly added paths in a single operation.
            _logger.LogInformation("Triggering scan for newly added folders.");
            await _libraryScanner.ScanSpecificPaths(newPathsToAdd, isIncremental: false);
        }
    }
    public async Task RemoveFolderFromWatchListAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        var realm = realmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefault();
        var foldersToWatchPaths = appModel?.UserMusicFolders ;
        var firstOfDef = foldersToWatchPaths?.FirstOrDefault(x => x.SystemFolderPath == path);
        bool removed = false ;
        if (foldersToWatchPaths is not null && firstOfDef is not null)
        {
            realm.Write(() =>
            {

                removed = foldersToWatchPaths.Remove(firstOfDef);

            });
            if (removed)
            {
                _logger.LogInformation("Removed folder from watch list and settings: {Path}", path);
              await  StartWatchingConfiguredFoldersAsync();


                _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderRemoved, path, null, null));


                _logger.LogInformation("Triggering library refresh after removing folder: {Path}", path);
                await _libraryScanner.ScanLibrary(foldersToWatchPaths.Freeze().AsEnumerable().Select(x=>x.SystemFolderPath).ToList());
            }
            else
            {
                _logger.LogWarning("Folder {Path} not found in watch list settings for removal.", path);
            }
        }
    }

    public async Task ClearAllWatchedFoldersAndRescanAsync()
    {
        var realm = realmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefault();
        var foldersToWatchPaths = appModel?.UserMusicFolders;

        _logger.LogInformation("Clearing all watched folders and settings.");
        foldersToWatchPaths?.Clear();
       await StartWatchingConfiguredFoldersAsync();

        _state.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderRemoved, "ALL_FOLDERS_CLEARED", null, null));

        _logger.LogInformation("Triggering library scan with empty folder list (to clear library).");
        await _libraryScanner.ScanLibrary(new List<string>());
    }


    private void HandleFileOrFolderCreated(FileSystemEventArgs e)
    {
        if (IsRelevantAudioFile(e.FullPath))
        {
            // Push the directory up to be batched
            var dir = Path.GetDirectoryName(e.FullPath);
            if (dir != null) _incrementalScanPathsSubject.OnNext(dir);
        }
        else if (Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath))
        {
            _incrementalScanPathsSubject.OnNext(e.FullPath);
        }
    }

    private void HandleFileOrFolderChanged(FileSystemEventArgs e)
    {
        if (IsRelevantAudioFile(e.FullPath))
        {
            var dir = Path.GetDirectoryName(e.FullPath);
            if (dir != null) _incrementalScanPathsSubject.OnNext(dir);
        }
    }

    private void HandleFileOrFolderRenamed(RenamedEventArgs e)
    {
        bool oldIsAudio = IsRelevantAudioFile(e.OldName) || WasPathPreviouslyKnownAudio(e.OldFullPath);
        bool newIsAudio = IsRelevantAudioFile(e.Name);

        if (oldIsAudio || newIsAudio)
        {
            var oldDir = Path.GetDirectoryName(e.OldFullPath);
            var newDir = Path.GetDirectoryName(e.FullPath);

            if (oldDir != null) _incrementalScanPathsSubject.OnNext(oldDir);
            if (newDir != null) _incrementalScanPathsSubject.OnNext(newDir);
        }
        else if ((Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath)) || WasPathPreviouslyWatchedSubfolder(e.OldFullPath))
        {
            var oldDir = Path.GetDirectoryName(e.OldFullPath);
            var newDir = Path.GetDirectoryName(e.FullPath);

            if (oldDir != null) _incrementalScanPathsSubject.OnNext(oldDir);
            _incrementalScanPathsSubject.OnNext(e.FullPath);
            if (newDir != null && newDir != e.FullPath) _incrementalScanPathsSubject.OnNext(newDir);
        }
    }

    private void HandleFileOrFolderDeleted(FileSystemEventArgs e)
    {
        if (IsRelevantAudioFile(e.Name) || WasPathPreviouslyKnownAudio(e.FullPath))
        {
            var dir = Path.GetDirectoryName(e.FullPath);
            if (dir != null) _incrementalScanPathsSubject.OnNext(dir);
        }
        else if (WasPathPreviouslyWatchedSubfolder(e.FullPath))
        {
            // Pushing to this subject debounces and triggers ScanLibrary() once
            _fullScanRequestedSubject.OnNext(Unit.Default);
        }
    }


    private bool IsRelevantAudioFile(string? fileNameOrPath)
    {
        if (string.IsNullOrEmpty(fileNameOrPath))
            return false;
        string extension = Path.GetExtension(fileNameOrPath);
        return _config.SupportedAudioExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
    private bool IsPathWithinWatchedFolders(string path)
    {
        var realm = realmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefault();
        var foldersToWatchPaths = appModel?.UserMusicFolders;


        if (foldersToWatchPaths == null)
            return false;
        return foldersToWatchPaths.Any(watchedFolder => path.StartsWith(watchedFolder.SystemFolderPath, StringComparison.OrdinalIgnoreCase));
    }
    private bool WasPathPreviouslyKnownAudio(string fullPath) {/* TODO: Check against current library if needed */ return false; }
    private bool WasPathPreviouslyWatchedSubfolder(string fullPath) {/* TODO: Check against known subfolders if needed */ return false; }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.LogInformation("Disposing FolderManagementService.");
            StopWatching();

            _coreSubscriptions.Dispose();
            _watcherSubscriptions.Dispose();
            _incrementalScanPathsSubject.Dispose();
            _fullScanRequestedSubject.Dispose();
            _allFoldersBehaviorSubject.Dispose();

            (_folderMonitor as IDisposable)?.Dispose();
        }
        _disposed = true;
    }

    public async Task ReScanFolder(string folderPath)
    {
        await _libraryScanner.ScanSpecificPaths(new List<string> { folderPath }, isIncremental: false);
    }

}