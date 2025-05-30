using Dimmer.Interfaces;
using Dimmer.Data.Models; // Assuming PlaybackStateInfo, SongModel, SongModelView are here or in sub-namespaces
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions; // For ToModel, ToModelView
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Dimmer.Orchestration;

public class SongsMgtFlow : IDisposable
{
    private readonly IDimmerStateService _state;
    private readonly IDimmerAudioService _audio;
    private readonly IMapper _mapper;
    private readonly ILogger<SongsMgtFlow> _logger;
    private readonly CompositeDisposable _subscriptions = new();
    private PlaybackStateInfo _currentGlobalPlaybackStateTracked = new(DimmerPlaybackState.Opening, null, null, null);
    private SongModel? _currentlyLoadedTrackInAudioEngine_SongModel; // Store as SongModel
    private SongModelView? _currentGlobalSongViewTracked;

    public IObservable<bool> AudioEngineIsPlayingObservable { get; }
    public IObservable<double> AudioEnginePositionObservable { get; }
    public IObservable<double> AudioEngineVolumeObservable { get; }

    public SongsMgtFlow(
        IDimmerStateService state,
        IDimmerAudioService audioService,
        IMapper mapper,
        ILogger<SongsMgtFlow> logger)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _audio = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? NullLogger<SongsMgtFlow>.Instance;

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

        _subscriptions.Add(
            _state.CurrentSong
                .Subscribe(songView => _currentGlobalSongViewTracked = songView,
                           ex => _logger.LogError(ex, "Error in _state.CurrentSong tracker subscription."))
        );

        _subscriptions.Add(
            _state.CurrentSong
                .DistinctUntilChanged(sv => sv?.Id)
                .ObserveOn(TaskPoolScheduler.Default)
                .SelectMany(async songView =>
                {
                    await LoadAndPrepareSongInAudioEngineAsync(songView);
                    return System.Reactive.Unit.Default;
                })
                .Subscribe(_ => { }, ex => _logger.LogError(ex, "Error processing CurrentSong change."))
        );

        _subscriptions.Add(
            _state.CurrentPlayBackState
                .DistinctUntilChanged(psi => new { psi.State, SongViewId = psi.SongView?.Id }) // Use .SongView
                .ObserveOn(TaskPoolScheduler.Default)
                .SelectMany(async playbackStateInfo =>
                {
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
                .Subscribe(evt => ReportAudioEngineIsPlayingChanged(evt.EventArgs),
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

    private async Task LoadAndPrepareSongInAudioEngineAsync(SongModelView? songViewFromState)
    {
        SongModel? songModelToLoad = songViewFromState?.ToModel(_mapper);

        if (songModelToLoad == null)
        {
            if (_currentlyLoadedTrackInAudioEngine_SongModel != null)
            {
                _logger.LogInformation("Global current song is now null. Stopping audio engine.");
                await _audio.StopAsync();
                _currentlyLoadedTrackInAudioEngine_SongModel = null;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(songModelToLoad.FilePath))
        {
            _logger.LogWarning("Song '{SongTitle}' (ID: {SongId}) from global state has no FilePath. Cannot play.", songModelToLoad.Title, songModelToLoad.Id);
            if (_currentlyLoadedTrackInAudioEngine_SongModel != null)
                await _audio.StopAsync();
            _currentlyLoadedTrackInAudioEngine_SongModel = null;
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Error, "Missing file path.", songViewFromState, songModelToLoad));
            return;
        }

        if (_audio.CurrentTrackMetadata?.Id == songViewFromState?.Id)
        {
            _logger.LogDebug("Song '{SongTitle}' is already the current track in audio engine. No reload needed.", songModelToLoad.Title);
            _currentlyLoadedTrackInAudioEngine_SongModel = songModelToLoad;
            return;
        }

        _logger.LogInformation("AudioEngine: New global current song '{SongTitle}'. Preparing to load.", songModelToLoad.Title);

        try
        {
            await _audio.InitializeAsync(songViewFromState!, songViewFromState?.ImageBytes);
            _currentlyLoadedTrackInAudioEngine_SongModel = songModelToLoad;
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
            _currentlyLoadedTrackInAudioEngine_SongModel = null;
        }
    }

    private async Task ControlAudioEnginePlaybackAsync(PlaybackStateInfo globalPlaybackState)
    {
        SongModelView? songViewForCommand = globalPlaybackState.SongView;
        SongModel? songModelForCommand = songViewForCommand?.ToModel(_mapper);


        if (songModelForCommand == null)
        {
            if (globalPlaybackState.State == DimmerPlaybackState.Playing || globalPlaybackState.State == DimmerPlaybackState.Resumed)
            {
                _logger.LogWarning("AudioEngine: Received command {Command} but no song context in PlaybackStateInfo.", globalPlaybackState.State);
                if (_audio.IsPlaying)
                    await _audio.StopAsync();
                _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Stopped, null, null, null));
            }
            return;
        }

        if (_currentlyLoadedTrackInAudioEngine_SongModel == null || _currentlyLoadedTrackInAudioEngine_SongModel.Id != songModelForCommand.Id)
        {
            _logger.LogDebug("AudioEngine: Playback command {Command} for '{ContextSongTitle}', but audio engine has '{LoadedSongTitle}'. Waiting for correct song to load.",
                globalPlaybackState.State, songModelForCommand.Title, _currentlyLoadedTrackInAudioEngine_SongModel?.Title ?? "nothing");
            return;
        }

        _logger.LogDebug("AudioEngine: Handling global state {Command} for song '{SongTitle}'", globalPlaybackState.State, songModelForCommand.Title);
        try
        {
            switch (globalPlaybackState.State)
            {
                case DimmerPlaybackState.Playing:
                case DimmerPlaybackState.Resumed:
                    if (!_audio.IsPlaying)
                    {
                        if (_audio.CurrentTrackMetadata?.Id == _currentlyLoadedTrackInAudioEngine_SongModel.ToModelView(_mapper)?.Id)
                        {
                            await _audio.PlayAsync();
                        }
                        else
                        {
                            _logger.LogWarning("AudioEngine: Play command for {SongTitle}, but audio engine's current track is {AudioEngineTrackTitle}. Mismatch.", _currentlyLoadedTrackInAudioEngine_SongModel.Title, _audio.CurrentTrackMetadata?.Title ?? "None");
                        }
                    }
                    break;
                case DimmerPlaybackState.PausedUI:
                    if (_audio.IsPlaying)
                        await _audio.PauseAsync();
                    break;
                case DimmerPlaybackState.Stopped:
                    if (_audio.IsPlaying || _currentlyLoadedTrackInAudioEngine_SongModel != null)
                        await _audio.StopAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AudioEngine: Error executing command {Command} for '{SongTitle}'.", globalPlaybackState.State, songModelForCommand.Title);
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Error, $"Cmd failed: {ex.Message}", songViewForCommand, songModelForCommand));
        }
    }

