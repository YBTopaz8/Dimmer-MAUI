using Dimmer.Utilities.Events;

namespace Dimmer.Interfaces;
public interface IDimmerAudioService
{

    ///<Summary>
    /// Pauses the currently initialized song.
    ///</Summary>
    Task PlayAsync();

    ///<Summary>
    /// Pauses the currently initialized song.
    ///</Summary>  
    Task PauseAsync();
    Task StopAsync();

    ///<Summary>
    /// Set AND PLAY the current playback position (in seconds).
    ///</Summary>    
    Task SeekAsync(double positionSeconds);
    Task InitializeAsync(SongModel songModel, byte[]? SongCoverImage=null);
    void InitializePlaylist(IEnumerable<SongModel> songModels);
    Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync();

    ///<Summary>
    /// Gets a value indicating whether the currently loaded audio file is playing.
    ///</Summary>
    bool IsPlaying { get; }

    ///<Summary>
    /// Gets the current position of audio playback in seconds.
    ///</Summary>
    double CurrentPosition { get; }

    ///<Summary>
    /// Gets the length of audio in seconds.
    ///</Summary>
    double Duration { get; }

    ///<Summary>
    /// Gets or sets the playback volume 0 to 1 where 0 is no-sound and 1 is full volume.
    ///</Summary>
    double Volume { get; set; }

    event EventHandler<PlaybackEventArgs> IsPlayingChanged;
    event EventHandler<PlaybackEventArgs> PlayEnded;
    event EventHandler PlayPrevious;
    event EventHandler PlayNext;
    event EventHandler<double>? PositionChanged;
    event EventHandler<double>? DurationChanged;
    event EventHandler<double>? SeekCompleted;
    event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    event EventHandler<PlaybackEventArgs>? ErrorOccurred;

    ValueTask DisposeAsync();   
    
}

public class AudioOutputDevice
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}