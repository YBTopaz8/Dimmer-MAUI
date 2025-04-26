using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Media3.Common;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using Android.Util;
using Android.Graphics;
using Dimmer.Droid;

namespace Dimmer.DimmerAudio;

public static class NotificationHelper
{
    public const string ChannelId = "dimmer_media_playback_channel";
    public const int NotificationId = 1001;

    public static void CreateChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var chan = new NotificationChannel(
            ChannelId,
            ctx.GetString(Resource.String.playback_channel_name),
            NotificationImportance.Low
        )
        { Description = ctx.GetString(Resource.String.playback_channel_desc) };
        chan.SetSound(null, null);
        chan.EnableLights(false);
        chan.EnableVibration(false);
        ((NotificationManager)ctx.GetSystemService(Context.NotificationService))!
            .CreateNotificationChannel(chan);

        Log.Debug("NotifHelper", "Channel created");
    }

    public static PlayerNotificationManager BuildManager(
        MediaSessionService service,
        MediaSession session)
    {
        CreateChannel(service);

        // PendingIntent to open your MainActivity
        var pi = PendingIntent.GetActivity(
            service, 0,
            new Intent(service, typeof(MainActivity))
              .SetAction(Intent.ActionMain)
              .AddCategory(Intent.CategoryLauncher),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );

        // Adapter for title/text/artwork
        var descrAdapter = new DefaultMediaDescriptionAdapter(pi);

        // Build & keep in a field
        var mgr = new PlayerNotificationManager.Builder(
                service, NotificationId, ChannelId
            )
            .SetMediaDescriptionAdapter(descrAdapter)!
            .SetNotificationListener(new NotificationListener(service))!
            .SetSmallIconResourceId(Resource.Drawable.exo_icon_circular_play)!            
           
            .Build();

        // Link session so transport-buttons work
        mgr.SetMediaSessionToken(session.SessionCompatToken);

        // Don’t call SetPlayer here — wait until after Prepare()
        // Return for caller to hang onto
        Log.Debug("NotifHelper", "Manager built");
        return mgr;
    }

    class NotificationListener : Java.Lang.Object, PlayerNotificationManager.INotificationListener
    {
        readonly MediaSessionService _svc;
        public NotificationListener(MediaSessionService svc) => _svc = svc;

        public void OnNotificationPosted(int notificationId, Notification? notification, bool ongoing)
        {
            if (ongoing)
                _svc.StartForeground(notificationId, notification);
            else
                _svc.StopForeground(false);
            Log.Debug("NotifHelper", $"Posted id={notificationId} ongoing={ongoing}");
        }

        public void OnNotificationCancelled(int id, bool dismissedByUser)
        {
            _svc.StopForeground(true);
            Log.Debug("NotifHelper", $"Cancelled id={id} userDismissed={dismissedByUser}");
        }
    }

    // You can keep the DefaultMediaDescriptionAdapter from Media3.UI,
    // or use the one shipped with the library.
}