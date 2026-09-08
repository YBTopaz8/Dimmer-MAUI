namespace Dimmer.DimmerAudio;

using Android.App;
using Android.Content;
using AndroidX.Media3.Session;

[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { Intent.ActionMediaButton })]
public partial class DimmerMediaButtonReceiver : MediaButtonReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        // This magic class provided by Google routes the intent directly into your MediaSessionCompat.Callback
        base.OnReceive(context, intent);
    }
}
