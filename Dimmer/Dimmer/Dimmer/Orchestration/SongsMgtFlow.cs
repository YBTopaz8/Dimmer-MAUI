using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions; // For ToModel, ToModelView
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
using ATL;
using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Orchestration;

public partial class SongsMgtFlow : IDisposable
{
    private readonly IDimmerStateService _state;
    private readonly IDimmerAudioService _audio;
    private readonly IMapper _mapper;
    private readonly IRepository<SongModel> songsRepo;
    private readonly ILogger<SongsMgtFlow> _logger;
    private readonly BaseAppFlow baseAppFlow;
    private readonly CompositeDisposable _subscriptions = new();
    private PlaybackStateInfo _currentGlobalPlaybackStateTracked = new(DimmerPlaybackState.Opening, null, null, null);

    private SongModelView? _currentGlobalSongViewTracked;
    public SongModelView? CurrentSongView => _currentGlobalSongViewTracked;
    public IObservable<bool> AudioEngineIsPlayingObservable { get; }
    public IObservable<double> AudioEnginePositionObservable { get; }
    public IObservable<double> AudioEngineVolumeObservable { get; }

    public SongsMgtFlow(
        IDimmerStateService state,
        IDimmerAudioService audioService,
        IMapper mapper,
        IRepository<SongModel> songsRepo,
        ILogger<SongsMgtFlow> logger
        ,
        BaseAppFlow baseAppFlow)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _audio = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.songsRepo=songsRepo;
        _logger = logger ?? NullLogger<SongsMgtFlow>.Instance;
        this.baseAppFlow=baseAppFlow;
        AudioEngineIsPlayingObservable = Observable.FromEventPattern<PlaybackEventArgs>(
                                             h => _audio.IsPlayingChanged += h,
                                             h => _audio.IsPlayingChanged -= h)
                                         .Select(evt => evt.EventArgs.IsPlaying)
                                         .StartWith(_audio.IsPlaying)
                                         .Replay(1).RefCount();

        AudioEnginePositionObservable = Observable.FromEventPattern<double>(
                                                h => _audio.PositionChanged += h,
                                                h => _audio.PositionChanged -= h)
                                            .Select(evt => evt.EventArgs)
                                            .StartWith(_audio.CurrentPosition)
                                            .Replay(1).RefCount();

        AudioEngineVolumeObservable = _state.DeviceVolume.StartWith(_audio.Volume).Replay(1).RefCount();


