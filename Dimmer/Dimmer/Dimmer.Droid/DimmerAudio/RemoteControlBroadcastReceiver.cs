using Android.App;
using Android.Content;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio; 
[BroadcastReceiver(Exported = true)]
[IntentFilter(new[] { Intent.ActionMediaButton })]
public class RemoteControlBroadcastReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null)
            return;
        if (intent.Action != Intent.ActionMediaButton)
            return;

        // Use GetParcelableExtra and cast to KeyEvent for compatibility
        var key = intent.GetParcelableExtra(Intent.ExtraKeyEvent) as KeyEvent;
        if (key == null || key.Action != KeyEventActions.Down)
            return;

        //string? action = key.KeyCode switch
        //{
        //    Keycode.MediaPlayPause => ExoPlayerService.ActionPlay,
        //    Keycode.MediaPlay => ExoPlayerService.ActionPlay,
        //    Keycode.MediaPause => ExoPlayerService.ActionPause,
        //    Keycode.MediaNext => ExoPlayerService.ActionNext,
        //    Keycode.MediaPrevious => ExoPlayerService.ActionPrevious,
        //    _ => null
        //};

        //if (action != null)
        //    context.StartService(new Intent(context, typeof(ExoPlayerService)).SetAction(action));
    }
}
