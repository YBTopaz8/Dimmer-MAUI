namespace Dimmer.Interfaces.Services;
public class FolderMgtService : IFolderMgtService
{
    private readonly IFolderMonitorService _folderMonitor;
    
    private bool _disposed; // To detect redundant calls
    public readonly IDimmerStateService _state;
    ISettingsService settings;

    public FolderMgtService(
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IDimmerStateService state) // adding the new repository parameter
    {
        _state = state;
         
        _folderMonitor = folderMonitor;
        _folderMonitor.OnCreated += OnFileCreated;
        _folderMonitor.OnDeleted += OnFileDeleted;
        _folderMonitor.OnChanged += OnFileChanged;
        _folderMonitor.OnRenamed += OnFileRenamed;
        
        this.settings=settings;
    }

    public IObservable<IReadOnlyList<FolderModel>> AllFolders => _allFolders.AsObservable();
    

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFolders
         = new([]);


    public List<FolderModel>? StartWatchingFolders()
    {
        var e = BaseAppFlow.DimmerAppState.UserMusicFoldersPreference.Select(x => new FolderModel()
        { FolderPath = x});
        //_allFolders.OnNext(e);
        _folderMonitor.Start(e.Select(x=>x.FolderPath));
        return e.ToList();
    }

    public void RestartWatching()
    {
        _folderMonitor.Stop();
        _folderMonitor.Start(BaseAppFlow.DimmerAppState.UserMusicFoldersPreference);
        StartWatchingFolders();
    }

    // --- Public CRUD on the list of watched folders --------------------------------
    public void AddFolderToPreference(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
        
        
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, path));
        return;
        RestartWatching();
    }

    public void RemoveFolderFromPreference(string path)
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, path));
        RestartWatching();
    }

    public void ClearAllFolders()
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, null));

        
    }

    // --- FileSystemWatcher event handlers ----------------------------------------
    public void OnFileCreated(FileSystemEventArgs e)
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, e.FullPath));
        StartWatchingFolders();
    }

    public void OnFileDeleted(FileSystemEventArgs e)
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderRemoved, e.FullPath));
        StartWatchingFolders();
    }

    public void OnFileChanged(string fullPath)
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FileChanged, fullPath));
    }

    public void OnFileRenamed(RenamedEventArgs e)
    {
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderNameChanged, e.FullPath));
        StartWatchingFolders();
    }

    // --- Disposal ---------------------------------------------------------------
    public void Dispose()
    {
        if (_disposed)
            return;
        _folderMonitor.Stop();
        _folderMonitor.Dispose();
        _allFolders.Dispose();
        _disposed = true;
    }
    public void SetupFolderWatching()
    {
        
        _folderMonitor.Stop();
        _folderMonitor.Dispose();

        _folderMonitor.Start(BaseAppFlow.DimmerAppState.UserMusicFoldersPreference);
        
    }


    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed resources
            _allFolders.Dispose();
            _folderMonitor.Dispose();
        }

        // Dispose unmanaged resources if any

        _disposed = true;
    }

    ~FolderMgtService()
    {
        Dispose(false);
    }

    public void SetFolderChecked(string path, bool isChecked)
    {
        throw new NotImplementedException();
    }

    public void SetFolderExpanded(string path, bool isExpanded)
    {
        throw new NotImplementedException();
    }

    public void SetFolderName(string path, string name)
    {
        throw new NotImplementedException();
    }

    public void SetFolderPath(string path, string newPath)
    {
        throw new NotImplementedException();
    }

    public void SetFolderSelected(string path, bool isSelected)
    {
        throw new NotImplementedException();
    }
}
