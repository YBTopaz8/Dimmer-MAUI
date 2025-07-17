// --- START OF FILE FolderMgtService.cs ---
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
// Add other necessary using statements
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services;

public class FolderMgtService : IFolderMgtService
{
    private readonly IDimmerStateService _state;
    private readonly ISettingsService _settingsService;
    private readonly IFolderMonitorService _folderMonitor;
    private readonly ILibraryScannerService _libraryScanner;
    private readonly ILogger<FolderMgtService> _logger;
    private readonly ProcessingConfig _config;

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFoldersBehaviorSubject = new(Array.Empty<FolderModel>());
    private readonly CompositeDisposable _monitorSubscriptions = new();
    private bool _disposed;
    private bool _isCurrentlyWatching;


    public FolderMgtService(
        IDimmerStateService state,
        ISettingsService settingsService,
        IFolderMonitorService folderMonitor,
        ILibraryScannerService libraryScanner,
        ILogger<FolderMgtService> logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _folderMonitor = folderMonitor ?? throw new ArgumentNullException(nameof(folderMonitor));
        _libraryScanner = libraryScanner ?? throw new ArgumentNullException(nameof(libraryScanner));
        _config =  new ProcessingConfig();
        _logger = logger ?? NullLogger<FolderMgtService>.Instance;



    }

    public IObservable<IReadOnlyList<FolderModel>> AllWatchedFolders => _allFoldersBehaviorSubject.AsObservable();

