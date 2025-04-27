using Dimmer.Services;
using Dimmer.Utilities.Events;

namespace Dimmer.Orchestration;

public class SongsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> linkRepo;
    private readonly IDimmerAudioService _audio;
    private readonly SubscriptionManager _subs;

    // cache of the master list
    private List<SongModel> _masterSongs ;

    // Exposed streams
    public IObservable<bool> IsPlaying { get; }
    public IObservable<double> Position { get; }
    public IObservable<double> Volume { get; }

    public SongsMgtFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<AlbumArtistGenreSongLink> linkRepo,
        ISettingsService settings,
        IFolderMonitorService folderMonitor,
        IDimmerAudioService audioService,
        IQueueManager<SongModelView> playQueue,
        SubscriptionManager subs,
        IMapper mapper
    ) : base(state, songRepo, genreRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
        this.songRepo=songRepo;
        _audio  = audioService;
        _subs   = subs;
        _masterSongs= new List<SongModel>();
        // keep AllCurrentSongsList in sync with the global AllCurrentSongs stream
        _subs.Add(
            _state.AllCurrentSongs
                  .Subscribe(list =>
                  {
                      // list is IReadOnlyList<SongModel>
                      _masterSongs = [.. list];
                  })
        );

        // Map audio‑service events into observables
        var playingChanged = Observable
            .FromEventPattern<PlaybackEventArgs>(
                h => _audio.IsPlayingChanged += h,
                h => _audio.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying);

        var positionChanged = Observable
            .FromEventPattern<double>(
                h => _audio.PositionChanged += h,
                h => _audio.PositionChanged -= h)
            .Select(_ => _audio.CurrentPosition);

        IsPlaying = playingChanged.StartWith(_audio.IsPlaying);
        Position  = positionChanged.StartWith(_audio.CurrentPosition);
        Volume    = Observable.Return(_audio.Volume);

        // Wire up play‑end/next/previous
        _audio.SeekCompleted += Audio_SeekCompleted;
        _audio.PlayEnded    += OnPlayEnded;
        _audio.PlayNext     += (_, _) => NextInQueue();
        _audio.PlayPrevious += (_, _) => PrevInQueue();

        // Auto‑play whenever CurrentSong changes
        _subs.Add(
            _state.CurrentPlayBackState
                  .Subscribe(async s =>
                  {
                      switch (s)
                      {
                          case DimmerPlaybackState.Playing:
                              await PlaySongInAudioService();
                              break;
                      }
                  })
        );
        SubscribeToCurrentSongChanges();
    }

    private void Audio_SeekCompleted(object? sender, double e)
    {
        SeekedTo(e);
    }

    private void SubscribeToCurrentSongChanges()
    {
        _subs.Add(
                    _state.CurrentSong
                    .DistinctUntilChanged()
                          .Subscribe(s =>
                          {
                              CurrentlyPlayingSongDB=s;
                          })
                );
    }

    public async Task PlaySongInAudioService()
    {
        if (string.IsNullOrWhiteSpace(CurrentlyPlayingSongDB.FilePath))
            return;

        var cover = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSongDB.FilePath, true);
        CurrentlyPlayingSong.ImageBytes = cover;

        await _audio
            .InitializeAsync(CurrentlyPlayingSongDB, cover);

        await _audio.PlayAsync();

        PlaySong();  // BaseAppFlow: records Play link
    }

    private void OnPlayEnded(object? s, PlaybackEventArgs e)
    {
        PlayEnded();   // BaseAppFlow: records Completed link
        _state.SetCurrentState(DimmerPlaybackState.Ended);
        _state.SetCurrentState(DimmerPlaybackState.Playing);
        
    }

    public void NextInQueue()
    {
        _state.SetCurrentState(DimmerPlaybackState.PlayNext);
        _state.SetCurrentState(DimmerPlaybackState.Playing);
    }

    public void PrevInQueue()
    {
        _state.SetCurrentState(DimmerPlaybackState.PlayPrevious);
        _state.SetCurrentState(DimmerPlaybackState.Playing);
    }

    public async Task PauseResumeSongAsync(double position, bool isPause = false)
    {
        if (isPause)
        {
            await _audio.PauseAsync();
            PauseSong();    // records Pause link
        }
        else
        {
            await _audio.SeekAsync(position);
            await _audio.PlayAsync();
            ResumeSong();   // records Resume link
        }
    }

    public async Task StopSongAsync()
    {
        await _audio.PauseAsync();
        CurrentlyPlayingSong.IsPlaying = false;
    }

    public async Task SeekTo(double position)
    {
        if (!_audio.IsPlaying)
            return;

        await _audio.SeekAsync(position);
        SeekedTo(position);
    }

    public void ChangeVolume(double newVolume)
        => _audio.Volume = Math.Clamp(newVolume, 0, 1);

    public void IncreaseVolume()
        => ChangeVolume(_audio.Volume + 0.01);

    public void DecreaseVolume()
        => ChangeVolume(_audio.Volume - 0.01);

    public double VolumeLevel => _audio.Volume;

    public List<SongModel> GetSongsByAlbumId(string albumId)
    {
        // 1. Find all Song IDs linked to the given Album ID
        var songIdsInAlbum = linkRepo.GetAll().AsEnumerable()
            .Where(l => l.AlbumId == albumId)
            .Select(l => l.SongId)
            .Distinct()
            .ToList(); // Materialize the list of IDs

        // 2. Retrieve the actual SongModel objects for those IDs
        // Check if _songRepo is directly accessible or needs casting/retrieval from base
        var songs = songRepo.GetAll().AsEnumerable() // Use the inherited song repository
                     .Where(s => songIdsInAlbum.Contains(s.LocalDeviceId))
                     .ToList();

        // 3. Optional: Sort songs by track number if available
        //    This often requires track number info on SongModel or the Link table
        //    Assuming SongModel has a TrackNumber property (might be string or int)
        songs = [.. songs.OrderBy(s => s.TrackNumber)]; // Example sorting

        return songs;
    }

    public void Dispose()
    {
        _subs.Dispose();
        base.Dispose();
    }
}
