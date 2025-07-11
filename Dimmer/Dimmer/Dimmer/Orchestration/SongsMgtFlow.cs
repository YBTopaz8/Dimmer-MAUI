using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions; // For ToModel, ToModelView
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
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

    }

    bool isChangedAndPassedChangedCheck;

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

                    _audio.Play();
                    baseAppFlow.UpdateDatabaseWithPlayEvent(songModelViewForCommand, PlayType.Play);

                    break;
                case DimmerPlaybackState.Playing:
                case DimmerPlaybackState.Resumed:
                    //if (!_audio.IsPlaying && _audio.CurrentPosition==0)
                    //{
                    //    var trc = new Track(songModelViewForCommand.FilePath);

                    //    await _audio.InitializeAsync(songModelViewForCommand, trc.EmbeddedPictures[0].PictureData);

                    //    _audio.Play();
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
        _subscriptions.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => _audio.PlayEnded += h, h => _audio.PlayEnded -= h)
                .Subscribe(evt => ReportAudioEnginePlayEnded(evt.EventArgs),
                           ex => _logger.LogError(ex, "Error in PlayEnded subscription."))
        );

        SongModelView? endedSongView = args.MediaSong;
        SongModel? endedSongModel = endedSongView?.ToModel(_mapper);


        _logger.LogInformation("AudioEngine: PlayEnded event for song '{SongTitle}'.", endedSongModel.Title);

        _state.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayCompleted, args.EventType, endedSongView, endedSongModel));
    }

    public void RequestSeek(double positionSeconds)
    {
        if (_audio.IsPlaying)
        {
            _audio.Seek(positionSeconds);
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

        _audio.Volume = newVolume; // Update audio service volume
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing SongsMgtFlow (AudioServiceBridge).");
        _subscriptions.Dispose();
    }
}