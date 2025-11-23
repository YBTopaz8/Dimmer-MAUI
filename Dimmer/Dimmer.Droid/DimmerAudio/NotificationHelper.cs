using Android.App;
using Android.OS;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Media3.Common;

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
        MediaSession session, SongModelView? song)
    {
        CreateChannel(service);

        var mainIntent = new Intent(service, typeof(TransitionActivity))
            .SetAction(Intent.ActionMain)
            .AddCategory(Intent.CategoryLauncher);


        var pi = PendingIntent.GetActivity(
            service, 0,
            mainIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );


        var customActionReceiver = new LyricsActionReceiver(service);

        var descrAdapter = new DefaultMediaDescriptionAdapter(pi);


        PlayerNotificationManager? mgr = new PlayerNotificationManager.Builder(
                service, NotificationId, ChannelId
            )
            .SetMediaDescriptionAdapter(descrAdapter)!
        .SetCustomActionReceiver(customActionReceiver)!
            .SetNotificationListener(new NotificationListener(service))!
            .SetSmallIconResourceId(Resource.Drawable.media_session_service_notification_ic_music_note)!

            .Build()!;

        mgr.SetShowPlayButtonIfPlaybackIsSuppressed(true);
        mgr.SetMediaSessionToken(session.PlatformToken);
        if (song != null)
        {
            mgr.SetUseChronometer(song.HasSyncedLyrics); // optional: show time counter if lyrics exist
        }


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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

    }


    public static Notification BuildMinimalNotification(Context context)
    {
        CreateChannel(context);
        var builder = new Notification.Builder(context, ChannelId)!
            .SetContentTitle("Dimmer Music Player")!
            .SetContentText("Preparing playback...")!
            .SetSmallIcon(Resource.Drawable.media_session_service_notification_ic_music_note)!
            .SetOngoing(true)!
            .SetPriority(0)!

            .SetVisibility(NotificationVisibility.Secret)!;

        return builder.Build();
    }
    class LyricsActionReceiver : Java.Lang.Object, PlayerNotificationManager.ICustomActionReceiver
    {
        private readonly Context _ctx;
        private const string ActionLyrics = "ACTION_LYRICS";
        private const string ServiceAction = "com.yvanbrunel.dimmer.ACTION_LYRICS";
        public LyricsActionReceiver(Context ctx) => _ctx = ctx;

        // List of actions this receiver supports
        public IList<string> CreateCustomActions(Context context, PlayerNotificationManager manager)
              => new List<string> { ActionLyrics };

        // Build the action for the notification
        public NotificationCompat.Action? GetCustomAction(Context context, string action)
        {
            if (action != ActionLyrics) return null;

            var intent = new Intent(context, typeof(ExoPlayerService))
                .SetAction("com.yvanbrunel.dimmer.ACTION_LYRICS");

            var pendingIntent = PendingIntent.GetService(context, 0, intent, PendingIntentFlags.Immutable);
            //var iconRes = Resource.Drawable.lyricist;

            return new NotificationCompat.Action(null, new Java.Lang.String("Lyrics"), pendingIntent);
        }

        // Handle when user taps the action
        public void OnCustomAction(IPlayer? player, string? action, Intent? intent)
        {
            if (action == ActionLyrics)
            {
                Toast.MakeText(_ctx, "Opening synced lyrics...", ToastLength.Short)?.Show();
                // TODO: trigger your overlay / lyrics view
            }
        }
        public IList<string>? GetCustomActions(IPlayer? player)
        => new List<string> { ActionLyrics };

        // Return a map of actionId -> Action
        public IDictionary<string, NotificationCompat.Action>? CreateCustomActions(Context? context, int instanceId)
        {
            if (context == null) return null;
            var act = BuildLyricsAction(context);
            return new Dictionary<string, NotificationCompat.Action> { { ActionLyrics, act } };
        }

        private static NotificationCompat.Action BuildLyricsAction(Context context)
        {
            var intent = new Intent(context, typeof(ExoPlayerService)).SetAction(ServiceAction);
            var pi = PendingIntent.GetService(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            return new NotificationCompat.Action(null, new Java.Lang.String ("Lyrics"), pi);
        }
    }


}