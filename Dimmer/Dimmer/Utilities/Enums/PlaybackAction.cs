namespace Dimmer.Utilities.Enums;

/// <summary>
/// Indicates the user's intent when interacting with a song.
/// This controls how the playback queue should be modified.
/// </summary>
public enum PlaybackAction
{
    /// <summary>
    /// Add the song to play immediately after the current song without interrupting playback.
    /// Insert at currentIndex + 1. This is the default tap behavior.
    /// </summary>
    PlayNext = 0,

    /// <summary>
    /// Stop current playback, clear the queue, and start playing the selected song and its context.
    /// This is typically triggered by long-press or explicit "Play Now" action.
    /// </summary>
    PlayNow = 1,

    /// <summary>
    /// Add the song to the end of the current playback queue.
    /// </summary>
    AddToQueue = 2,

    /// <summary>
    /// Jump to this song if it's already in the current queue (same context).
    /// Don't rebuild the queue, just change the current index.
    /// </summary>
    JumpInQueue = 3,

    /// <summary>
    /// Replace the entire queue with a new context but maintain the playback flow.
    /// Used when playing from a specific collection like an album or playlist.
    /// </summary>
    ReplaceQueue = 4
}
