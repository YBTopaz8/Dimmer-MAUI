using Dimmer.Utilities.Events;

namespace Dimmer.DimmerAudio;

public delegate void StatusChangedEventHandler(object sender, PlaybackEventArgs e);
public delegate void BufferingEventHandler(object sender, EventArgs e);
public delegate void CoverReloadedEventHandler(object PlaybackEventArgs, EventArgs e);
public delegate void PlayingEventHandler(object sender, EventArgs e);
public delegate void PlayingChangedEventHandler(object sender, PlaybackEventArgs e);
public delegate void PositionChangedEventHandler(object sender, long position);
public delegate void SeekCompletedEventHandler(object sender, double position);
public delegate void PlayNextEventHandler(object sender, PlaybackEventArgs PreviousStateArgs);
public delegate void PlayPreviousEventHandler(object sender, PlaybackEventArgs PreviousStateArgs);

/// <summary>
/// Defines the contract for an Activity or Component that interacts
/// with the audio playback service (ExoPlayerService).
/// It receives the service binder and handles events raised by the service.
/// </summary>
public interface IAudioActivity
{
    /// <summary>
    /// Gets or sets the binder received from the service connection.
    /// Allows the Activity to call methods on the service.
    /// </summary>
    ExoPlayerServiceBinder? Binder { get; set; }

    // --- Events that the Activity MUST implement handlers for ---
    // These events are raised by the Service (via the connection)
    // and handled by the Activity to update the UI etc.

    /// <summary>
    /// Handles the StatusChanged event from the service.
    /// </summary>
    void OnStatusChanged(object sender, EventArgs e);

    /// <summary>
    /// Handles the Buffering event from the service.
    /// </summary>
    void OnBuffering(object sender, EventArgs e); // Or (object sender, bool isBuffering)

    /// <summary>
    /// Handles the CoverReloaded event from the service.
    /// </summary>
    void OnCoverReloaded(object sender, EventArgs e); // Or (object sender, CoverArtEventArgs args)

    /// <summary>
    /// Handles the Playing event from the service.
    /// </summary>
    void OnPlaying(object sender, EventArgs e);

    /// <summary>
    /// Handles the PlayingChanged event from the service.
    /// </summary>
    /// <param name="isPlaying">True if playback is active, false otherwise.</param>
    void OnPlayingChanged(object sender, bool isPlaying);

    /// <summary>
    /// Handles the PositionChanged event from the service.
    /// </summary>
    /// <param name="position">Current playback position in seconds.</param>
    void OnPositionChanged(object sender, long position);
    void OnSeekCompleted(object sender, double position);
}


