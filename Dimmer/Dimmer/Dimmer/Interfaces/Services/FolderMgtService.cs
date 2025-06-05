// --- START OF FILE FolderMgtService.cs ---
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables; // For CompositeDisposable
// Add other necessary using statements
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Interfaces.Services; // Or your preferred namespace for service implementations

public class FolderMgtService : IFolderMgtService
{
    private readonly IDimmerStateService _state; // For signaling UI or other non-scan-triggering events
    private readonly ISettingsService _settingsService;
    private readonly IFolderMonitorService _folderMonitor;
    private readonly ILibraryScannerService _libraryScanner; // <<< NEW DEPENDENCY
    private readonly ILogger<FolderMgtService> _logger;   // <<< NEW DEPENDENCY
    private readonly ProcessingConfig _config; // To know supported audio extensions

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFoldersBehaviorSubject = new(Array.Empty<FolderModel>());
    private readonly CompositeDisposable _monitorSubscriptions = new();
    private bool _disposed;
    private bool _isCurrentlyWatching;


    public FolderMgtService(
        IDimmerStateService state,
        ISettingsService settingsService,
        IFolderMonitorService folderMonitor,
        ILibraryScannerService libraryScanner, // Injected
        ILogger<FolderMgtService> logger)     // Injected
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _folderMonitor = folderMonitor ?? throw new ArgumentNullException(nameof(folderMonitor));
        _libraryScanner = libraryScanner ?? throw new ArgumentNullException(nameof(libraryScanner));
        _config =  new ProcessingConfig();
        _logger = logger ?? NullLogger<FolderMgtService>.Instance;

