using Dimmer.Services;

namespace Dimmer.Orchestration;

public class PlayListMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IQueueManager<SongModelView> _queue;
    private readonly SubscriptionManager _subs;

    // cache the master song list too, if you ever need to build song‑based playlists
    private List<SongModel> _masterSongs = new();

    // playlist collection for UI
    public ObservableCollection<PlaylistModel> CurrentSetOfPlaylists { get; }
        = new();

    public PlayListMgtFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IQueueManager<SongModelView> queueManager,
        SubscriptionManager subs,
        IMapper mapper
    ) : base(state, songRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
        _playlistRepo = playlistRepo;
        _queue        = queueManager;
        _subs         = subs;

        // 1) prime the playlist list
        foreach (var pl in _playlistRepo.GetAll())
            CurrentSetOfPlaylists.Add(pl);

        // 2) keep your song cache up to date in case you want to queue from this flow too
        _subs.Add(
            _state.AllSongs
                  .Subscribe(list =>
                  {
                      _masterSongs = list.ToList();
                  })
        );

        // 3) react to playback events
        _subs.Add(
            _state.CurrentSong
                  .DistinctUntilChanged()
                  .Subscribe(_ => { OnPlaybackStateChanged(DimmerPlaybackState.Playing); })
        );
        _subs.Add(
            _state.CurrentPlayBackState
                  .DistinctUntilChanged()
                  .Subscribe(state => {
                      switch (state)
                      {
                          case DimmerPlaybackState.Opening:
                              break;
                          case DimmerPlaybackState.Stopped:
                              break;
                          case DimmerPlaybackState.Playing:
                              break;
                          case DimmerPlaybackState.Paused:
                              break;
                          case DimmerPlaybackState.Loading:
                              break;
                          case DimmerPlaybackState.Error:
                              break;
                          case DimmerPlaybackState.Failed:
                              break;
                          case DimmerPlaybackState.Previewing:
                              break;
                          case DimmerPlaybackState.LyricsLoad:
                              break;
                          case DimmerPlaybackState.ShowPlayBtn:
                              break;
                          case DimmerPlaybackState.ShowPauseBtn:
                              break;
                          case DimmerPlaybackState.RefreshStats:
                              break;
                          case DimmerPlaybackState.Initialized:
                              break;
                          case DimmerPlaybackState.Ended:
                              break;
                          case DimmerPlaybackState.CoverImageDownload:
                              break;
                          case DimmerPlaybackState.LoadingSongs:
                              break;
                          case DimmerPlaybackState.SyncingData:
                              break;
                          case DimmerPlaybackState.Buffering:
                              break;
                          case DimmerPlaybackState.DoneScanningData:
                              break;
                          case DimmerPlaybackState.PlayCompleted:
                              break;
                          case DimmerPlaybackState.PlayPrevious:
                              break;
                          case DimmerPlaybackState.PlayNext:
                              break;
                          case DimmerPlaybackState.Skipped:
                              break;
                          case DimmerPlaybackState.RepeatSame:
                              break;
                          case DimmerPlaybackState.RepeatAll:
                              break;
                          case DimmerPlaybackState.RepeatPlaylist:
                              break;
                          case DimmerPlaybackState.MoveToNextSongInQueue:
                              break;
                          default:
                              break;
                      }
                  })
        );
    }

    public void CreatePlaylistOfFiftySongs()
    {
        // example: build from cached _masterSongs
        var fifty = _masterSongs.Take(50).ToList();
        // ... your logic to save that as a PlaylistModel
    }

    private void OnPlaybackStateChanged(DimmerPlaybackState st)
    {
        switch (st)
        {
            case DimmerPlaybackState.Ended:
                AdvanceQueue();
                break;
            case DimmerPlaybackState.Skipped:
                InitializeQueue(CurrentlyPlayingSong);
                break;
        }
    }

    private void InitializeQueue(SongModelView start)
    {
        _queue.Initialize(
            items: _masterSongs
                    .Select(m => _mapper.Map<SongModelView>(m)),
            startIndex: _masterSongs
                    .FindIndex(m => m.LocalDeviceId == start.LocalDeviceId) + 1
        );
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Skipped);
    }

    private void AdvanceQueue()
    {
        var next = _mapper.Map<SongModel>(_queue.Next());

        if (next != null)
            _state.SetCurrentSong(next);
    }

    public void Dispose()
    {
        _subs.Dispose();
        base.Dispose();
    }
}
