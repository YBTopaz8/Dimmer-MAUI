namespace Dimmer.Models;

public enum MediaPlayerState
{
    Initialized,
    Playing,
    Paused,
    Stopped,
    LyricsLoad,
    CoverImageDownload,
    LoadingSongs
}
public class PlaybackInfo
{
    public double TimeElapsed { get; set; } = 0;
    public double CurrentTimeInSeconds { get; set; } = 0;
}