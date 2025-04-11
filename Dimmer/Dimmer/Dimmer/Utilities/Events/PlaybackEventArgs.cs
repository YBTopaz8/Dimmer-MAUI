namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? MediaSong { get; set; }
    public bool IsPlaying { get; set; } 
    public PlaybackEventType EventType { get; set; }
    public PlaybackEventArgs()
    {
        
    }
}

public enum PlaybackEventType
{
    None,
    Started,
    Stopped,
    Play,
    Pause,
    StoppedManually,
    StoppedAuto,
    Next,
    Previous,
    Seek
}