        InitializeSongMgtFlow(state);
        Debug.WriteLine("Done With Song Mgt Flow");
    }

    private void InitializeSongMgtFlow(IDimmerStateService state)
    {

        _subscriptions.Add(
            _state.CurrentPlayBackState
                .DistinctUntilChanged(psi => new { psi.State, SongViewId = psi.SongView?.Id }) // Use .SongView
                .SelectMany(async playbackStateInfo =>
                {
                    if (playbackStateInfo.Songdb is null || playbackStateInfo.SongView is null)
                    {
                        return System.Reactive.Unit.Default;
                    }
                    else if (_audio.CurrentTrackMetadata == playbackStateInfo.SongView)
                    {
                        return System.Reactive.Unit.Default;
                    }
                    await ControlAudioEnginePlaybackAsync(playbackStateInfo);
                    return System.Reactive.Unit.Default;
                })
                .Subscribe(_ => { }, ex => _logger.LogError(ex, "Error processing CurrentPlayBackState change."))
        );

        _subscriptions.Add(
            _state.DeviceVolume
                .DistinctUntilChanged()
                .Subscribe(volume =>
                {
                    if (Math.Abs(_audio.Volume - volume) > 0.001)
                    {
                        _logger.LogTrace("AudioEngine: Setting volume from global state to {Volume}", volume);
                        _audio.Volume = volume;
                    }
                }, ex => _logger.LogError(ex, "Error processing DeviceVolume change."))
        );

        _subscriptions.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audio.IsPlayingChanged += h, h => _audio.IsPlayingChanged -= h)
                .Subscribe(evt =>
                {
                    if (!isChangedAndPassedChangedCheck)
                    {

                        isChangedAndPassedChangedCheck=true;
                        ReportAudioEngineIsPlayingChanged(evt.EventArgs);
                    }
                },
                           ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription."))
        );

        _subscriptions.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audio.PlayEnded += h, h => _audio.PlayEnded -= h)
                .Subscribe(evt => ReportAudioEnginePlayEnded(evt.EventArgs),
                           ex => _logger.LogError(ex, "Error in PlayEnded subscription."))
        );

        _subscriptions.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audio.MediaKeyNextPressed += h, h => _audio.MediaKeyNextPressed -= h)
                .Subscribe(evt =>
                {
                    _logger.LogDebug("MediaKeyNextPressed received. Setting global state to PlayNextUser.");
                    _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayNextUser, null, evt.EventArgs.MediaSong, evt.EventArgs.MediaSong?.ToModel(_mapper)));
                }, ex => _logger.LogError(ex, "Error in MediaKeyNextPressed subscription."))
        );

        _subscriptions.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audio.MediaKeyPreviousPressed += h, h => _audio.MediaKeyPreviousPressed -= h)
                .Subscribe(evt =>
                {
                    _logger.LogDebug("MediaKeyPreviousPressed received. Setting global state to PlayPreviousUser.");
                    _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayPreviousUser, null, evt.EventArgs.MediaSong, evt.EventArgs.MediaSong?.ToModel(_mapper)));
                }, ex => _logger.LogError(ex, "Error in MediaKeyPreviousPressed subscription."))
        );

        _subscriptions.Add(
            Observable.FromEventPattern<double>(h => _audio.SeekCompleted += h, h => _audio.SeekCompleted -= h)
                .Subscribe(evt => _logger.LogTrace("AudioEngine: Seek completed to {PositionMs}ms.", evt.EventArgs),
                           ex => _logger.LogError(ex, "Error in SeekCompleted subscription."))
        );
        //_currentGlobalPlaybackStateTracked = state.CurrentPlayBackState.FirstAsync().Wait();
        _subscriptions.Add(
       state.CurrentPlayBackState
           .Subscribe(
               psi => _currentGlobalPlaybackStateTracked = psi,
               ex => _logger.LogError(ex, "Error in _state.CurrentPlayBackState tracker subscription.")
           )
   );
        // Avoid Task.Run here if SetDeviceVolume might interact with UI thread sensitive code in _state
        // Better to ensure _state setters are safe or use appropriate scheduler if needed
        _state.SetDeviceVolume(_audio.Volume);
        _logger.LogInformation("SongsMgtFlow (AudioServiceBridge) initialized.");
    }

    bool isChangedAndPassedChangedCheck;
    private async Task LoadAndPrepareSongInAudioEngineAsync(SongModelView? songViewFromState)
    {
        if (songViewFromState== null)
        {
            return;
        }
        SongModel? songModelToLoad = songViewFromState?.ToModel(_mapper);

        if (songModelToLoad == null)
        {

            _logger.LogInformation("Global current song is now null. Stopping audio engine.");


            return;
        }


        _logger.LogInformation("AudioEngine: New global current song '{SongTitle}'. Preparing to load.", songModelToLoad.Title);

        try
        {
            await _audio.InitializeAsync(songViewFromState!, songViewFromState?.ImageBytes);
            _logger.LogInformation("AudioEngine: Successfully initialized with '{SongTitle}'.", songModelToLoad.Title);

            var currentGlobalPlaybackState = await _state.CurrentPlayBackState.FirstAsync();
            if (currentGlobalPlaybackState.State == DimmerPlaybackState.Playing &&
                (currentGlobalPlaybackState.SongView?.Id == songViewFromState?.Id || currentGlobalPlaybackState.SongView == null))
            {
                _logger.LogInformation("AudioEngine: Global state is 'Playing' for '{SongTitle}', playing immediately after load.", songModelToLoad.Title);
                await _audio.PlayAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioEngine: Error initializing song '{SongTitle}'.", songModelToLoad.Title);
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Error, $"Init failed: {ex.Message}", songViewFromState, songModelToLoad));
        }
    }

    public async Task ControlAudioEnginePlaybackAsync(PlaybackStateInfo globalPlaybackState)
    {
        SongModel? songModelForCommand = globalPlaybackState.Songdb;
        SongModelView? songModelViewForCommand = globalPlaybackState.SongView;
        _currentGlobalSongViewTracked=songModelViewForCommand;

        //var cover = FileCoverImageProcessor.SaveOrGetCoverImageToFilePath(songModelViewForCommand.CoverImagePath);

        _logger.LogDebug("AudioEngine: Handling global state {Command} for song '{SongTitle}'", globalPlaybackState.State, songModelForCommand.Title);
        try
        {
            switch (globalPlaybackState.State)
            {
                case DimmerPlaybackState.PlaylistPlay:

                    await _audio.InitializeAsync(songModelViewForCommand!, null);

                    await _audio.PlayAsync();
                    break;
                case DimmerPlaybackState.Playing:
                case DimmerPlaybackState.Resumed:
                    //if (!_audio.IsPlaying && _audio.CurrentPosition==0)
                    //{
                    //    var trc = new Track(songModelViewForCommand.FilePath);

                    //    await _audio.InitializeAsync(songModelViewForCommand, trc.EmbeddedPictures[0].PictureData);

                    //    await _audio.PlayAsync();
                    //}
                    //if (_audio.CurrentTrackMetadata != globalPlaybackState.SongView)
                    //{
                    //    await _audio.InitializeAsync(globalPlaybackState.SongView);

                    //    await _audio.PlayAsync();
                    //}
                    //else
                    //{
                    //    await _audio.PlayAsync();

                    //}

                    break;
                case DimmerPlaybackState.PausedDimmer:
                    if (_audio.IsPlaying)
                        await _audio.PauseAsync();
                    break;
                case DimmerPlaybackState.PlayCompleted:
                    //await _audio.StopAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioEngine: Error executing command {Command} for '{SongTitle}'.", globalPlaybackState.State, songModelForCommand.Title);
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Error, $"Cmd failed: {ex.Message}", null, songModelForCommand));
        }
    }

    private void ReportAudioEngineIsPlayingChanged(PlaybackEventArgs args)
    {
        SongModelView? eventSongView = args.MediaSong;
        SongModel? eventSongModel = eventSongView?.ToModel(_mapper);
        SongModel? relevantSongModel = eventSongModel;

        if (relevantSongModel == null && !args.IsPlaying)
            return;

        // Use the tracked field here:
        PlaybackStateInfo currentGlobalPsiValue = _currentGlobalPlaybackStateTracked;
        DimmerPlaybackState newReportedState;

        if (args.IsPlaying)
        {
            newReportedState = DimmerPlaybackState.Playing;
        }
        else // Not playing
        {
            if (currentGlobalPsiValue.State == DimmerPlaybackState.Playing ||
                currentGlobalPsiValue.State == DimmerPlaybackState.Resumed ||
                currentGlobalPsiValue.State == DimmerPlaybackState.Loading)
            {
                newReportedState = DimmerPlaybackState.PausedDimmer;
            }
            else
            {
                newReportedState = currentGlobalPsiValue.State; // Keep existing non-playing state
            }
        }

        SongModelView? songViewForState = relevantSongModel?.ToModelView(_mapper);

        if (currentGlobalPsiValue.State != newReportedState || currentGlobalPsiValue.SongView?.Id != songViewForState?.Id)
        {
            _logger.LogDebug("AudioEngine: IsPlayingChanged event (IsPlaying: {IsEnginePlaying}). Global state changing from {OldGlobalState} to {NewGlobalState} for song {SongTitle}.",
                args.IsPlaying, currentGlobalPsiValue.State, newReportedState, relevantSongModel?.Title ?? "Unknown");
            // Pass the extra parameter from the original event if available and relevant, or null
            object? extraParamFromAudio = args.EventType; // Assuming PlaybackEventArgs has this
            _state.SetCurrentState(new PlaybackStateInfo(newReportedState, extraParamFromAudio, songViewForState, relevantSongModel));
        }
    }
    private void ReportAudioEnginePlayEnded(PlaybackEventArgs args)
    {
        SongModelView? endedSongView = args.MediaSong;
        SongModel? endedSongModel = endedSongView?.ToModel(_mapper);


        _logger.LogInformation("AudioEngine: PlayEnded event for song '{SongTitle}'.", endedSongModel.Title);

        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayCompleted, args.EventType, endedSongView, endedSongModel));
    }

    public async Task RequestSeekAsync(double positionSeconds)
    {
        if (_audio.IsPlaying)
        {
            await _audio.SeekAsync(positionSeconds);
        }
        else
        {
            _logger.LogWarning("AudioEngine: Seek requested but no track loaded/playing or audio service not ready.");
        }
    }

    public void RequestSetVolume(double volume)
    {
        double newVolume = Math.Clamp(volume, 0.0, 1.0);
        _logger.LogDebug("AudioEngine: UI Requesting SetVolume to {Volume}", newVolume);
        _state.SetDeviceVolume(newVolume);
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing SongsMgtFlow (AudioServiceBridge).");
        _subscriptions.Dispose();
    }
}