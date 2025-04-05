using Dimmer.Data.Models;
using Dimmer.Database.ModelView;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Utilities;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Dimmer.Orchestration;
public class BaseAppFlow : IDisposable
{


    #region FolderWatcher Region
    
    private readonly object _lock = new object(); // For thread safety
    private bool _isDisposed = false; // To prevent multiple disposals




    #endregion



    public IObservable<bool> IsPlaying=> _isPlayingSubj.AsObservable();
    BehaviorSubject<bool> _isPlayingSubj = new(false);
    System.Timers.Timer? _positionTimer;

    public static BehaviorSubject<SongModel> CurrentSong { get; } = new(new SongModel());

    public static BehaviorSubject<List<SongModel>> AllSongs { get; } = new([]);
    public static BehaviorSubject<List<AlbumArtistGenreSongLink>> AllLinks { get; } = new([]);
    public static BehaviorSubject<List<GenreModel>> AllGenre { get; } = new([]);
    public static BehaviorSubject<List<ArtistModel>> AllArtists{ get; } = new([]);
    public static BehaviorSubject<List<AlbumModel>> AllAlbums { get; } = new([]);

    

    public BaseAppFlow(Realm appDb, IDimmerAudioService dimmerAudioService)
    {
        Db = appDb;
        AudioService = dimmerAudioService;
        Initialize();
        LoadAppData();        
        SubscribeToCurrentSongChange();
        this.AudioService = dimmerAudioService;
        
        this.AudioService.PlayPrevious += AudioService_PlayPrevious;
        this.AudioService.PlayNext += AudioService_PlayNext;
        this.AudioService.IsPlayingChanged += AudioService_PlayingChanged;
        this.AudioService.PlayEnded += AudioService_PlayEnded;
        this.AudioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

        SetupFolderMonitoring();
    }

    void SetupFolderMonitoring()
    {
        List<string>? folds = AppSettingsService.MusicFoldersPreference.MusicFolders;
        if (folds == null || folds.Count == 0)
        {
            return;
        }
        foreach (var path in folds)
        {
            SingleFolderMonitor? newSingleFolderMonitor = new SingleFolderMonitor(path);
            newSingleFolderMonitor.StartMonitoring();
            
        }
    }
    #region Audio Service Event Handlers
    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        //currentPositionInSec = e/1000;
    }

    private void AudioService_PlayingChanged(object? sender, bool e)
    {
        _isPlayingSubj.OnNext(e);
        
        if (_isPlayingSubj.Value != e)
        {
            _isPlayingSubj.OnNext(e);
        }

        if (_isPlayingSubj.Value)
        {
            _positionTimer?.Start();
            
        }
        else
        {
            _positionTimer?.Stop();
            
        }
    }
    private void StopPositionTimer()
    {
        _positionTimer?.Stop();
    }
    private void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        StopPositionTimer();
        

    }


    private void AudioService_PlayNext(object? sender, EventArgs e)
    {
        
    }
    
    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        
    }
    #endregion

    private void LoadAppData()
    {
        var dbb = Db.All<SongModel>().OrderBy(x => x.DateCreated).ToList();
        AllSongs.OnNext(dbb);        
    }

    public void SubscribeToCurrentSongChange()
    {
        CurrentSong.Subscribe(song =>
        {
            
        });
    }

    public Realm Db { get; }
    public IDimmerAudioService AudioService { get; }

    public void Initialize()
    {
        Debug.WriteLine(Db.GetType());
    }
    private bool _disposed = false;

    // Other members...

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources.
                _positionTimer?.Dispose();
                _isPlayingSubj?.Dispose();
            }

            // Dispose unmanaged resources.
            // Set large fields to null.

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~BaseAppFlow()
    {
        Dispose(false);
    }
}

