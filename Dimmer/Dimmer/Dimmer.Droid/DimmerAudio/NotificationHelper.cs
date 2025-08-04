using Android.App;
using Android.OS;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using Android;

namespace Dimmer.DimmerAudio;

public static class NotificationHelper
{
    public const string ChannelId = "dimmer_media_playback_channel";
    public const int NotificationId = 10899;
    public const int BubbleNotificationId = 1899;


    public static NotificationChannel? CreateChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return null;

        var notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
        if (notificationManager == null)
        {
            Log.Error("NotifHelper", "Failed to get NotificationManager service for channel creation.");
            return null;
        }


        var existingChannel = notificationManager.GetNotificationChannel(ChannelId);
        if (existingChannel != null)
        {
            bool needsUpdate = false;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q && !existingChannel.CanBubble())
            {




                Log.Info("NotifHelper", $"Channel '{ChannelId}' exists. CanBubble: {existingChannel.CanBubble()}");
                if (!existingChannel.CanBubble())
                {





                    Log.Warn("NotifHelper", $"Channel '{ChannelId}' exists but does not allow bubbles. User might have disabled it. Attempting to re-apply settings.");

                }
            }


        }


        var channelName = "Dimmer Playback";
        var channelDesc = "Media Playback Controls For Dimmer";
        var importance = NotificationImportance.Low;

        var chan = new NotificationChannel(ChannelId, channelName, importance)
        {
            Description = channelDesc
        };


        chan.SetAllowBubbles(true);
        chan.EnableLights(true);
        chan.LockscreenVisibility = NotificationVisibility.Public;
        chan.SetShowBadge(true);
        chan.SetBypassDnd(true);
        chan.EnableVibration(false);




        notificationManager.CreateNotificationChannel(chan);
        Log.Debug("NotifHelper", $"Channel '{ChannelId}' created/updated. SDK: {Build.VERSION.SdkInt}, Bubble support attempted: {Build.VERSION.SdkInt >= BuildVersionCodes.Q}");


        return null;
    }

    public static PlayerNotificationManager BuildManager(
        MediaSessionService service,
        MediaSession session)
    {
        CreateChannel(service);


        var pi = PendingIntent.GetActivity(
            service, 0,
            new Intent(service, typeof(MainActivity))
              .SetAction(Intent.ActionMain)
              .AddCategory(Intent.CategoryLauncher),
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );


        var descrAdapter = new DefaultMediaDescriptionAdapter(pi);


        PlayerNotificationManager mgr = new PlayerNotificationManager.Builder(
                service, NotificationId, ChannelId
            )
            .SetMediaDescriptionAdapter(descrAdapter)!
            .SetNotificationListener(new NotificationListener(service))!
            .SetSmallIconResourceId(Resource.Drawable.dimmicoo)!

            .Build()!;

        mgr.SetShowPlayButtonIfPlaybackIsSuppressed(true);
        mgr.SetMediaSessionToken(session.PlatformToken);
        mgr.SetUseFastForwardActionInCompactView(false);
        mgr.SetUsePreviousAction(true);
        mgr.SetUseNextActionInCompactView(true);
        mgr.SetUsePreviousActionInCompactView(true);
        mgr.SetUseNextAction(true);
        mgr.SetUseRewindActionInCompactView(false);

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
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    _svc.StopForeground(StopForegroundFlags.Detach);
                }
                else
                {
                    _svc.StopForeground(StopForegroundFlags.Detach);
                }

            }
            Log.Debug("NotifHelper", $"Posted id={notificationId} ongoing={ongoing}");
        }

        public void OnNotificationCancelled(int notificationId, bool dismissedByUser)
        {
            Log.Debug("NotifHelper", $"Notification cancelled: ID={notificationId}, DismissedByUser={dismissedByUser}");
            if (notificationId == BubbleNotificationId)
            {
                Log.Info("NotifHelper", "Bubble carrier notification cancelled.");

                return;
            }

            _svc.StopForeground(true);


        }



        //public void OnNotificationCancelled(int notificationId, bool dismissedByUser)
        //{









        //}
    }


    public static Notification BuildMinimalNotification(Context context)
    {
        CreateChannel(context);
        var builder = new Notification.Builder(context, ChannelId)!
            .SetContentTitle("Dimmer Music Player")!
            .SetContentText("Preparing playback...")!
            .SetSmallIcon(Resource.Drawable.dimmicoo)!
            .SetOngoing(true)!
            .SetPriority(0)!

            .SetVisibility(NotificationVisibility.Secret)!;

        return builder.Build();
    }
}