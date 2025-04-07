namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? MediaSong { get; set; }
    public bool IsPlaying { get; set; } 
    public PlaybackEventType EventType { get; set; }
    public PlaybackEventArgs(PlaybackEventType evtType)
    {
        EventType = evtType;
    }
}

public enum PlaybackEventType
{
    None,
    Play,
    Pause,
    StoppedManually,
    StoppedAuto,
    Next,
    Previous,
    Seek
}