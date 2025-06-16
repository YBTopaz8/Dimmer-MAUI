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

    //public static void CreateChannel(Context ctx)
    //{
    //    if (Build.VERSION.SdkInt < BuildVersionCodes.O)
    //        return;

    //    var chan = new NotificationChannel(
    //        ChannelId,
    //        ctx.GetString(Resource.String.playback_channel_name),
    //        NotificationImportance.Low
    //    )
    //    { 

    //        Description = ctx.GetString(Resource.String.playback_channel_desc) 
    //    };
    //    chan.Importance = NotificationImportance.Low; // Set to Low for background playback

    //    chan.EnableLights(true);
    //    chan.LockscreenVisibility = NotificationVisibility.Public;
    //    chan.SetAllowBubbles(true); // Allow bubbles on this channel
    //    chan.SetShowBadge(true); // Show badge on app icon
    //    chan.SetBypassDnd(true); // Bypass Do Not Disturb
    //    chan.EnableVibration(false);


    //    // *** ADD THIS LINE FOR BUBBLES (Requires API 29+) ***
    //    if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
    //    {
    //        chan.SetAllowBubbles(true); // Allow notifications on this channel to bubble
    //    }


    //    var notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;
    //    if (notificationManager != null)
    //    {
    //        notificationManager.CreateNotificationChannel(chan);
    //        Log.Debug("NotifHelper", "Channel created/updated (Bubble support potentially enabled)");
    //        Console.WriteLine("NotifHelper : Channel created/updated (Bubble support potentially enabled)");
    //    }
    //    else
    //    {
    //        Log.Error("NotifHelper", "Failed to get NotificationManager service.");
    //    }
    //}


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

        // Check if channel already exists with correct settings
        var existingChannel = notificationManager.GetNotificationChannel(ChannelId);
        if (existingChannel != null)
        {
            bool needsUpdate = false;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q && !existingChannel.CanBubble())
            {
                // If bubbles are supported on device and channel doesn't allow them,
                // it might have been disabled by user or old config.
                // Deleting and recreating can sometimes fix this, but be mindful of user's custom settings.
                // For now, let's just log if it can't bubble.
                Log.Info("NotifHelper", $"Channel '{ChannelId}' exists. CanBubble: {existingChannel.CanBubble()}");
                if (!existingChannel.CanBubble())
                {
                    // You *could* delete and recreate the channel if CanBubble is false,
                    // but this will reset any user customizations for that channel.
                    // notificationManager.DeleteNotificationChannel(ChannelId);
                    // existingChannel = null; // Force recreation
                    // needsUpdate = true; // Or simply try to update if deletion is too aggressive
                    Log.Warn("NotifHelper", $"Channel '{ChannelId}' exists but does not allow bubbles. User might have disabled it. Attempting to re-apply settings.");
                    // It's often better to guide the user to re-enable it in settings if they disabled it.
                }
            }
            // You could add more checks here if other channel properties need to be enforced.
            // For now, we will always proceed to create/update.
        }


        var channelName = ctx.GetString(Resource.String.playback_channel_name);
        var channelDesc = ctx.GetString(Resource.String.playback_channel_desc);
        var importance = NotificationImportance.Low; // Low is fine for media that can bubble

        var chan = new NotificationChannel(ChannelId, channelName, importance)
        {
            Description = channelDesc
        };

        // Common settings
        chan.SetAllowBubbles(true);
        chan.EnableLights(false); // Media notifications usually don't need lights
        chan.LockscreenVisibility = NotificationVisibility.Public;
        chan.SetShowBadge(true);
        chan.SetBypassDnd(true); // Usually media shouldn't bypass DND unless critical
        chan.EnableVibration(false); // Vibration handled by media player usually

        // Bubble specific setting
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            chan.SetAllowBubbles(true); // CRITICAL for bubbles
            Log.Info("NotifHelper", $"Channel '{ChannelId}' setting AllowBubbles to true.");
        }

        notificationManager.CreateNotificationChannel(chan);
        Log.Debug("NotifHelper", $"Channel '{ChannelId}' created/updated. SDK: {Build.VERSION.SdkInt}, Bubble support attempted: {Build.VERSION.SdkInt >= BuildVersionCodes.Q}");

        // Re-check channel after creation/update
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            var updatedChannel = notificationManager.GetNotificationChannel(ChannelId);
            if (updatedChannel != null)
            {
                updatedChannel.SetAllowBubbles(true);
                Log.Info("NotifHelper", $"After Create/Update - Channel '{ChannelId}' CanBubble: {updatedChannel.CanBubble()}");
            }
            else
            {
                Log.Error("NotifHelper", $"After Create/Update - Channel '{ChannelId}' is somehow null!");
            }

            return updatedChannel;
        }

        return null;
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
        PlayerNotificationManager mgr = new PlayerNotificationManager.Builder(
                service, NotificationId, ChannelId
            )
            .SetMediaDescriptionAdapter(descrAdapter)!
            .SetNotificationListener(new NotificationListener(service))!
            .SetSmallIconResourceId(Resource.Drawable.atom)!

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
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 33
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
                // Optionally, clean up bubble state if needed
                return;
            }
            // This is for the main media notification
            _svc.StopForeground(true); // True is equivalent to StopForegroundFlags.Remove on newer APIs
                                       // Consider if you want to stop the service here or just remove notification
                                       // if (_svc.IsPlaybackStoppedCompletely()) { _svc.StopSelf(); }
        }



        //public void OnNotificationCancelled(int notificationId, bool dismissedByUser)
        //{
        //    if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 33
        //    {
        //        _svc.StopForeground(StopForegroundFlags.Remove);
        //    }
        //    else
        //    {
        //        _svc.StopForeground(StopForegroundFlags.Detach);
        //    }
        //    Log.Debug("NotifHelper", $"Cancelled id={notificationId} userDismissed={dismissedByUser}");
        //}
    }
    public static bool AreBubblesSupported(Context context)
    {
        Log.Info("NotifHelper_SupportCheck", $"--- Checking Bubble Support --- API: {Build.VERSION.SdkInt}");
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q) // API 29
        {
            Log.Info("NotifHelper", "AreBubblesSupported: False (SDK < Q)");
            //return false;
        }

        // On API 33+, check POST_NOTIFICATIONS first
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33
        {
            if (context.CheckSelfPermission(Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
            {
                Log.Warn("NotifHelper", "AreBubblesSupported: False (POST_NOTIFICATIONS permission not granted on API 33+)");
                //return false;
            }
        }

        try
        {
            NotificationManager notificationManager = NotificationManager.FromContext(context);
            if (notificationManager == null)
            {
                Log.Error("NotifHelper", "AreBubblesSupported: False (NotificationManager is null)");
                return false;
            }

            // Check global setting for bubbles
            bool areBubblesGloballyEnabled = notificationManager.AreBubblesAllowed();
            if (!areBubblesGloballyEnabled)
            {
                // This could be due to Developer Options > Bubbles being OFF,
                // or the user disabled bubbles for your app in App Info > Notifications.
                Log.Warn("NotifHelper", "AreBubblesSupported: False (Bubbles are not globally allowed for this app by the system/user). Check Developer Options and App Notification Settings.");
                // You might want to call OpenBubbleSettings(context) here or prompt the user.
            }

            // Check if OUR channel exists and allows bubbles
            var channel = notificationManager.GetNotificationChannel(ChannelId);
            bool channelExists = channel != null;
            bool channelAllowsBubbles = channel?.CanBubble() ?? false;

            Log.Info("NotifHelper", $"AreBubblesSupported: API={Build.VERSION.SdkInt}, GlobalEnabled={areBubblesGloballyEnabled}, ChannelExists={channelExists}, ChannelAllowsBubbles={channelAllowsBubbles} (Channel ID: {ChannelId})");

            if (!channelExists)
            {
                Log.Error("NotifHelper", $"AreBubblesSupported: False (Channel '{ChannelId}' does not exist!). Make sure CreateChannel is called BEFORE this check.");
                // Attempt to create it defensively, though it should have been created.
                CreateChannel(context);
                channel = notificationManager.GetNotificationChannel(ChannelId);
                channelAllowsBubbles = channel?.CanBubble() ?? false;
                Log.Info("NotifHelper", $"After defensive channel creation: ChannelExists={channel != null}, ChannelAllowsBubbles={channelAllowsBubbles}");
            }
            else if (!channelAllowsBubbles && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                Log.Warn("NotifHelper", $"AreBubblesSupported: False (Channel '{ChannelId}' exists but CanBubble() is false). User might have disabled bubbles for this specific channel. Or CreateChannel failed to set it.");
            }


            Log.Info("NotifHelper_SupportCheck", $"Channel '{ChannelId}' Allows Bubbles (CanBubble()): {channelAllowsBubbles}");
            if (channelExists && !channelAllowsBubbles)
            {
                Log.Warn("NotifHelper_SupportCheck", "Channel exists but does NOT allow bubbles. User may have disabled for channel.");

            }
            bool finalResult = areBubblesGloballyEnabled && channelExists && channelAllowsBubbles;
            Log.Info("NotifHelper_SupportCheck", $"--- Final Bubble Support Result: {finalResult} ---");
            return areBubblesGloballyEnabled && channelExists && channelAllowsBubbles;
        }
        catch (Exception ex)
        {
            Log.Error("NotifHelper", $"Error checking bubble support: {ex.Message}");
            return false;
        }
    }


    // Optional: Method to open Bubble settings for the user
    public static void OpenBubbleSettings(Context context)
    {
        try
        {
            var intent = new Intent(Android.Provider.Settings.ActionAppNotificationBubbleSettings);
            intent.PutExtra(Android.Provider.Settings.ExtraAppPackage, context.PackageName);
            intent.AddFlags(ActivityFlags.NewTask);

            if (intent.ResolveActivity(context.PackageManager!) != null)
            {
                context.StartActivity(intent);
                Log.Debug("NotifHelper", "Opened App Notification Bubble Settings for user.");
            }
            else
            {
                Log.Warn("NotifHelper", "Could not resolve App Notification Bubble Settings intent.");
            }
        }
        catch (System.Exception ex)
        {
            Log.Error("NotifHelper", $"Error opening bubble settings: {ex.Message}");
        }
    }

    public static Notification BuildMinimalNotification(Context context)
    {
        CreateChannel(context);
        var builder = new Notification.Builder(context, ChannelId)!
            .SetOngoing(true)!
            .SetPriority(0)!
            .SetVisibility(NotificationVisibility.Secret)!;

        return builder.Build();
    }
}