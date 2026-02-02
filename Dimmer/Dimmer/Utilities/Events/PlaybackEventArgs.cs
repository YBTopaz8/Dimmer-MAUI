namespace Dimmer.Utilities.Events;
public class PlaybackEventArgs : EventArgs
{
    public SongModelView? AudioServiceCurrentPlayingSongView { get; set; }
    public bool IsPlaying { get; set; }
    public DimmerPlaybackState EventType { get; set; }
    public bool IsUseMyPlaylist { get; set; } = true;
    public bool IsError { get; set; }
    
    public PlaybackEventArgs(SongModelView? song)
    {
        if (song is null) return;
        AudioServiceCurrentPlayingSongView = song;
    }
}
