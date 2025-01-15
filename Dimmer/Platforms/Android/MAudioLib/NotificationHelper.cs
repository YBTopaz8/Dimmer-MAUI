using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using static Android.App.Notification;
using Resource = global::Android.Resource; 
using AndroidMedia = Android.Media;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;


/// <summary>
/// Helper class for creating and managing media playback notifications.
/// </summary>
public static class NotificationHelper
{
    // Unique ID for the notification channel
    public const string CHANNEL_ID = "DimmerMusic";
    private const int NotificationId = 1001; // Changed to avoid potential conflicts

    /// <summary>
    /// Cancels all active notifications.
    /// </summary>
    /// <param name="context">The application context.</param>
    internal static void StopNotification(Context context)
    {
        NotificationManagerCompat.From(context).CancelAll();
    }

    /// <summary>
    /// Creates a notification channel for media playback (required for Android Oreo and above).
    /// </summary>
    /// <param name="context">The application context.</param>
    //public static void CreateNotificationChannel(Context context)
    //{
    //    if (Build.VERSION.SdkInt < BuildVersionCodes.O)
    //    {
    //        // No need to create notification channels before Android Oreo.
    //        return;
    //    }

    //    // Create the NotificationChannel
    //    var name = "Media Playback";
    //    var description = "Show controls for ongoing media playback.";
    //    var importance = NotificationImportance.Low; // Consider Importance.Default for higher visibility
    //    var channel = new NotificationChannel(CHANNEL_ID, name, importance)
    //    {
    //        Description = description,
    //        LockscreenVisibility = NotificationVisibility.Public // Show notification on lock screen
    //    };
    //    channel.SetSound(null, null); // No notification sound

    //    // Register the channel with the system
    //    var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
    //    notificationManager?.CreateNotificationChannel(channel);
    //}

    /// <summary>
    /// Builds and displays the media playback notification.
    /// </summary>
    /// <param name="context">The application context.</param>
    /// <param name="mediaMetadata">The metadata of the currently playing track.</param>
    /// <param name="mediaSession">The media session associated with playback.</param>
    /// <param name="largeIcon">The large icon to display in the notification.</param>
    /// <param name="isPlaying">Indicates if media is currently playing.</param>
    /// <returns>The built Notification object.</returns>
    //internal static Notification StartNotification(
    //    Context context,
    //    MediaMetadata mediaMetadata,
    //    AndroidMedia.Session.MediaSession mediaSession,
    //    Bitmap? largeIcon,
    //    bool isPlaying)
    //{
        //try
        //{
        //    // Create an intent that will open the app if the notification is clicked
        //    Intent intent = new Intent(context, typeof(MainActivity));
        //    intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.NewTask);
        //    PendingIntentFlags pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
        //        ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
        //        : PendingIntentFlags.UpdateCurrent;
        //    PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, intent, pendingIntentFlags); // Request code 0

        //    // Build the notification
        //    MediaStyle style = new MediaStyle().SetMediaSession(mediaSession.SessionToken);
        //    var s = new  AndroidX.Core.App.NotificationCompat.Style();
        //    NotificationCompat.Builder builder = new NotificationCompat.Builder(context, CHANNEL_ID)
        //        .SetStyle(style)
        //        .SetContentTitle(mediaMetadata.GetString(MediaMetadata.MetadataKeyTitle))
        //        .SetContentText(mediaMetadata.GetString(MediaMetadata.MetadataKeyArtist))
        //        .SetSubText(mediaMetadata.GetString(MediaMetadata.MetadataKeyAlbum))
        //        .SetSmallIcon( Resource.Drawable.atom) // Use app's resource
        //        .SetContentIntent(pendingIntent)
        //        .SetShowWhen(false)
        //        .SetOngoing(isPlaying)
        //        .SetVisibility(NotificationCompat.VisibilityPublic);

        //    // Add media control actions
        //    builder.AddAction(GenerateActionCompat(context, Resource.Drawable.stepbackward, "Previous", MediaPlayerService.ActionPrevious));
        //    AddPlayPauseActionCompat(builder, context, isPlaying);
        //    builder.AddAction(GenerateActionCompat(context, Resource.Drawable.stepforward, "Next", MediaPlayerService.ActionNext));

        //    // Show media controls in the compact notification view
        //    style.SetShowActionsInCompactView(0, 1, 2);

        //    // Set the large icon
        //    if (largeIcon != null)
        //    {
        //        builder.SetLargeIcon(largeIcon);
        //    }

        //    return builder.Build();
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"Error creating notification: {ex.Message}");
        //    // Consider logging the exception with a more robust logging mechanism
        //    return new NotificationCompat.Builder(context, CHANNEL_ID).Build(); // Return a basic notification as fallback
        //}
    //}

    /// <summary>
    /// Generates a NotificationCompat.Action for media controls.
    /// </summary>
    /// <param name="context">The application context.</param>
    /// <param name="icon">The resource ID of the action icon.</param>
    /// <param name="title">The title of the action.</param>
    /// <param name="intentAction">The action string for the intent.</param>
    /// <returns>The created NotificationCompat.Action.</returns>
    //internal static NotificationCompat.Action GenerateActionCompat(Context context, int icon, string title, string intentAction)
    //{
    //    Intent intent = new Intent(context, typeof(MediaPlayerService)).SetAction(intentAction);
    //    PendingIntentFlags flags = PendingIntentFlags.Immutable; // Use Immutable for actions
    //    PendingIntent pendingIntent = PendingIntent.GetService(context, GenerateRequestCode(), intent, flags);
    //    return new NotificationCompat.Action.Builder(icon, title, pendingIntent).Build();
    //}

    /// <summary>
    /// Adds a play or pause action to the notification builder based on the playing state.
    /// </summary>
    /// <param name="builder">The NotificationCompat.Builder instance.</param>
    /// <param name="context">The application context.</param>
    /// <param name="isPlaying">Indicates if media is currently playing.</param>
    //private static void AddPlayPauseActionCompat(NotificationCompat.Builder builder, Context context, bool isPlaying)
    //{
    //    var icon = isPlaying ? Resource.Drawable.pauseicon : Resource.Drawable.playicon;
    //    var title = isPlaying ? "Pause" : "Play";
    //    var intentAction = isPlaying ? MediaPlayerService.ActionPause : MediaPlayerService.ActionPlay;
    //    builder.AddAction(GenerateActionCompat(context, icon, title, intentAction));
    //}

    /// <summary>
    /// Generates a unique request code for PendingIntents.
    /// </summary>
    /// <returns>A unique request code.</returns>
    private static int GenerateRequestCode()
    {
        return Guid.NewGuid().GetHashCode();
    }
}