        // No automatic subscriptions to _folderMonitor here.
        // StartWatchingConfiguredFolders will set them up.
    }

    public IObservable<IReadOnlyList<FolderModel>> AllWatchedFolders => _allFoldersBehaviorSubject.AsObservable();

    public void StartWatchingConfiguredFolders()
    {
        if (_isCurrentlyWatching)
        {
            StopWatching(); // Stop existing watchers before starting new ones
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

        // Unsubscribe from previous monitor events if any
        _monitorSubscriptions.Clear();

        // Subscribe to folder monitor events
        // Assuming IFolderMonitorService events are standard .NET events
        // If they are IObservables, use .Subscribe().DisposeWith(_monitorSubscriptions)
        _folderMonitor.OnCreated += HandleFileOrFolderCreated;
        ;
        _folderMonitor.OnDeleted += HandleFileOrFolderDeleted;
        _folderMonitor.OnChanged += HandleFileOrFolderChanged; // Note: Changed takes string
        _folderMonitor.OnRenamed += HandleFileOrFolderRenamed;

        _folderMonitor.Start(foldersToWatchPaths);
        _isCurrentlyWatching = true;
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderWatchStarted, null, null, null)); // Signal watch has started
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

        _monitorSubscriptions.Clear(); // If events were IObservables added to it
        _isCurrentlyWatching = false;
    }

    public async Task AddFolderToWatchListAndScanAsync(string path)
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

            StartWatchingConfiguredFolders();     // Refresh watchers with the new list
            _settingsService.AddMusicFolder(path); // Persist to settings
        }

        _logger.LogInformation("Adding folder to watch list and settings: {Path}", path);

        // Signal to the app that a folder preference was added (UI might react)
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, path, null, null));

        _logger.LogInformation("Triggering scan for newly added folder: {Path}", path);
        await _libraryScanner.ScanSpecificPathsAsync(new List<string> { path }, isIncremental: false);
    }

    public async Task RemoveFolderFromWatchListAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        bool removed = _settingsService.RemoveMusicFolder(path); // Persist to settings
        if (removed)
        {
            _logger.LogInformation("Removed folder from watch list and settings: {Path}", path);
            StartWatchingConfiguredFolders(); // Refresh watchers

            // Signal to the app that a folder preference was removed
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, path, null, null));

            // After removing a folder, trigger a full re-scan of *remaining* folders
            // to ensure library consistency (songs from removed folder are gone from DB/state).
            // ILibraryScannerService would need a way to know which songs to remove, or it
            // re-evaluates based on the current set of watched folders.
            _logger.LogInformation("Triggering library refresh after removing folder: {Path}", path);
            var remainingFolders = _settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>();
            await _libraryScanner.ScanLibraryAsync(remainingFolders);
        }
        else
        {
            _logger.LogWarning("Folder {Path} not found in watch list settings for removal.", path);
        }
    }

    public async Task ClearAllWatchedFoldersAndRescanAsync()
    {
        _logger.LogInformation("Clearing all watched folders and settings.");
        _settingsService.ClearAllFolders(); // Persist
        StartWatchingConfiguredFolders();   // Will stop monitoring as list is empty

        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, "ALL_FOLDERS_CLEARED", null, null));

        _logger.LogInformation("Triggering library scan with empty folder list (to clear library).");
        await _libraryScanner.ScanLibraryAsync(new List<string>()); // Scan with empty list
    }

    // --- FileSystemWatcher Event Handlers ---
    private async void HandleFileOrFolderCreated(FileSystemEventArgs e)
    {
        _logger.LogDebug("FS Event: Created - {FullPath}", e.FullPath);
        if (IsRelevantAudioFile(e.FullPath)) // Check if it's an audio file we care about
        {
            _logger.LogInformation("Relevant audio file created: {FilePath}. Triggering incremental scan of parent directory.", e.FullPath);
            // It's often better to scan the directory in case multiple files were added in quick succession
            // or if metadata relies on folder structure.
            await _libraryScanner.ScanSpecificPathsAsync(new List<string> { Path.GetDirectoryName(e.FullPath)! }, isIncremental: true);
        }
        else if (Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath)) // A new subfolder inside a watched folder
        {
            _logger.LogInformation("New directory created within watched scope: {FolderPath}. Triggering incremental scan.", e.FullPath);
            await _libraryScanner.ScanSpecificPathsAsync(new List<string> { e.FullPath }, isIncremental: true); // Scan the new folder
        }
    }

    private async void HandleFileOrFolderDeleted(FileSystemEventArgs e)
    {
        _logger.LogDebug("FS Event: Deleted - {FullPath}", e.FullPath);
        if (IsRelevantAudioFile(e.Name) || WasPathPreviouslyKnownAudio(e.FullPath)) // Check by extension or if we knew it was audio
        {
            _logger.LogInformation("Relevant audio file or known audio path deleted: {FilePath}. Triggering library refresh of parent.", e.FullPath);
            // Re-scan parent directory to update library (remove song, potentially update album/artist if they become empty)
            await _libraryScanner.ScanSpecificPathsAsync(new List<string> { Path.GetDirectoryName(e.FullPath)! }, isIncremental: true);
        }
        else if (WasPathPreviouslyWatchedSubfolder(e.FullPath)) // A subfolder we might have scanned was deleted
        {
            _logger.LogInformation("Watched sub-directory deleted: {FolderPath}. Triggering full library refresh.", e.FullPath);
            // Simplest is to rescan all configured folders.
            var currentFolders = _settingsService.UserMusicFoldersPreference?.ToList() ?? new List<string>();
            await _libraryScanner.ScanLibraryAsync(currentFolders);
        }
    }

    private async void HandleFileOrFolderChanged(string fullPath) // IFolderMonitorService OnChanged provides string
    {
        _logger.LogDebug("FS Event: Changed - {FullPath}", fullPath);
        if (IsRelevantAudioFile(fullPath)) // Only rescan if an audio file's metadata might have changed
        {
            _logger.LogInformation("Relevant audio file changed: {FilePath}. Triggering incremental scan of parent directory.", fullPath);
            await _libraryScanner.ScanSpecificPathsAsync(new List<string> { Path.GetDirectoryName(fullPath)! }, isIncremental: true);
            // Optionally, set a more specific state if UI needs to react to just a file change
            // _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FileChanged, fullPath, null, null));
        }
    }

    private async void HandleFileOrFolderRenamed(RenamedEventArgs e)
    {
        _logger.LogDebug("FS Event: Renamed - {OldFullPath} to {NewFullPath}", e.OldFullPath, e.FullPath);
        bool oldIsAudio = IsRelevantAudioFile(e.OldName) || WasPathPreviouslyKnownAudio(e.OldFullPath);
        bool newIsAudio = IsRelevantAudioFile(e.Name); // Name is new name

        if (oldIsAudio || newIsAudio) // If either old or new path relates to audio files
        {
            _logger.LogInformation("Relevant audio file/folder renamed: {OldPath} -> {NewPath}. Triggering scan of relevant directories.", e.OldFullPath, e.FullPath);
            var pathsToScan = new List<string>();
            if (Path.GetDirectoryName(e.OldFullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.OldFullPath)!);
            if (Path.GetDirectoryName(e.FullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.FullPath)!);

            await _libraryScanner.ScanSpecificPathsAsync([.. pathsToScan.Distinct()], isIncremental: true);
        }
        else if (Directory.Exists(e.FullPath) && IsPathWithinWatchedFolders(e.FullPath) || WasPathPreviouslyWatchedSubfolder(e.OldFullPath)) // A subfolder was renamed
        {
            _logger.LogInformation("Directory renamed within watched scope. Old: {OldPath}, New: {NewPath}. Triggering scan.", e.OldFullPath, e.FullPath);
            var pathsToScan = new List<string>();
            if (Path.GetDirectoryName(e.OldFullPath) != null)
                pathsToScan.Add(Path.GetDirectoryName(e.OldFullPath)!); // Scan old parent
            pathsToScan.Add(e.FullPath); // Scan new path itself
            if (Path.GetDirectoryName(e.FullPath) != null && Path.GetDirectoryName(e.FullPath) != e.FullPath)
                pathsToScan.Add(Path.GetDirectoryName(e.FullPath)!); // Scan new parent


            await _libraryScanner.ScanSpecificPathsAsync([.. pathsToScan.Distinct()], isIncremental: true);
        }
    }

    // Helper methods (conceptual)
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


    // --- Disposal ---
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
            StopWatching(); // This also clears _monitorSubscriptions if they were .NET events
            _monitorSubscriptions.Dispose(); // Explicitly dispose CompositeDisposable
            _allFoldersBehaviorSubject.Dispose();
            // _folderMonitor is managed by DI if registered as IDisposable, or needs manual disposal here if created by this class.
            // Your provided FolderMonitorService implements IDisposable.
            (_folderMonitor as IDisposable)?.Dispose();
        }
        _disposed = true;
    }
}