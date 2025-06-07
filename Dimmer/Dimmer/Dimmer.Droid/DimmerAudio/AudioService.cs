﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

using AndroidX.Media3.Common;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events; // Assuming this namespace is correct for PlaybackEventArgs

namespace Dimmer.DimmerAudio;

/// <summary>
/// Implementation of the cross-platform IDimmerAudioService interface,
/// acting as a wrapper around the Android-specific ExoPlayerService.
/// Requires connection via SetBinder.
/// </summary>
public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable, IAudioActivity
{
    private ExoPlayerServiceBinder? _binder;
    private ExoPlayerService? Service => _binder?.Service;
    private IPlayer? Player => Service?.GetPlayerInstance(); // Convenience accessor

    private bool _isInitialized = false; // Track if binder is set
    private SongModelView? _currentSongModel; // Store context if needed
    public SongModelView? CurrentTrackMetadata => _currentSongModel; // Expose current track metadata
    // --- IDimmerAudioService Implementation ---

    public bool IsPlaying => Player?.IsPlaying ?? false;

    public double CurrentPosition => (Player?.CurrentPosition ?? 0) / 1000.0; // Convert ms to seconds

    public double Duration => Player?.Duration > 0 ? (Player.Duration / 1000.0) : 0; // Convert ms to seconds, handle C.TIME_UNSET

    public double Volume
    {
        get => Player?.Volume ?? 1.0f; // ExoPlayer volume is 0.0f to 1.0f
        set
        {
            if (Player != null)
            {
                // Clamp value between 0.0 and 1.0
                Player.Volume = (float)Math.Max(0.0, Math.Min(1.0, value));
                NotifyPropertyChanged();
            }
        }
    }


    // Cross-platform events
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged; // Maps roughly to StatusChanged/PlayingChanged
    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayEnded; // Triggered by OnStatusChanged(Ended)
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed; // Can be triggered manually or via service events if implemented
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;     // Can be triggered manually or via service events if implemented
    public event EventHandler<double>? PositionChanged; // Maps to OnPositionChanged (ms to s)
    public event EventHandler<double>? DurationChanged; // Triggered when metadata/duration updates
    public event EventHandler<double>? SeekCompleted; // Triggered after a seek operation completes
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred; // Triggered by player errors
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<PlaybackEventArgs> PlayStarted;


    /// <summary>
    /// Connects this abstraction layer to the actual Android service instance.
    /// This MUST be called after the service connection is established.
    /// </summary>
    public void SetBinder(ExoPlayerServiceBinder? binder)
    {

        if (_binder == binder)
            return; // No change

        // Unsubscribe from previous service if any
        DisconnectEvents();

        _binder = binder;
        _isInitialized = _binder?.Service != null;

        if (_isInitialized && Service != null)
        {
            // SubscribeAsync to events from the *new* service instance
            ConnectEvents();
            Console.WriteLine("[AudioService] Binder set and events connected.");

            // Refresh initial state
            NotifyAllPropertiesChanged();
        }
        else
        {
            Console.WriteLine("[AudioService] Binder set to null or service not available.");
        }
    }

    // --- Methods mapping to Service Commands/Player Actions ---

    public void InitializePlaylist(IEnumerable<SongModelView> songModel)
    {

    }

    public List<AudioOutputDevice>? GetAllAudioDevices()
    {
        return  Service.GetAvailableAudioOutputMAUI();
        
    }
    public bool SetPreferredOutputDevice(AudioOutputDevice dev)
    {
        return Service.SetPreferredDevice(dev);
    }
    public Task InitializeAsync(SongModelView songModel, byte[]? songCoverImage)
    {
        _currentSongModel = songModel;

        Service?.Prepare(_currentSongModel.FilePath,
            _currentSongModel.Title, _currentSongModel.ArtistName, _currentSongModel.AlbumName, songModel);

        // The actual preparation and playback is triggered by sending a command
        // to the ExoPlayerService, usually from the UI layer after connecting.
        // This method might just store context.
        // Alternatively, it could prepare the command bundle here.
        Console.WriteLine($"[AudioService] InitializeAsync called for: {songModel?.Title}. Ready for PREPARE_PLAY command.");
        return Task.CompletedTask;
    }

    public Task PlayAsync()
    {
        Player?.Play();
        Console.WriteLine("[AudioService] Play command sent.");

        return Task.CompletedTask; // Android service calls are mostly async fire-and-forget
    }


    public Task PauseAsync()
    {
        Player?.Pause();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        Player?.Stop();
        Console.WriteLine("[AudioService] Stop command sent.");

        return Task.CompletedTask;
    }

    public Task SeekAsync(double positionSeconds)
    {
        long positionMs = (long)(positionSeconds * 1000.0);
        Player?.SeekTo(positionMs);
        Console.WriteLine($"[AudioService] Seek command sent to {positionMs}ms.");
        SeekCompleted?.Invoke(this, positionSeconds);

        return Task.CompletedTask;
    }

    public async Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
    {

        return null;
        //var s = await Service.GetAvailableAudioOutputs();
        //return Service?.GetAvailableAudioOutputs()!;

    }

    // --- IAudioActivity Implementation (Handles events FROM ExoPlayerService) ---

    // Binder property by IAudioActivity interface
    ExoPlayerServiceBinder? IAudioActivity.Binder { get => _binder; set => SetBinder(value); }

    public void OnStatusChanged(object sender, EventArgs e)
    {

        var playerState = Player?.PlaybackState ?? Player?.PlaybackState;
        var isPlaying = Player?.IsPlaying ?? false;
        Console.WriteLine($"[AudioService] OnStatusChanged received. PlayerState: {playerState}, IsPlaying: {isPlaying}");


        NotifyPropertyChanged(nameof(IsPlaying));
        NotifyPropertyChanged(nameof(Duration)); // Duration might become available when Ready


    }

    public void OnBuffering(object sender, EventArgs e) // Assuming EventArgs for now
    {

        Console.WriteLine($"[AudioService] OnBuffering received. Player Buffering: {Player?.IsLoading}");

    }


    public void OnCoverReloaded(object sender, EventArgs e)
    {
        Console.WriteLine("[AudioService] OnCoverReloaded received.");

    }

    public void OnPlaying(object sender, EventArgs e)
    {

        Console.WriteLine("[AudioService] OnPlaying event received (check if needed).");
    }

    public void OnPlayingChanged(object sender, bool isPlaying)
    {


    }

    public void OnPositionChanged(object sender, long position)
    {
        double positionSeconds = position / 1000.0;
        NotifyPropertyChanged(nameof(CurrentPosition));
        PositionChanged?.Invoke(this, positionSeconds);

    }
    public void OnSeekCompleted(object sender, double position)
    {
        double positionSeconds = position / 1000.0;
        NotifyPropertyChanged(nameof(CurrentPosition));
        SeekCompleted?.Invoke(this, positionSeconds);

    }

    // --- Helper Methods ---

    private void ConnectEvents()
    {
        if (Service == null)
            return;
        // SubscribeAsync to events coming *from* the ExoPlayerService
        Service.StatusChanged += OnStatusChanged;
        Service.Buffering += OnBuffering;
        Service.CoverReloaded += OnCoverReloaded;

        Service.PlayingChanged += Service_PlayingChanged;
        Service.PositionChanged += OnPositionChanged;
        Service.PlayNextPressed += Service_PlayNextPressed;
        Service.PlayPreviousPressed += Service_PlayPreviousPressed;

    }

    private void Service_PlayPreviousPressed(object sender, PlaybackEventArgs PreviousStateArgs)
    {
        MediaKeyPreviousPressed?.Invoke(this, PreviousStateArgs);
    }

    private void Service_PlayNextPressed(object sender, PlaybackEventArgs PreviousStateArgs)
    {
        var eventArgs = new PlaybackEventArgs(PreviousStateArgs.MediaSong) { EventType=DimmerPlaybackState.PlayNextUser };
        MediaKeyNextPressed?.Invoke(this, eventArgs);
    }

    public void Service_PlayingChanged(object sender, PlaybackEventArgs e)
    {

        IsPlayingChanged?.Invoke(this, new(_currentSongModel)
        {
            MediaSong=_currentSongModel,
            IsPlaying=e.IsPlaying
        });
        NotifyPropertyChanged(nameof(IsPlaying));
    }

    private void DisconnectEvents()
    {
        if (Service == null)
            return;
        // Unsubscribe from events
        Service.StatusChanged -= OnStatusChanged;
        Service.Buffering -= OnBuffering;
        Service.CoverReloaded -= OnCoverReloaded;

        Service.PlayingChanged -= Service_PlayingChanged;
        Service.PositionChanged -= OnPositionChanged;
        Service.PlayNextPressed -= Service_PlayNextPressed;
        Service.PlayPreviousPressed -= Service_PlayPreviousPressed;

    }

    private void NotifyAllPropertiesChanged()
    {
        NotifyPropertyChanged(nameof(IsPlaying));
        NotifyPropertyChanged(nameof(CurrentPosition));
        NotifyPropertyChanged(nameof(Duration));
        NotifyPropertyChanged(nameof(Volume));
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // --- IAsyncDisposable ---

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("[AudioService] DisposeAsync called.");
        DisconnectEvents();
        _binder = null; // Release binder reference
        // No native resources owned directly by this class, service handles player.
        await Task.CompletedTask; // Return completed task
    }

}