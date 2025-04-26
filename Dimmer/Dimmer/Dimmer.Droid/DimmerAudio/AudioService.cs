using Android.Media.Session;
using Android.OS;
using AndroidX.Media3.Common;
using Dimmer.Data.Models; // Assuming this namespace is correct
using Dimmer.Interfaces; // Assuming this namespace is correct for IDimmerAudioService
using Dimmer.Utilities.Events; // Assuming this namespace is correct for PlaybackEventArgs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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
    private SongModel? _currentSongModel; // Store context if needed

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
    public event EventHandler? PlayPrevious; // Can be triggered manually or via service events if implemented
    public event EventHandler? PlayNext;     // Can be triggered manually or via service events if implemented
    public event EventHandler<double>? PositionChanged; // Maps to OnPositionChanged (ms to s)
    public event EventHandler<double>? DurationChanged; // Triggered when metadata/duration updates
    public event EventHandler<double>? SeekCompleted; // Triggered after a seek operation completes
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred; // Triggered by player errors
    public event PropertyChangedEventHandler? PropertyChanged;


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
            // Subscribe to events from the *new* service instance
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

    public Task InitializeAsync(SongModel songModel, byte[]? songCoverImage)
    {
        _currentSongModel = songModel;
        // The actual preparation and playback is triggered by sending a command
        // to the ExoPlayerService, usually from the UI layer after connecting.
        // This method might just store context.
        // Alternatively, it could prepare the command bundle here.
        Console.WriteLine($"[AudioService] InitializeAsync called for: {songModel?.Title}. Ready for PREPARE_PLAY command.");
        return Task.CompletedTask;
    }

    public Task PlayAsync()
    {
            Service?.PrepareAndPlayAsync(_currentSongModel.FilePath,
                _currentSongModel.Title,_currentSongModel.ArtistName, _currentSongModel.AlbumName);

            //Player?.Play();
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
            // Note: Seek completion is often handled via PlaybackStateChanged or PositionDiscontinuity events
            // Raising SeekCompleted here might be premature. Best to raise it in OnPlaybackStateChanged when Ready after seek.
      
        return Task.CompletedTask;
    }

    public Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
    {
        // This is highly platform-specific and not directly available from ExoPlayer core.
        // Would require using Android AudioManager APIs.
        Console.WriteLine("[AudioService] GetAvailableAudioOutputsAsync - Not implemented for Android ExoPlayer directly.");
        return Task.FromResult(new List<AudioOutputDevice>()); // Return empty list
    }

    // --- IAudioActivity Implementation (Handles events FROM ExoPlayerService) ---

    // Binder property required by IAudioActivity interface
    ExoPlayerServiceBinder? IAudioActivity.Binder { get => _binder; set => SetBinder(value); }


    // Events required by IAudioActivity - We raise our own events, so these can be empty delegates,
    // but they must exist to satisfy the interface implementation when connecting via ServiceConnection.
    // The ServiceConnection will try to attach to these, even if this class is the one handling the source events.
    public event StatusChangedEventHandler? StatusChanged { add { } remove { } }
    public event BufferingEventHandler? Buffering { add { } remove { } }
    public event CoverReloadedEventHandler? CoverReloaded { add { } remove { } }
    public event PlayingEventHandler? Playing { add { } remove { } }
    public event PlayingChangedEventHandler? PlayingChanged { add { } remove { } } // ServiceConnection attaches here
    event PositionChangedEventHandler? IAudioActivity.PositionChanged { add { } remove { } } // ServiceConnection attaches here


    // These On... methods are the handlers called BY the ServiceConnection when the Service raises events.
    public void OnStatusChanged(object sender, EventArgs e)
    {
        // This requires the ExoPlayerService to pass state information in EventArgs or a custom EventArgs
        // For now, map basic states based on player properties when this is called.
        var playerState = Player?.PlaybackState ?? Player.PlaybackState;
        var isPlaying = Player?.IsPlaying ?? false;
        Console.WriteLine($"[AudioService] OnStatusChanged received. PlayerState: {playerState}, IsPlaying: {isPlaying}");

        
            // Update properties that might change with state
            NotifyPropertyChanged(nameof(IsPlaying));
            NotifyPropertyChanged(nameof(Duration)); // Duration might become available when Ready
        
        
    }

    public void OnBuffering(object sender, EventArgs e) // Assuming EventArgs for now
    {
        // Or (object sender, bool isBuffering) if service passes data
        Console.WriteLine($"[AudioService] OnBuffering received. Player Buffering: {Player?.IsLoading}");
       
    }

    public void OnCoverReloaded(object sender, EventArgs e)
    {
        Console.WriteLine("[AudioService] OnCoverReloaded received.");
        // TODO: Raise a specific event or update relevant property if UI needs notification
    }

    public void OnPlaying(object sender, EventArgs e)
    {
        // This event might be less useful than OnPlayingChanged or OnPositionChanged
        Console.WriteLine("[AudioService] OnPlaying event received (check if needed).");
    }

    public void OnPlayingChanged(object sender, bool isPlaying)
    {
        Console.WriteLine($"[AudioService] OnPlayingChanged received: IsPlaying={isPlaying}");
        NotifyPropertyChanged(nameof(IsPlaying));
        //IsPlayingChanged?.Invoke(this, new PlaybackEventArgs(isPlaying ? PlaybackState.Playing : PlaybackState.Paused));
        //PlaybackStateChanged?.Invoke(this, new PlaybackEventArgs(isPlaying ? PlaybackState.Playing : PlaybackState.Paused)); // Also reflect in general state
    }

    public void OnPositionChanged(object sender, long positionMs)
    {
        // No Console log here - too frequent
        double positionSeconds = positionMs / 1000.0;
        NotifyPropertyChanged(nameof(CurrentPosition));
        PositionChanged?.Invoke(this, positionSeconds);

        // Check if duration became available/changed
        // Note: A dedicated duration change event from the service might be better
        double newDuration = Player?.Duration > 0 ? (Player.Duration / 1000.0) : 0;
        // Simple check - needs refinement if duration fluctuates wrongly
        // if (Math.Abs(newDuration - _lastReportedDuration) > 0.1) {
        //    _lastReportedDuration = newDuration;
        //    NotifyPropertyChanged(nameof(Duration));
        //    DurationChanged?.Invoke(this, newDuration);
        // }

    }

    // --- Helper Methods ---

    private void ConnectEvents()
    {
        if (Service == null)
            return;
        // Subscribe to events coming *from* the ExoPlayerService
        Service.StatusChanged += OnStatusChanged;
        Service.Buffering += OnBuffering;
        Service.CoverReloaded += OnCoverReloaded;
        Service.Playing += OnPlaying;
        Service.PlayingChanged += OnPlayingChanged;
        Service.PositionChanged += OnPositionChanged;
        // TODO: Subscribe to an error event from the service if defined
        // Service.ErrorOccurred += OnServiceError;
    }

    private void DisconnectEvents()
    {
        if (Service == null)
            return;
        // Unsubscribe from events
        Service.StatusChanged -= OnStatusChanged;
        Service.Buffering -= OnBuffering;
        Service.CoverReloaded -= OnCoverReloaded;
        Service.Playing -= OnPlaying;
        Service.PlayingChanged -= OnPlayingChanged;
        Service.PositionChanged -= OnPositionChanged;
        // TODO: Unsubscribe from error event
        // Service.ErrorOccurred -= OnServiceError;
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