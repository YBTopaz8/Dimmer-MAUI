namespace Dimmer_MAUI.Platforms.Android.MAudioLib;
public interface IAudioActivity
{

    public event StatusChangedEventHandler StatusChanged;

    public event CoverReloadedEventHandler CoverReloaded;

    public event PlayingEventHandler Playing;

    public event BufferingEventHandler Buffering;
}