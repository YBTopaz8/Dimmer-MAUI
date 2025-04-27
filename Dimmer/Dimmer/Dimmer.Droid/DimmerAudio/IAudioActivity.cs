using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio;

public delegate void StatusChangedEventHandler(object sender, EventArgs e);
public delegate void BufferingEventHandler(object sender, EventArgs e);
public delegate void CoverReloadedEventHandler(object sender, EventArgs e);
public delegate void PlayingEventHandler(object sender, EventArgs e);
public delegate void PlayingChangedEventHandler(object sender, bool isPlaying);
public delegate void PositionChangedEventHandler(object sender, long positionMs);

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
	ExoPlayerServiceBinder? Binder { get;  set; }

	// --- Events that the Activity MUST implement handlers for ---
	// These events are raised by the Service (via the connection)
	// and handled by the Activity to update the UI etc.

	/// <summary>
	/// Fired when the general playback status changes (e.g., Idle, Ended).
	/// </summary>
	event StatusChangedEventHandler StatusChanged;
	/// <summary>
	/// Fired when the player enters or exits a buffering state.
	/// </summary>
	event BufferingEventHandler Buffering;
	/// <summary>
	/// Fired when the cover art associated with the current track changes.
	/// </summary>
	event CoverReloadedEventHandler CoverReloaded;
	/// <summary>
	/// Fired periodically during playback (or state change, confirm usage).
	/// </summary>
	event PlayingEventHandler Playing;
	/// <summary>
	/// Fired when playback starts or pauses.
	/// </summary>
	event PlayingChangedEventHandler PlayingChanged;
	/// <summary>
	/// Fired frequently during playback to report the current position.
	/// </summary>
	event PositionChangedEventHandler PositionChanged;

	// --- Methods that the Activity MUST implement ---
	// These are the handler implementations for the events above.

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
	/// <param name="positionMs">Current playback position in milliseconds.</param>
	void OnPositionChanged(object sender, long positionMs);
}


