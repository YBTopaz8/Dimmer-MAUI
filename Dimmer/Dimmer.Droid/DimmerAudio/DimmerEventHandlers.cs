namespace Dimmer.DimmerAudio;

public delegate void StatusChangedEventHandler(object sender, PlaybackEventArgs e);
public delegate void BufferingEventHandler(object sender, EventArgs e);
public delegate void CoverReloadedEventHandler(object PlaybackEventArgs, EventArgs e);
public delegate void PlayingEventHandler(object sender, EventArgs e);
public delegate void PlayingChangedEventHandler(object sender, PlaybackEventArgs e);
public delegate void PositionChangedEventHandler(object sender, long position);
public delegate void SeekCompletedEventHandler(object sender, double position);
public delegate void PlayNextEventHandler(object sender, PlaybackEventArgs PreviousStateArgs);
public delegate void PlayPreviousEventHandler(object sender, PlaybackEventArgs PreviousStateArgs);

public static class MediaCommands
{
    // These values are from the official Android/Media3 documentation
    // and are stable.

    // See: https://developer.android.com/reference/androidx/media3/common/Player.Command

    /// <summary>Command to seek to the previous media item. Integer value: 8</summary>
    public const int CommandSeekToPreviousMediaItem = 8;

    /// <summary>Command to seek to the next media item. Integer value: 9</summary>
    public const int CommandSeekToNextMediaItem = 9;

    /// <summary>Command to seek to the previous timeline window. Integer value: 7</summary>
    public const int CommandSeekToPrevious = 7;

    /// <summary>Command to seek to the next timeline window. Integer value: 10</summary>
    public const int CommandSeekToNext = 10;

    /// <summary>Command to play or pause playback. Integer value: 1</summary>
    public const int CommandPlayPause = 1;
}