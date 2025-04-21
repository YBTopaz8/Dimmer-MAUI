using System.Reactive.Concurrency;

namespace Dimmer.Orchestration;

public class BaseAppFlow : IDisposable
{


    public readonly IPlayerStateService _state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly ISettingsService _settings;
    private readonly IFolderMonitorService _folderMonitor;
    public readonly IMapper _mapper;
    private bool _disposed;

    public SongModelView CurrentlyPlayingSong { get; set; } = new();
    public SongModel CurrentlyPlayingSongDB { get; set; } = new();

    public bool  IsShuffleOn
        => _settings.ShuffleOn;

    public RepeatMode CurrentRepeatMode
        => _settings.RepeatMode;

    public BaseAppFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IMapper mapper)
    {
        _state = state;
        _songRepo = songRepo;
        _pdlRepo = pdlRepo;
        _playlistRepo = playlistRepo;
        _artistRepo = artistRepo;
        _albumRepo = albumRepo;
        _settings = settings;
        _folderMonitor = folderMonitor;
        _mapper = mapper;

        Initialize();

    }
    public static IReadOnlyCollection<SongModel> MasterList { get; private set; }
    
    private IDisposable Initialize()
    {
        // 1) load once
        MasterList = [.. _songRepo
            .GetAll()            
            .OrderBy(x => x.DateCreated)];
        //_state.LoadAllSongs(MasterList);

        // 2) folder‑watch
        _folderMonitor.Start(_settings.UserMusicFoldersPreference);

        // 3) live updates, on UI‑thread if available
        var syncCtx = SynchronizationContext.Current;
        IScheduler scheduler = syncCtx != null
            ? new SynchronizationContextScheduler(syncCtx)
            : TaskPoolScheduler.Default;

        return _songRepo
            .WatchAll()
            .ObserveOn(scheduler)
            .DistinctUntilChanged()
            .Subscribe(list =>
            {
                if (list.Count == MasterList.Count)
                {
                    return;
                }
                MasterList = [.. list];
            });
        
    }


    public void PlaySong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Play);

    public void PauseSong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Pause);

    public void ResumeSong()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Resume);
    
    public void PlayEnded()
        => UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Completed);

    public void UpdatePlaybackState(
        string? songId,
        PlayType type,
        double? position = null)
    {
        if(string.IsNullOrEmpty(songId))
        {
            songId = CurrentlyPlayingSongDB.LocalDeviceId;
        }


        var link = new PlayDateAndCompletionStateSongLink
        {
            LocalDeviceId = Guid.NewGuid().ToString(),
            SongId = songId,
            PlayType = (int)type,
            DatePlayed = DateTime.Now,
            PositionInSeconds = position ?? 0,
            WasPlayCompleted = type == PlayType.Completed
        };

        _pdlRepo.AddOrUpdate(link);
    }


    public void UpsertPlaylist(PlaylistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _playlistRepo.AddOrUpdate(model);
    }

    public void UpsertArtist(ArtistModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _artistRepo.AddOrUpdate(model);
    }

    public void UpsertAlbum(AlbumModel model)
    {
        if (string.IsNullOrEmpty(model.LocalDeviceId))
            model.LocalDeviceId = Guid.NewGuid().ToString();
        _albumRepo.AddOrUpdate(model);
    }

    public void ToggleShuffle(bool isOn)
        => _settings.ShuffleOn = isOn;

    public RepeatMode ToggleRepeatMode()
    {
        var next = (RepeatMode)(((int)_settings.RepeatMode + 1) % 3);
        _settings.RepeatMode = next;
        return next;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _state.Dispose();
        _folderMonitor.Dispose();
        _disposed = true;
    }
}
