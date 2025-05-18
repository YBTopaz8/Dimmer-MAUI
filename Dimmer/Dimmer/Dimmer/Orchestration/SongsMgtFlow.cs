using Dimmer.Utilities.Events;

namespace Dimmer.Orchestration;

public class SongsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _linkRepo;
    private readonly IDimmerAudioService _audio;
    private readonly SubscriptionManager _subs;

    // Exposed streams
    public IObservable<bool> IsPlaying { get; }
    public IObservable<double> Position { get; }
    public IObservable<double> Volume { get; }

    public SongsMgtFlow(
        IDimmerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<UserModel> userRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<AlbumArtistGenreSongLink> linkRepo,
        ISettingsService settings,
        IFolderMgtService folderMonitor,
        IDimmerAudioService audioService,
        IQueueManager<SongModelView> playQueue,
        SubscriptionManager subs,
        IRepository<AppStateModel> appstateRepo,
        IMapper mapper
    ) : base(state, songRepo, genreRepo, userRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo,appstateRepo, settings, folderMonitor, subs, mapper)
    {
        this.songRepo=songRepo;
        _audio  = audioService;
        _subs   = subs;

        // keep AllCurrentSongsList in sync with the global AllCurrentSongs stream

        _linkRepo=linkRepo;
        // Map audio‑service events into observables
        var playingChanged = Observable
            .FromEventPattern<PlaybackEventArgs>(
                h => _audio.IsPlayingChanged += h,
                h => _audio.IsPlayingChanged -= h)
            .Select(evt =>
            {
                return evt.EventArgs.IsPlaying;
            });

        var positionChanged = Observable
            .FromEventPattern<double>(
                h => _audio.PositionChanged += h,
                h => _audio.PositionChanged -= h)
            .Select(_ => _audio.CurrentPosition);

        IsPlaying = playingChanged.StartWith(_audio.IsPlaying);
        Position  = positionChanged.StartWith(_audio.CurrentPosition);
        Volume    = Observable.Return(_audio.Volume);
        _state.SetDeviceVolume(_audio.Volume);
        // Wire up play‑end/next/previous
        _audio.SeekCompleted += Audio_SeekCompleted;
        _audio.PlayEnded    += OnPlayEnded;
        _audio.MediaKeyNextPressed     += (_, e) => NextInQueue(e.EventType);
        _audio.MediaKeyPreviousPressed += (_, e) => PrevInQueue(e.EventType);

        // Auto‑play whenever CurrentSong changes
        _subs.Add(
            _state.CurrentPlayBackState
                  .Subscribe(async s =>
                  {
                      switch (s)
                      {
                          case (DimmerPlaybackState.Playing, null):
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
                              CurrentlyPlayingSong=s;
                          })
                );
    }

    public async Task PlaySongInAudioService()
    {
        if (string.IsNullOrWhiteSpace(CurrentlyPlayingSong.FilePath))
            return;

        var cover = PlayBackStaticUtils.GetCoverImage(CurrentlyPlayingSong.FilePath, true);
        CurrentlyPlayingSong.ImageBytes = cover;

        await _audio
            .InitializeAsync(CurrentlyPlayingSong, cover);

        await _audio.PlayAsync();

        PlaySong();  // BaseAppFlow: records Play link
    }

    private void OnPlayEnded(object? s, PlaybackEventArgs e)
    {
        PlayEnded();   // BaseAppFlow: records Completed link
        _state.SetCurrentState((DimmerPlaybackState.Ended,null));
        
    }

    public void NextInQueue(DimmerPlaybackState requester)
    {
        _state.SetCurrentState((requester, null));
        _state.SetCurrentState((DimmerPlaybackState.PlayNextUser, null));

        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Skipped);
    }

    public void PrevInQueue(DimmerPlaybackState requester)
    {
        _state.SetCurrentState((requester,null));
        UpdatePlaybackState(CurrentlyPlayingSong.LocalDeviceId, PlayType.Previous);
    }

    public async Task PauseResumeSongAsync(double position, bool isPause = false)
    {
        if (isPause )
        {
            await _audio.PauseAsync();
            _state.SetCurrentState((DimmerPlaybackState.PausedUI,null));
            AddPauseSongEventToDB();    // records Pause link
        }
        else
        {
            if(position < 1)
            {
                await PlaySongInAudioService();
                return;
            }
            await _audio.SeekAsync(position);
            await _audio.PlayAsync();
            _state.SetCurrentState((DimmerPlaybackState.Resumed,null));
            AddResumeSongToDB();   // records Resume link
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
    {
        _audio.Volume = Math.Clamp(newVolume, 0, 1);
        //_state.SetDeviceVolume(_audio.Volume);
    }

    public void IncreaseVolume()
    {
        ChangeVolume(_audio.Volume + 0.01);
    }

    public void DecreaseVolume()
    {
        ChangeVolume(_audio.Volume - 0.01);
    }

    public double VolumeLevel => _audio.Volume;

    public List<SongModel> GetSongsByAlbumId(string albumId)
    {
        // 1. Find all Song IDs linked to the given Album ID
        var songIdsInAlbum = _linkRepo.GetAll().AsEnumerable()
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

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        base.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subs.Dispose();
        }
    }
}
