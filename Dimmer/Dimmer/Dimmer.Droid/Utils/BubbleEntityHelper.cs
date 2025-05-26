using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics; // For Bitmap
using Android.OS;
using Android.Util; // For Log
using AndroidX.Core.App; // For Person, ShortcutInfoCompat, ShortcutManagerCompat, NotificationCompat, NotificationManagerCompat
using AndroidX.Core.Content;
using AndroidX.Core.Content.PM;
using AndroidX.Core.Graphics.Drawable; // For IconCompat
using Dimmer;
using System;
using System.Collections.Generic;
using Person = AndroidX.Core.App.Person;

namespace Dimmer.Utils;
public static class BubbleEntityHelper
{
    // Call this method when you want to make an entity eligible for bubbling
    // or update its information, and then potentially show a notification that can bubble.
    public static void PrepareAndNotifyBubbleableEntity(
        Context context,
        string entityShortcutId,    // Unique ID for this bubblable entity
        string entityShortLabel,    // Short title for shortcut/bubble
        string entityLongLabel,     // Longer description
        Intent expandedBubbleIntent,// Intent to launch when bubble is expanded
        int entityIconResId,        // Drawable resource for the shortcut/bubble icon
        Bitmap? entityIconBitmap = null, // Optional: Bitmap for higher-res icon
        string? notificationTitle = null, // Title for the carrier notification
        string? notificationText = null,  // Text for the carrier notification
        int notificationSmallIconResId = 0 // Small icon for the carrier notification
        )
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q) // Bubbles API level
        {
            Log.Warn("BubbleEntityHelper", "Bubbles not supported on this API level.");
            return;
        }
        var categories = new HashSet<string> { "android.shortcut.conversation" };
        // --- 1. Create/Update the ShortcutInfo ---
        // This "Person" represents your entity. For non-chat apps, it's often a "bot" or app persona.
        Person entityPersona = new Person.Builder()
            .SetName(entityShortLabel) // Name of the "person" (your entity)
            .SetKey($"persona_{entityShortcutId}") // Unique key for this persona
            .SetBot(true)
            .SetImportant(true) // Mark as important for system ranking
            .Build();

        var shortcutBuilder = new ShortcutInfoCompat.Builder(context, entityShortcutId)
            .SetShortLabel(entityShortLabel)
            .SetLongLabel(entityLongLabel)
            .SetIntent(expandedBubbleIntent) // Intent for when shortcut itself is tapped (might be same as bubble)
            .SetPerson(entityPersona)
            .SetLongLived(true) // ESSENTIAL for persistent bubbles
                                // This allows the shortcut to persist even if app is not running,
                                // and for users to pin it or for it to be surfaced by the system.
            .SetCategories(categories)
            .SetRank(0); // Rank among other shortcuts

        if (entityIconBitmap != null && !entityIconBitmap.IsRecycled)
        {
            shortcutBuilder.SetIcon(IconCompat.CreateWithBitmap(entityIconBitmap));
        }
        else if (entityIconResId != 0)
        {
            shortcutBuilder.SetIcon(IconCompat.CreateWithResource(context, entityIconResId));
        }
        else
        {
            shortcutBuilder.SetIcon(IconCompat.CreateWithResource(context, Resource.Mipmap.appicon)); // Fallback
        }

        ShortcutInfoCompat shortcutInfo = shortcutBuilder.Build();
        try
        {
            ShortcutManagerCompat.PushDynamicShortcut(context, shortcutInfo);
            Log.Info("BubbleEntityHelper", $"Published/Updated shortcut: ID={entityShortcutId}");
        }
        catch (Exception ex)
        {
            Log.Error("BubbleEntityHelper", $"Error publishing shortcut ID {entityShortcutId}: {ex.Message}");
            return; // If shortcut fails, bubble likely won't work as intended
        }

        // --- 2. Check if Bubbles are Actually Supported/Enabled ---
        // Assuming NotificationHelper.ChannelId and AreBubblesSupported are available
        // You might move CreateChannel and AreBubblesSupported into this class or another shared utility.
        NotificationHelper.CreateChannel(context); // Ensure channel is created and configured for bubbles
        if (!NotificationHelper.AreBubblesSupported(context))
        {
            Log.Warn("BubbleEntityHelper", $"Bubbles are not currently supported/enabled for app/channel. Shortcut ID {entityShortcutId} created, but notification won't bubble now.");
            // Optionally, post a non-bubbling notification here or inform the user.
            // Forcing a bubble when user has disabled them is not possible.
            //return;
        }

        // --- 3. Create BubbleMetadata ---
        // The PendingIntent for the bubble's expanded view
        var bubbleContentPendingIntent = PendingIntent.GetActivity(
            context,
            entityShortcutId.GetHashCode(), // Unique request code based on entity ID
            expandedBubbleIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
        );

        // Icon for the collapsed bubble (can be different from shortcut icon if needed)
        IconCompat bubbleCollapsedIcon;
        if (entityIconBitmap != null && !entityIconBitmap.IsRecycled)
        {
            bubbleCollapsedIcon = IconCompat.CreateWithBitmap(entityIconBitmap);
        }
        else if (entityIconResId != 0)
        {
            bubbleCollapsedIcon = IconCompat.CreateWithResource(context, entityIconResId);
        }
        else
        {
            bubbleCollapsedIcon = IconCompat.CreateWithResource(context, Resource.Mipmap.appicon); // Fallback
        }


        NotificationCompat.BubbleMetadata.Builder bubbleMetadataBuilder;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // API 30+ prefers shortcutId in BubbleMetadata
        {
            bubbleMetadataBuilder = new NotificationCompat.BubbleMetadata.Builder(
                                        bubbleContentPendingIntent, bubbleCollapsedIcon)

                                    .SetIcon(bubbleCollapsedIcon) // Link bubble to the shortcut
                                    .SetAutoExpandBubble(false); // Set auto expand behavior
        }
        else // API 29 (Q)
        {
#pragma warning disable CS0618 // Builder() is obsolete but needed for API 29 target
            bubbleMetadataBuilder = new NotificationCompat.BubbleMetadata.Builder()
                .SetIntent(bubbleContentPendingIntent)
                .SetIcon(bubbleCollapsedIcon);
#pragma warning restore CS0618
        }

        NotificationCompat.BubbleMetadata bubbleMetadata = bubbleMetadataBuilder
           .SetDesiredHeight(context.Resources.DisplayMetrics.HeightPixels / 2) // Example: half screen height
           .SetAutoExpandBubble(false) // Set to true if you want it to expand immediately on notification post
                                       // For persistent bubbles like Telegram, false is common; user taps to expand.
           .SetSuppressNotification(true) // If true, the carrier notification is hidden when bubble is shown.
                                          // For persistent bubbles, you might want this true so only the bubble is visible.
           .Build();


        // --- 4. Create and Post the Carrier Notification ---
        // This notification acts as the vehicle for the bubble.
        // It MUST be associated with the same shortcutId for the system to treat it as
        // a notification FOR that bubblable entity/conversation.
        string effectiveNotifTitle = notificationTitle ?? entityShortLabel;
        string effectiveNotifText = notificationText ?? $"Tap to open {entityShortLabel}";
        int effectiveSmallIcon = notificationSmallIconResId != 0 ? notificationSmallIconResId : Resource.Drawable.atom; // Fallback

        NotificationCompat.Builder notificationBuilder =
            new NotificationCompat.Builder(context, NotificationHelper.ChannelId) // Use your bubble-enabled channel
                .SetContentTitle(effectiveNotifTitle)
                .SetContentText(effectiveNotifText)
                .SetSmallIcon(effectiveSmallIcon) // MUST be a valid small icon
                .SetPriority(NotificationCompat.PriorityDefault) // Or Low if not time-sensitive
                .SetBubbleMetadata(bubbleMetadata)
                .SetShortcutId(entityShortcutId) // CRITICAL: Link notification to the shortcut
                .SetLocusId(new LocusIdCompat(entityShortcutId)) // Provides context for the system
                .AddPerson(entityPersona) // Reinforces the "conversation" aspect
                .SetCategory(NotificationCompat.CategoryMessage) // Hint to system it's like a message
                                                                 // Or NotificationCompat.CategoryCall, etc.
                .SetContentIntent(bubbleContentPendingIntent) // What happens if user taps notification itself
                .SetAutoCancel(true); // Notification dismissed when tapped (if not suppressed and bubble shown)

        // Optionally, add actions to the notification if it's not suppressed
        // .AddAction(actionIcon, "Action Text", actionPendingIntent)

        NotificationManagerCompat notificationManager = NotificationManagerCompat.From(context);
        try
        {
            // Use a notification ID derived from the entity to allow updating/cancelling it.
            // Or use a fixed ID if you only ever have one "Now Playing" bubble.
            notificationManager.Notify(entityShortcutId.GetHashCode(), notificationBuilder.Build());
            Log.Info("BubbleEntityHelper", $"Notification for shortcut ID {entityShortcutId} posted. Bubble should appear if settings allow.");
        }
        catch (Exception ex)
        {
            Log.Error("BubbleEntityHelper", $"EXCEPTION posting notification for shortcut {entityShortcutId}: {ex.Message}");
        }
    }

    public static void CancelBubbleNotification(Context context, string entityShortcutId)
    {
        NotificationManagerCompat notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Cancel(entityShortcutId.GetHashCode());
        Log.Info("BubbleEntityHelper", $"Cancelled notification for shortcut ID {entityShortcutId}.");
        // Note: Cancelling the notification might not always remove a user-initiated bubble,
        // especially if the shortcut is long-lived and pinned.
        // To attempt to remove the shortcut itself:
        // ShortcutHelper.RemoveNowPlayingShortcut(context); (if you have such a method)
    }
}