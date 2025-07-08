namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? MediaSong { get; set; }
    public bool IsPlaying { get; set; }
    public DimmerPlaybackState EventType { get; set; }
    public bool IsUseMyPlaylist { get; set; } = true;
    public PlaybackEventArgs(SongModelView song)
    {
        MediaSong = song;
    }
}
