using Dimmer.Utilities.Events;

namespace Dimmer.Interfaces;
/// <summary>
/// 
/// </summary>
public interface IDimmerAudioService
{

    /// <summary>
    /// Plays the asynchronous.
    /// </summary>
    /// <returns></returns>
    /// <Summary>
    /// Pauses the currently initialized song.
    /// </Summary>
    Task PlayAsync();

    /// <summary>
    /// Pauses the asynchronous.
    /// </summary>
    /// <returns></returns>
    /// <Summary>
    /// Pauses the currently initialized song.
    /// </Summary>
    Task PauseAsync();
    /// <summary>
    /// Stops the asynchronous.
    /// </summary>
    /// <returns></returns>
    Task StopAsync();

    /// <summary>
    /// Seeks the asynchronous.
    /// </summary>
    /// <param name="positionSeconds">The position seconds.</param>
    /// <returns></returns>
    /// <Summary>
    /// Set AND PLAY the current playback position (in seconds).
    /// </Summary>
    Task SeekAsync(double positionSeconds);
    /// <summary>
    /// Initializes the asynchronous.
    /// </summary>
    /// <param name="songModel">The song model.</param>
    /// <param name="SongCoverImage">The song cover image.</param>
    /// <returns></returns>
    Task InitializeAsync(SongModelView songModel, byte[]? SongCoverImage=null);
    /// <summary>
    /// Initializes the playlist.
    /// </summary>
    /// <param name="songModels">The song models.</param>
    void InitializePlaylist(IEnumerable<SongModelView> songModels);
    /// <summary>
    /// Gets the available audio outputs asynchronous.
    /// </summary>
    /// <returns></returns>
    Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync();

    /// <summary>
    /// Gets a value indicating whether this instance is playing.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is playing; otherwise, <c>false</c>.
    /// </value>
    /// <Summary>
    /// Gets a value indicating whether the currently loaded audio file is playing.
    /// </Summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets the current position.
    /// </summary>
    /// <value>
    /// The current position.
    /// </value>
    /// <Summary>
    /// Gets the current position of audio playback in seconds.
    /// </Summary>
    double CurrentPosition { get; }

    /// <summary>
    /// Gets the duration.
    /// </summary>
    /// <value>
    /// The duration.
    /// </value>
    /// <Summary>
    /// Gets the length of audio in seconds.
    /// </Summary>
    double Duration { get; }

    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    /// <value>
    /// The volume.
    /// </value>
    /// <Summary>
    /// Gets or sets the playback volume 0 to 1 where 0 is no-sound and 1 is full volume.
    /// </Summary>
    double Volume { get; set; }

    /// <summary>
    /// Occurs when [is playing changed].
    /// </summary>
    event EventHandler<PlaybackEventArgs> IsPlayingChanged;
    /// <summary>
    /// Occurs when [play ended].
    /// </summary>
    event EventHandler<PlaybackEventArgs> PlayEnded;
    /// <summary>
    /// Occurs when [play previous].
    /// </summary>
    event EventHandler PlayPrevious;
    /// <summary>
    /// Occurs when [play next].
    /// </summary>
    event EventHandler PlayNext;
    /// <summary>
    /// Occurs when [position changed].
    /// </summary>
    event EventHandler<double>? PositionChanged;
    /// <summary>
    /// Occurs when [duration changed].
    /// </summary>
    event EventHandler<double>? DurationChanged;
    /// <summary>
    /// Occurs when [seek completed].
    /// </summary>
    event EventHandler<double>? SeekCompleted;
    /// <summary>
    /// Occurs when [playback state changed].
    /// </summary>
    event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    /// <summary>
    /// Occurs when [error occurred].
    /// </summary>
    event EventHandler<PlaybackEventArgs>? ErrorOccurred;

    /// <summary>
    /// Disposes the asynchronous.
    /// </summary>
    /// <returns></returns>
    ValueTask DisposeAsync();   
    
}

/// <summary>
/// 
/// </summary>
public class AudioOutputDevice
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    /// <value>
    /// The identifier.
    /// </value>
    public string? Id { get; set; }
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string? Name { get; set; }
}