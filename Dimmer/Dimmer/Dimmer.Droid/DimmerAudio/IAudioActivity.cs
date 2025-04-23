using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio;

public delegate void StatusChangedEventHandler(object sender, EventArgs e);
public delegate void BufferingEventHandler(object sender, EventArgs e);
public delegate void CoverReloadedEventHandler(object sender, EventArgs e);
public delegate void PlayingEventHandler(object sender, EventArgs e);
public delegate void PlayingChangedEventHandler(object sender, bool isPlaying);
public delegate void PositionChangedEventHandler(object sender, long positionMs);

public interface IAudioActivity
{
    ExoPlayerServiceBinder Binder { get; set; }
    event StatusChangedEventHandler StatusChanged;
    event BufferingEventHandler Buffering;
    event CoverReloadedEventHandler CoverReloaded;
    event PlayingEventHandler Playing;
    event PlayingChangedEventHandler PlayingChanged;
    event PositionChangedEventHandler PositionChanged;
}