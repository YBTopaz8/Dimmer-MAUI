using Android.Content;
using Android.OS;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;
public class MediaPlayerServiceConnection : Java.Lang.Object //, IServiceConnection
{
    public readonly IAudioActivity instance;

    public MediaPlayerServiceConnection(IAudioActivity mediaPlayer)
    {
        this.instance = mediaPlayer;
    }


    public void OnServiceDisconnected(ComponentName name)
    {

    }

}