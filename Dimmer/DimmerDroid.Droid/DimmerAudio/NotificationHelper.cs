namespace Dimmer.DimmerAudio;

using Android.Graphics;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using Resource = Dimmer.Resource;
using AndroidX.Media.Session;

public static class NotificationHelper
{
    public const string ChannelId = "dimmer_audio_channel";
    public const int NotificationId = 1000;

    public static void CreateNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

        var channel = new NotificationChannel(ChannelId, "Dimmer Playback", NotificationImportance.Low)
        { 
            Description = "Controls for Dimmer Audio Playback", LockscreenVisibility = NotificationVisibility.Public,
            //ShowBadge = false
        };

        var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        manager?.CreateNotificationChannel(channel);
    }

    public static Notification? BuildNotification(
        Context context,
        MediaSessionCompat mediaSession,
        bool isPlaying,
        SongModelView? currentSong,
        Bitmap? coverArt) // Pass null if you don't have it
    {
        CreateNotificationChannel(context);

        // 1. Standard Intents (Routed through MediaButtonReceiver)
        var playPauseIntent = MediaButtonReceiver.BuildMediaButtonPendingIntent(context, isPlaying ? PlaybackStateCompat.ActionPause : PlaybackStateCompat.ActionPlay);
        var prevIntent = MediaButtonReceiver.BuildMediaButtonPendingIntent(context, PlaybackStateCompat.ActionSkipToPrevious);
        var nextIntent = MediaButtonReceiver.BuildMediaButtonPendingIntent(context, PlaybackStateCompat.ActionSkipToNext);

        // 2. Custom Intent (Favorite)
        var favIntent = new Intent(context, typeof(DimmerCompatMediaService));
        favIntent.SetAction(DimmerMediaSessionCallback.ActionFavorite);
        var favPendingIntent = PendingIntent.GetService(context, 200, favIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // 3. App Open Intent (When user taps the notification body)
        var openAppIntent = new Intent(context, typeof(MainActivity));
        var openAppPendingIntent = PendingIntent.GetActivity(context, 0, openAppIntent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // 4. Build the Notification UI
        var builder = new NotificationCompat.Builder(context, ChannelId)?
            .SetContentTitle(currentSong?.Title ?? "Unknown Title")?
            .SetContentText(currentSong?.OtherArtistsName ?? "Unknown Artist")?
            .SetSubText(currentSong?.AlbumName ?? "Unknown Album")?
            .SetSmallIcon(Resource.Drawable.dimmicoo)? 
            .SetLargeIcon(coverArt)?
            .SetContentIntent(openAppPendingIntent)?
            .SetVisibility(NotificationCompat.VisibilityPublic)?
            .SetOngoing(isPlaying)? // Prevents swipe-away while playing

            // Actions (0, 1, 2, 3)
            .AddAction(currentSong?.IsFavorite ?? false ? Resource.Drawable.media3_icon_heart_filled : Resource.Drawable.media3_icon_heart_unfilled, "Favorite", favPendingIntent)? // Action 0
            .AddAction(Resource.Drawable.media3_icon_previous, "Previous", prevIntent)?        // Action 1
            .AddAction(isPlaying ? Resource.Drawable.media3_icon_pause : Resource.Drawable.media3_icon_circular_play, isPlaying ? "Pause" : "Play", playPauseIntent)? // Action 2
            .AddAction(Resource.Drawable.media3_icon_next, "Next", nextIntent)?                // Action 3

            // Apply MediaStyle
            .SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle()?
                .SetMediaSession(mediaSession.SessionToken)?
                // Show Prev, Play/Pause, Next on the compact lock screen view
                .SetShowActionsInCompactView(1, 2, 3));

        return builder?.Build();
    }
}