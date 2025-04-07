namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? MediaSong { get; set; }
    public bool IsPlaying { get; set; }  
}
