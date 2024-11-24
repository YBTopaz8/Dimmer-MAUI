namespace Dimmer_MAUI.MAudioLib;
public interface INativeAudioService
{
    public static INativeAudioService Current;
    
    void Initialize(SongModelView? media = null, byte[]? ImageBytes=null);
    Task PlayAsync(bool IsFromPreviousOrNext = false);

    Task PauseAsync();
    Task ResumeAsync(double positionInSeconds);
    ///<Summary>
    /// Set the current playback position (in seconds).
    ///</Summary>
    Task SetCurrentTime(double value);

    Task DisposeAsync();
    ///<Summary>
    /// Gets a value indicating whether the currently loaded audio file is playing.
    ///</Summary>
    bool IsPlaying { get;}
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
    /// <summary>
    /// Gets or sets the playback volume muted. false means not mute; true means mute.
    /// </summary>
    bool Muted { get; set; }

    ///<Summary>
    /// Gets or sets the balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right.
    ///</Summary>
    double Balance { get; set; }

    event EventHandler<bool> IsPlayingChanged;
    event EventHandler PlayEnded;
    event EventHandler PlayNext;
    event EventHandler PlayPrevious;
    event EventHandler<long> IsSeekedFromNotificationBar;
}