    public void StartWatchingConfiguredFolders()
    {
        if (_isCurrentlyWatching)
        {
            StopWatching();
        }

        var foldersToWatchPaths = _settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>();
        if (foldersToWatchPaths.Count==0)
        {
            _logger.LogInformation("No folders configured to watch.");
            _allFoldersBehaviorSubject.OnNext(Array.Empty<FolderModel>());
            return;
        }

        _logger.LogInformation("Starting to watch folders: {Folders}", string.Join(", ", foldersToWatchPaths));

        var folderModels = foldersToWatchPaths.Select(p => new FolderModel { Path = p }).ToList();
        _allFoldersBehaviorSubject.OnNext(folderModels.AsReadOnly());


        _monitorSubscriptions.Clear();




        _folderMonitor.OnCreated += HandleFileOrFolderCreated;
        ;
        _folderMonitor.OnDeleted += HandleFileOrFolderDeleted;
        _folderMonitor.OnChanged += HandleFileOrFolderChanged;
        _folderMonitor.OnRenamed += HandleFileOrFolderRenamed;

        _folderMonitor.Start(foldersToWatchPaths);
        _isCurrentlyWatching = true;
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderWatchStarted, null, null, null));
    }


    public void StopWatching()
    {
        if (!_isCurrentlyWatching)
            return;

        _logger.LogInformation("Stopping folder watching.");
        _folderMonitor.Stop();

        _folderMonitor.OnCreated -= HandleFileOrFolderCreated;
        _folderMonitor.OnDeleted -= HandleFileOrFolderDeleted;
        _folderMonitor.OnChanged -= HandleFileOrFolderChanged;
        _folderMonitor.OnRenamed -= HandleFileOrFolderRenamed;

        _monitorSubscriptions.Clear();
        _isCurrentlyWatching = false;
    }

    public void AddFolderToWatchListAndScan(string path)
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

        if (!_settingsService.UserMusicFoldersPreference?.Contains(path, StringComparer.OrdinalIgnoreCase) == true)
        {

            StartWatchingConfiguredFolders();
            _settingsService.AddMusicFolder(path);
        }

        _logger.LogInformation("Adding folder to watch list and settings: {Path}", path);


        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, path, null, null));

        _logger.LogInformation("Triggering scan for newly added folder: {Path}", path);
        Task.Run(async () => await _libraryScanner.ScanSpecificPaths(new List<string> { path }, isIncremental: false));
    }

    public void RemoveFolderFromWatchListAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        bool removed = _settingsService.RemoveMusicFolder(path);
        if (removed)
        {
            _logger.LogInformation("Removed folder from watch list and settings: {Path}", path);
            StartWatchingConfiguredFolders();


            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, path, null, null));





            _logger.LogInformation("Triggering library refresh after removing folder: {Path}", path);
            var remainingFolders = _settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>();
            Task.Run(async () => await _libraryScanner.ScanLibrary(remainingFolders));
        }
        else
        {
            _logger.LogWarning("Folder {Path} not found in watch list settings for removal.", path);
        }
    }

    public void ClearAllWatchedFoldersAndRescanAsync()
    {
        _logger.LogInformation("Clearing all watched folders and settings.");
        _settingsService.ClearAllFolders();
        StartWatchingConfiguredFolders();

        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, "ALL_FOLDERS_CLEARED", null, null));

        _logger.LogInformation("Triggering library scan with empty folder list (to clear library).");
        _libraryScanner.ScanLibrary(new List<string>());
    }


    private async void HandleFileOrFolderCreated(FileSystemEventArgs e)
    {
        _logger.LogDebug("FS Event: Created - {FullPath}", e.FullPath);
        if (IsRelevantAudioFile(e.FullPath))
        {
            _logger.LogInformation("Relevant audio file created: {FilePath}. Triggering incremental scan of parent directory.", e.FullPath);
            // It's often better to scan the directory in case multiple files were added in quick succession
            // or if metadata relies on folder structure.
            await _libraryScanner.ScanSpecificPaths(new List<string> { Path.GetDirectoryName(e.FullPath)! }, isIncremental: true);
        }
        else if (Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath))
        {
            _logger.LogInformation("New directory created within watched scope: {FolderPath}. Triggering incremental scan.", e.FullPath);
           await _libraryScanner.ScanSpecificPaths(new List<string> { e.FullPath }, isIncremental: true);
        }
    }

    private async void HandleFileOrFolderDeleted(FileSystemEventArgs e)
    {
        _logger.LogDebug("FS Event: Deleted - {FullPath}", e.FullPath);
        if (IsRelevantAudioFile(e.Name) || WasPathPreviouslyKnownAudio(e.FullPath))
        {
            _logger.LogInformation("Relevant audio file or known audio path deleted: {FilePath}. Triggering library refresh of parent.", e.FullPath);
            // Re-scan parent directory to update library (remove song, potentially update album/artist if they become empty)
            await Task.Run(() => _libraryScanner.ScanSpecificPaths(new List<string> { Path.GetDirectoryName(e.FullPath)! }, isIncremental: true));
        }
        else if (WasPathPreviouslyWatchedSubfolder(e.FullPath))
        {
            _logger.LogInformation("Watched sub-directory deleted: {FolderPath}. Triggering full library refresh.", e.FullPath);

            var currentFolders = _settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>();
            _libraryScanner.ScanLibrary(currentFolders);
        }
    }

    private void HandleFileOrFolderChanged(string fullPath)
    {
        _logger.LogDebug("FS Event: Changed - {FullPath}", fullPath);
        if (IsRelevantAudioFile(fullPath))
        {
            _logger.LogInformation("Relevant audio file changed: {FilePath}. Triggering incremental scan of parent directory.", fullPath);
            _libraryScanner.ScanSpecificPaths(new List<string> { Path.GetDirectoryName(fullPath)! }, isIncremental: true);


        }
    }

    private void HandleFileOrFolderRenamed(RenamedEventArgs e)
    {
        _logger.LogDebug("FS Event: Renamed - {OldFullPath} to {NewFullPath}", e.OldFullPath, e.FullPath);
        bool oldIsAudio = IsRelevantAudioFile(e.OldName) || WasPathPreviouslyKnownAudio(e.OldFullPath);
        bool newIsAudio = IsRelevantAudioFile(e.Name);

        if (oldIsAudio || newIsAudio)
        {
            _logger.LogInformation("Relevant audio file/folder renamed: {OldPath} -> {NewPath}. Triggering scan of relevant directories.", e.OldFullPath, e.FullPath);
            var pathsToScan = new List<string>();
            if (Path.GetDirectoryName(e.OldFullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.OldFullPath)!);
            if (Path.GetDirectoryName(e.FullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.FullPath)!);

            _libraryScanner.ScanSpecificPaths([.. pathsToScan.Distinct()], isIncremental: true);
        }
        else if (Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath) || WasPathPreviouslyWatchedSubfolder(e.OldFullPath))
        {
            _logger.LogInformation("Directory renamed within watched scope. Old: {OldPath}, New: {NewPath}. Triggering scan.", e.OldFullPath, e.FullPath);
            var pathsToScan = new List<string>();
            if (Path.GetDirectoryName(e.OldFullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.OldFullPath)!);
            pathsToScan.Add(e.FullPath);
            if (Path.GetDirectoryName(e.FullPath) != null && Path.GetDirectoryName(e.FullPath) != e.FullPath)
                pathsToScan.Add(Path.GetDirectoryName(e.FullPath)!);


            _libraryScanner.ScanSpecificPaths([.. pathsToScan.Distinct()], isIncremental: true);
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
        var watched = _settingsService.UserMusicFoldersPreference;
        if (watched == null)
            return false;
        return watched.Any(watchedFolder => path.StartsWith(watchedFolder, StringComparison.OrdinalIgnoreCase));
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
        if (_disposed)
            return;
        if (disposing)
        {
            _logger.LogInformation("Disposing FolderManagementService.");
            StopWatching();
            _monitorSubscriptions.Dispose();
            _allFoldersBehaviorSubject.Dispose();


            (_folderMonitor as IDisposable)?.Dispose();
        }
        _disposed = true;
    }
}