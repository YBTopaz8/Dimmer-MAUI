using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using Android.Util;
using AndroidX.Core.App;
using AndroidX.Core.Graphics.Drawable;
using Dimmer.Activities;

namespace Dimmer.DimmerAudio;

public static class NotificationHelper
{
    public const string ChannelId = "dimmer_media_playback_channel";
    public const int NotificationId = 010899;
    public const int BubbleNotificationId = 1899;

    public static void CreateChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var chan = new NotificationChannel(
            ChannelId,
            ctx.GetString(Resource.String.playback_channel_name),
            NotificationImportance.Low
        )
        { 
            
            Description = ctx.GetString(Resource.String.playback_channel_desc) 
        };
        chan.Importance = NotificationImportance.Low; // Set to Low for background playback
        
        chan.EnableLights(true);
        chan.LockscreenVisibility = NotificationVisibility.Public;
        chan.SetAllowBubbles(true); // Allow bubbles on this channel
        chan.SetShowBadge(true); // Show badge on app icon
        chan.SetBypassDnd(true); // Bypass Do Not Disturb
        chan.EnableVibration(false);


        // *** ADD THIS LINE FOR BUBBLES (Requires API 29+) ***
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            chan.SetAllowBubbles(true); // Allow notifications on this channel to bubble
        }


        var notificationManager = (NotificationManager)ctx.GetSystemService(Context.NotificationService)!;
        if (notificationManager != null)
        {
            notificationManager.CreateNotificationChannel(chan);
            Log.Debug("NotifHelper", "Channel created/updated (Bubble support potentially enabled)");
            Console.WriteLine("NotifHelper : Channel created/updated (Bubble support potentially enabled)");
        }
        else
        {
            Log.Error("NotifHelper", "Failed to get NotificationManager service.");
        }
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
            .SetSmallIconResourceId(Resource.Drawable.exo_icon_circular_play)!            
           
            .Build()!;
        mgr.SetShowPlayButtonIfPlaybackIsSuppressed(true);
        mgr.SetMediaSessionToken(session.PlatformToken);

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
                    _svc.StopForeground(StopForegroundFlags.Remove);
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
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 33
            {
                _svc.StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
                _svc.StopForeground(StopForegroundFlags.Detach);
            }
            Log.Debug("NotifHelper", $"Cancelled id={notificationId} userDismissed={dismissedByUser}");
        }
    }
    public static bool AreBubblesSupported(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            return false;
        try
        {
            var notificationManager = NotificationManager.FromContext(context);
            bool areBubblesGloballyEnabled = notificationManager.AreBubblesAllowed();
            var channel = notificationManager.GetNotificationChannel(ChannelId); // Check OUR channel
            bool channelAllowsBubbles = channel?.CanBubble() ?? false;
            Log.Debug("NotifHelper", $"AreBubblesSupported: Global={areBubblesGloballyEnabled}, Channel={channelAllowsBubbles}");
            return areBubblesGloballyEnabled && channelAllowsBubbles;
        }
        catch (System.Exception ex)
        {
            Log.Error("NotifHelper", $"Error checking bubble support: {ex.Message}");
            return false;
        }
    }

    // Method to Show the Bubble
    public static void ShowPlaybackBubble(Context context, string trackTitle) // Add more parameters if needed (artist, etc.)
    {
        if (!AreBubblesSupported(context))
        {
            Log.Warn("NotifHelper", "Cannot show bubble: Bubbles not supported or not enabled.");
            // Consider prompting user to enable via RequestBubblePermission()
            return;
        }

        var notificationManager = NotificationManagerCompat.From(context);

        try
        {
            // --- 1. Intent for Bubble Content Activity ---
            // Use the Activity created in step 2
            var targetIntent = new Intent(context, typeof(PlaybackBubbleActivity));
            targetIntent.SetAction("SHOW_PLAYBACK_BUBBLE_" + DateTime.Now.Ticks); // Unique action prevents intent caching issues
            // Add extras if needed for the bubble activity
             targetIntent.PutExtra("trackTitle", trackTitle);

            var pendingIntentFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var bubbleContentPendingIntent = PendingIntent.GetActivity(context, 0, targetIntent, pendingIntentFlags);

            // --- 2. Icon for the Bubble ---
            // Ensure 'ic_bubble_icon' drawable exists (monochrome recommended)
            var bubbleIcon = IconCompat.CreateWithResource(context, Resource.Drawable.exo_icon_circular_play); // <<< YOUR BUBBLE ICON HERE
            var intSender = bubbleContentPendingIntent.IntentSender;
            
            // --- 3. BubbleMetadata ---
            NotificationCompat.BubbleMetadata bubbleMetadata;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Use IntentSender constructor for API 30+
            {
                bubbleMetadata = new NotificationCompat.BubbleMetadata.Builder(bubbleContentPendingIntent, bubbleIcon)
                   .SetDesiredHeight(400) // Adjust as needed
                   .SetAutoExpandBubble(false) // Don't expand automatically?
                   .SetSuppressNotification(true) // IMPORTANT: Hide the bubble's own notification shade entry
                   .Build();
            }
            else if (Build.VERSION.SdkInt == BuildVersionCodes.Q) // Use deprecated constructor for API 29
            {
                bubbleMetadata = new NotificationCompat.BubbleMetadata.Builder(bubbleContentPendingIntent, bubbleIcon)
                   .SetIcon(bubbleIcon)
                   .SetDesiredHeight(900)
                   .SetAutoExpandBubble(false)
                   
                   .SetSuppressNotification(true)
                   .Build();
            }
            else
            {
                Log.Error("NotifHelper", "Trying to create bubble metadata on unsupported OS version.");
                return; // Should have been caught by AreBubblesSupported
            }


            // --- 4. Build the Bubble Notification ---
            // This notification's main purpose is to CARRY the BubbleMetadata.
            // It uses the SAME channel ID as your media notification.
            var notificationBuilder = new NotificationCompat.Builder(context, ChannelId)
               .SetContentTitle("Playback Control") // Simple title
               .SetContentText(trackTitle)         // Show current track?
               .SetSmallIcon(Resource.Drawable.exo_icon_circular_play) // Status bar icon (REQUIRED) - Use your playback icon
               .SetPriority(NotificationCompat.PriorityDefault)
               .SetBubbleMetadata(bubbleMetadata) // *** Attach the bubble metadata ***
               .SetContentIntent(bubbleContentPendingIntent) // Tapping shade entry (if not suppressed) opens bubble activity
               .SetCategory(NotificationCompat.CategoryService) // Appropriate category
               .SetOngoing(false) // Bubble notification itself isn't typically ongoing unless you want it sticky
               .SetOnlyAlertOnce(true); // Don't buzz/sound every time it's posted

            // --- 5. Post the Bubble Notification ---
            // Use the UNIQUE BubbleNotificationId
            notificationManager.Notify(BubbleNotificationId, notificationBuilder.Build());
            Log.Debug("NotifHelper", $"Bubble notification posted with ID {BubbleNotificationId}");

        }
        catch (System.Exception ex)
        {
            Log.Error("NotifHelper", $"Error showing playback bubble: {ex.Message} | {ex.StackTrace}");
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

            if (intent.ResolveActivity(context.PackageManager) != null)
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
        var builder = new Notification.Builder(context, ChannelId)
            .SetContentTitle("Dimmer Music Player")
            .SetContentText("Preparing playback...")
            .SetSmallIcon(Resource.Drawable.exo_icon_circular_play) // use your icon
            .SetOngoing(true);

        return builder.Build();
    }
}