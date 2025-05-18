namespace Dimmer.Interfaces.Services;
public class FolderMgtService : IFolderMgtService
{
    private readonly IFolderMonitorService _folderMonitor;
    
    private bool _disposed; // To detect redundant calls
    public readonly IDimmerStateService _state;

    IRepository<AppStateModel> appStateModelRepo;
    public FolderMgtService(
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IDimmerStateService state,
        IRepository<AppStateModel> appStateModelRepo) // adding the new repository parameter
    {
        _state = state;
         
        _folderMonitor = folderMonitor;
        this.appStateModelRepo = appStateModelRepo;
        _folderMonitor.OnCreated += OnFileCreated;
        _folderMonitor.OnDeleted += OnFileDeleted;
        _folderMonitor.OnChanged += OnFileChanged;
        _folderMonitor.OnRenamed += OnFileRenamed;

    }

    public IObservable<IReadOnlyList<FolderModel>> AllFolders => _allFolders.AsObservable();
    

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFolders
         = new([]);

    AppStateModel CurrentAppState;
    private List<FolderModel> LoadFolderModels()
    {
        var s = appStateModelRepo.GetAll().ToList();
        if (s is null || s.Count<1 )
        {
            CurrentAppState = new AppStateModel();
            CurrentAppState.MinimizeToTrayPreference =true;
            appStateModelRepo.AddOrUpdate(CurrentAppState);
            return Enumerable.Empty<FolderModel>().ToList();

        }
        CurrentAppState = s[0];
        return CurrentAppState.UserMusicFoldersPreference.Select(x => new FolderModel()).ToList();
    }

    public void StartWatchingFolders()
    {
        var models = LoadFolderModels();
        _allFolders.OnNext(models);
    }

    public void RestartWatching()
    {
        _folderMonitor.Stop();
        _folderMonitor.Start(CurrentAppState.UserMusicFoldersPreference);
        StartWatchingFolders();
    }

    // --- Public CRUD on the list of watched folders --------------------------------
    public void AddFolderToPreference(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
        CurrentAppState.UserMusicFoldersPreference.Add(path);
        appStateModelRepo.AddOrUpdate(CurrentAppState);
        
        _state.SetCurrentState((DimmerPlaybackState.FolderAdded, path));
        RestartWatching();
    }

    public void RemoveFolderFromPreference(string path)
    {
        CurrentAppState.UserMusicFoldersPreference.Add(path);
        appStateModelRepo.AddOrUpdate(CurrentAppState);
        _state.SetCurrentState((DimmerPlaybackState.FolderRemoved, path));
        RestartWatching();
    }

    public void ClearAllFolders()
    {
        CurrentAppState.UserMusicFoldersPreference.Clear();
        appStateModelRepo.AddOrUpdate(CurrentAppState);

        _state.SetCurrentState((DimmerPlaybackState.FolderRemoved,null));

        
    }

    // --- FileSystemWatcher event handlers ----------------------------------------
    public void OnFileCreated(FileSystemEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderAdded, e.FullPath));
        StartWatchingFolders();
    }

    public void OnFileDeleted(FileSystemEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderRemoved, e.FullPath));
        StartWatchingFolders();
    }

    public void OnFileChanged(string fullPath)
    {
        _state.SetCurrentState((DimmerPlaybackState.FileChanged, fullPath));
    }

    public void OnFileRenamed(RenamedEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderNameChanged, e.FullPath));
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

        _folderMonitor.Start(CurrentAppState.UserMusicFoldersPreference);
        
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
