using Dimmer.Services;

namespace Dimmer.Orchestration;

public class PlayListMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IQueueManager<SongModel> _queue;
    private readonly SubscriptionManager _subs;
    
    // cache the master song list too, if you ever need to build song‑based playlists
    private IEnumerable<SongModel>? AllCurrentSongsList;

    // playlistSongs collection for UI
    public PlaylistModel CurrentPlaylist { get; }
    public ObservableCollection<PlaylistModel> CurrentSetOfPlaylists { get; }
        = new();

    public PlayListMgtFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IQueueManager<SongModel> queueManager,
        SubscriptionManager subs,
        IMapper mapper
    ) : base(state, songRepo, genreRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
        _playlistRepo = playlistRepo;
        _queue        = queueManager;
        _subs         = subs;

        // 1) prime the playlistSongs list
        foreach (var pl in _playlistRepo.GetAll())
            CurrentSetOfPlaylists.Add(pl);

        // 2) keep your song cache up to date in case you want to queue from this flow too
        _subs.Add(
            _state.AllCurrentSongs
                  .Subscribe((Action<IReadOnlyList<SongModel>>)(list =>
                  {
                      this.AllCurrentSongsList = list.ToList();
                  }))
        );
        _subs.Add(_state.CurrentPlaylist.Subscribe(_state
            =>
        {

        }));
        // 3) react to playback events
        _subs.Add(
            _state.CurrentSong
                  .DistinctUntilChanged()
                  .Subscribe(song=> 
                  {                      
                      CurrentlyPlayingSongDB = song;
                      
                  })
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
                              OnPlaybackStateChanged(DimmerPlaybackState.Playing);
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
                              OnPlaybackStateChanged(DimmerPlaybackState.Ended);
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
                              OnPlaybackStateChanged(DimmerPlaybackState.PlayPrevious);

                              break;
                          case DimmerPlaybackState.PlayNext:
                              OnPlaybackStateChanged(DimmerPlaybackState.PlayNext);
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


        _subs.Add(
            _state.CurrentPage.DistinctUntilChanged()
            .Subscribe(page =>
            {
                switch (page)
                {
                    case CurrentPage.HomePage:
                        break;
                    case CurrentPage.PlaylistsPage:
                        break;
                    case CurrentPage.SpecificAlbumPage:
                        break;
                    case CurrentPage.AllArtistsPage:
                        break;
                    case CurrentPage.AllAlbumsPage:
                        break;
                    default:
                        break;
                }
            }));

        
        _subs.Add(
            _state.AllCurrentSongs
                .DistinctUntilChanged()
                .Subscribe(playlistSongs =>
                {
                    if (playlistSongs == null || playlistSongs.Count<1)
                        return;
                    var source = playlistSongs.ToList();

                    var songIndex = source.FindIndex(s =>
             s.LocalDeviceId == CurrentlyPlayingSongDB!.LocalDeviceId);
                    if (songIndex < 0)
                        songIndex = 0; // fallback to start

                    // 3) initialize the queue at that position
                    _queue.Initialize(source, startIndex: songIndex);
                })
        );
    }

    public void CreatePlaylistOfFiftySongs()
    {        
        var fifty = AllCurrentSongsList.Take(50).ToList();        
    }

    private void OnPlaybackStateChanged(DimmerPlaybackState st)
    {
        switch (st)
        {
            case DimmerPlaybackState.PlayPrevious:
                PlayPreviousInQueue(); break;
            case DimmerPlaybackState.PlayNext:
            case DimmerPlaybackState.Ended:
                
                
                AdvanceQueue();
                break;
            case DimmerPlaybackState.Playing:
                
                break;
        }
    }

    private void InitializeQueueWithMastList()
    {
        if (AllCurrentSongsList == null)
            return;
        _queue.Initialize(items: AllCurrentSongsList);
        
    }

    private void AdvanceQueue()
    {
        var next = _mapper.Map<SongModel>(_queue.Next());

        if (next != null)
        {
            _state.SetCurrentSong(next);
            _state.SetSecondSelectdSong(next);
        }
            
    }
    private void PlayPreviousInQueue()
    {
        var prev = _mapper.Map<SongModel>(_queue.Previous());

        if (prev != null)
        {
            _state.SetCurrentSong(prev);
            _state.SetSecondSelectdSong(prev);
        }
    }

    public void Dispose()
    {
        _subs.Dispose();
        base.Dispose();
    }
}
