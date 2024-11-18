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
    public double CurrentPercentagePlayed { get; set; }
    public double CurrentTimeInSeconds { get; set; }
}