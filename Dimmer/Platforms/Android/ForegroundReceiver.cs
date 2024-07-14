using Android.App;
using Android.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Platforms.Android;
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { "com.xamarin.action.ActionNotifTapped" })]
public class ForegroundReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        Intent startActivityIntent = new Intent(context, typeof(MainActivity));
        startActivityIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        context.StartActivity(startActivityIntent);
        
    }
}
