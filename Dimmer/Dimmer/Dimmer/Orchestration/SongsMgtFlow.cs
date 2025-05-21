using Dimmer.Utilities.Events;
using System.Diagnostics;

namespace Dimmer.Orchestration;

public class SongsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<SongModel> songRepo;
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
        IRepository<DimmerPlayEvent> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        ISettingsService settings,
        IFolderMgtService folderMonitor,
        IDimmerAudioService audioService,
        IQueueManager<SongModelView> playQueue,
        SubscriptionManager subs,
        IRepository<AppStateModel> appstateRepo,
        IMapper mapper
    ) : base(state, songRepo, genreRepo, userRepo,  pdlRepo, playlistRepo, artistRepo, albumRepo,appstateRepo, settings, folderMonitor, subs, mapper)
    {
        this.songRepo=songRepo;
        _audio  = audioService;
        _subs   = subs;

        // keep AllCurrentSongsList in sync with the global AllCurrentSongs stream

        // Map audio‑service events into observables
        var playingChanged = Observable
            .FromEventPattern<PlaybackEventArgs>(
                h => _audio.IsPlayingChanged += h,
                h => _audio.IsPlayingChanged -= h)
            .Select(evt =>
            {
                Debug.WriteLine(evt.EventArgs.MediaSong.Title);
                Debug.WriteLine(evt.EventArgs.IsPlaying);
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
            .DistinctUntilChanged()
            .SubscribeOn(SynchronizationContext.Current)
                  .Subscribe(async s =>
                  {
                      switch (s.State)
                      {
                          case DimmerPlaybackState.Playing:
                              await PlaySongInAudioService();
                              break;
                      }
                  })
        );
        SubscribeToCurrentSongChanges();
    }
    public void SetPlayState()
    {
        //this triggers the pl flow and song mgt flow
        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Playing, null));

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
        _state.SetCurrentState(new(DimmerPlaybackState.Ended,null));
        
    }

    public void NextInQueue(DimmerPlaybackState requester)
    {
        _state.SetCurrentState(new(requester, null));
        _state.SetCurrentState(new(DimmerPlaybackState.PlayNextUser, null));

        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Skipped);
    }

    public void PrevInQueue(DimmerPlaybackState requester)
    {
        _state.SetCurrentState(new(requester,null));
        UpdatePlaybackState(CurrentlyPlayingSong, PlayType.Previous);
    }

    public async Task PauseResumeSongAsync(double position, bool isPause = false)
    {
        if (isPause )
        {
            await _audio.PauseAsync();
            _state.SetCurrentState(new(DimmerPlaybackState.PausedUI,null));
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
            _state.SetCurrentState(new(DimmerPlaybackState.Resumed,null));
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

    public List<SongModel> GetSongsByAlbumId(ObjectId albumId)
    {
        throw new NotImplementedException("This method is not implemented yet.");
        // 1. Find all Song IDs linked to the given Album ID
        
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
