namespace Dimmer_MAUI.Utilities.Models;
public enum MediaPlayerState
{
    Initialized,
    Playing,
    Paused,
    Stopped,
    Ended,
    LyricsLoad,
    CoverImageDownload,
    LoadingSongs,
    ShowPlayBtn,
    ShowPauseBtn,
    RefreshStats,

}
public class PlaybackInfo
{
    public double TimeElapsed { get; set; } = 0;
    public double CurrentTimeInSeconds { get; set; } = 0;
}