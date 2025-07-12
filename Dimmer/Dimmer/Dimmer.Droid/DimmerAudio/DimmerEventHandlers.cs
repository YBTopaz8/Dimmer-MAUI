using Dimmer.Utilities.Events;

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
