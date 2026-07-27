using AndroidX.Core.App;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using Notification = Android.App.Notification;

namespace Dimmer.DimmerAudio;

public static class NotificationHelper
{
    public const string ChannelId = "dimmer_media_playback_channel";
    public const int NotificationId = 10899;



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
            


        }


        var channelName = "Dimmer Playback";
        var channelDesc = "Media Playback Controls For Dimmer";
        var importance = NotificationImportance.Low;

        var chan = new NotificationChannel(ChannelId, channelName, importance)
        {
            Description = channelDesc
        };

        chan.EnableLights(true);
        chan.LockscreenVisibility = NotificationVisibility.Public;
        chan.SetShowBadge(true);
        chan.SetBypassDnd(true);
        chan.EnableVibration(false);




        notificationManager.CreateNotificationChannel(chan);
      

        return chan;
    }

    public static PlayerNotificationManager BuildManager(
        MediaSessionService service,
        MediaSession session, SongModelView? song)
    {
        CreateChannel(service);

        var mainIntent = new Intent(service, typeof(MainActivity))
            .SetAction("ShowMiniPlayer")
            .AddCategory(Intent.CategoryLauncher);


        var pi = PendingIntent.GetActivity(
            service, 0,
            mainIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
        );


        var descrAdapter = new DefaultMediaDescriptionAdapter(pi);


        PlayerNotificationManager? mgr = new PlayerNotificationManager.Builder(
                service, NotificationId, ChannelId
            )
            .SetMediaDescriptionAdapter(descrAdapter)!
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
        mgr.SetUsePreviousAction(false);
        mgr.SetUseNextActionInCompactView(false);
        mgr.SetUsePreviousActionInCompactView(false);
        mgr.SetUseNextAction(false);
        mgr.SetUseRewindActionInCompactView(false);
        mgr.SetUseStopAction(false);


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
   

}