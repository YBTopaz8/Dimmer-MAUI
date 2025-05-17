using ATL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services;
public class FolderMgtService : IFolderMgtService
{
    private readonly IFolderMonitorService _folderMonitor;
    private readonly ISettingsService _settings;
    private bool _disposed; // To detect redundant calls
    public readonly IDimmerStateService _state;
    
    public FolderMgtService(
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IDimmerStateService state)
    {
        _state = state;
        _settings = settings;
        _folderMonitor = folderMonitor;

        _folderMonitor.OnCreated += OnFileCreated;
        _folderMonitor.OnDeleted += OnFileDeleted;
        _folderMonitor.OnChanged += OnFileChanged;
        _folderMonitor.OnRenamed += OnFileRenamed;

        // 2) folder‑watch
        _folderMonitor.Start(_settings.UserMusicFoldersPreference);
        _state.SetCurrentState((DimmerPlaybackState.FolderWatchStarted, null));
    }

    public IObservable<IReadOnlyList<FolderModel>> AllFolders => _allFolders.AsObservable();
    

    private readonly BehaviorSubject<IReadOnlyList<FolderModel>> _allFolders
         = new([]);


    private List<FolderModel> LoadFolderModels() =>
        _settings.UserMusicFoldersPreference
                 .Where(Directory.Exists)
                 .Select(p => new FolderModel
                 {
                     FolderPath        = p,
                     FolderName        = Path.GetFileName(p),
                     IsChecked   = true,
                     IsExpanded  = false,
                     IsSelected  = false
                 })
                 .ToList();

    private void RefreshFolders()
    {
        var models = LoadFolderModels();
        _allFolders.OnNext(models);
    }

    public void RestartWatching()
    {
        _folderMonitor.Stop();
        _folderMonitor.Start(_settings.UserMusicFoldersPreference);
        RefreshFolders();
    }

    // --- Public CRUD on the list of watched folders --------------------------------
    public void AddFolderToPreference(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);
        _settings.AddMusicFolder(path);
        _state.SetCurrentState((DimmerPlaybackState.FolderAdded, path));
        RestartWatching();
    }

    public void RemoveFolderFromPreference(string path)
    {
        _settings.RemoveMusicFolder(path);
        _state.SetCurrentState((DimmerPlaybackState.FolderRemoved, path));
        RestartWatching();
    }

    public void ClearAllFolders()
    {
        foreach (var p in _settings.UserMusicFoldersPreference.ToList())
            RemoveFolderFromPreference(p);
    }

    // --- FileSystemWatcher event handlers ----------------------------------------
    public void OnFileCreated(FileSystemEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderAdded, e.FullPath));
        RefreshFolders();
    }

    public void OnFileDeleted(FileSystemEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderRemoved, e.FullPath));
        RefreshFolders();
    }

    public void OnFileChanged(string fullPath)
    {
        _state.SetCurrentState((DimmerPlaybackState.FileChanged, fullPath));
    }

    public void OnFileRenamed(RenamedEventArgs e)
    {
        _state.SetCurrentState((DimmerPlaybackState.FolderNameChanged, e.FullPath));
        RefreshFolders();
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

        _folderMonitor.Start(_settings.UserMusicFoldersPreference);
        
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
