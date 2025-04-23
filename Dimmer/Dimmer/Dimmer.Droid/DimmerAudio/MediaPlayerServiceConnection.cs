using Android.Content;
using Android.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio;
public class MediaPlayerServiceConnection : Java.Lang.Object, IServiceConnection
{
    readonly IAudioActivity activity;
    public MediaPlayerServiceConnection(IAudioActivity act) => activity = act;

    public void OnServiceConnected(ComponentName name, IBinder binder)
    {
        if (binder is ExoPlayerServiceBinder b)
        {
            activity.Binder = b;
            var svc = b.GetService();
            // wire events
            svc.StatusChanged   += activity.StatusChanged;
            svc.Buffering       += activity.Buffering;
            svc.Playing         += activity.Playing;
            svc.PlayingChanged  += activity.PlayingChanged;
            svc.PositionChanged += activity.PositionChanged;
            svc.CoverReloaded   += activity.CoverReloaded;
        }
    }
    public void OnServiceDisconnected(ComponentName name) { }
}