    private void ReportAudioEngineIsPlayingChanged(PlaybackEventArgs args)
    {
        SongModelView? eventSongView = args.MediaSong;
        SongModel? eventSongModel = eventSongView?.ToModel(_mapper);
        SongModel? relevantSongModel = eventSongModel ?? _currentlyLoadedTrackInAudioEngine_SongModel;

        if (relevantSongModel == null && args.IsPlaying)
        {
            _logger.LogWarning("AudioEngine: IsPlayingChanged to TRUE, but no track context. Forcing stop.");
            Task.Run(async () => await _audio.StopAsync());
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Stopped, null, null, null));
            return;
        }
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
                newReportedState = DimmerPlaybackState.PausedUI;
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
        SongModel? endedSongModel = endedSongView?.ToModel(_mapper) ?? _currentlyLoadedTrackInAudioEngine_SongModel;

        if (endedSongModel == null)
        {
            _logger.LogWarning("AudioEngine: PlayEnded event but no track context. Current global song: {GlobalSongTitle}", _currentGlobalSongViewTracked?.Title ?? "None");
            if (_currentGlobalSongViewTracked != null)
                _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Ended, null, _currentGlobalSongViewTracked, _currentGlobalSongViewTracked.ToModel(_mapper)));
            return;
        }

        _logger.LogInformation("AudioEngine: PlayEnded event for song '{SongTitle}'.", endedSongModel.Title);

        if (_currentGlobalSongViewTracked?.Id == endedSongView?.Id)
        {
            _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Ended, args.EventType, endedSongView, endedSongModel));
        }
        else
        {
            _logger.LogWarning("AudioEngine: PlayEnded for '{ActualEndedSongTitle}', but global current song is '{GlobalCurrentSongTitle}'. Suppressing Ended state.",
                endedSongModel.Title, _currentGlobalSongViewTracked?.Title ?? "None");
        }
    }

    public async Task RequestSeekAsync(double positionSeconds)
    {
        if (_currentlyLoadedTrackInAudioEngine_SongModel != null &&
            (_audio.CurrentTrackMetadata?.Id == _currentlyLoadedTrackInAudioEngine_SongModel.ToModelView(_mapper)?.Id || _audio.IsPlaying))
        {
            _logger.LogDebug("AudioEngine: UI Requesting Seek to {PositionSec}s for '{SongTitle}'", positionSeconds, _currentlyLoadedTrackInAudioEngine_SongModel.Title);
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