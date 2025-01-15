using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using static Android.App.Notification;
using static Android.Resource;
using AndroidMedia = Android.Media;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;
public static class NotificationHelper
{
    public static readonly string CHANNEL_ID = "location_notification";
    private const int NotificationId = 1000;

    

    internal static void StopNotification(Context context)
    {
        NotificationManagerCompat nm = NotificationManagerCompat.From(context);
        nm.CancelAll();
    }

    public static void CreateNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            //NotificationChannel channel = new NotificationChannel(CHANNEL_ID, "Location Notifications", NotificationImportance.Default);
            // Notification channels are new in API 26 (and not a part of the
            // support library). There is no need to create a notification
            // channel on older versions of Android.
            return;
        }

        var name = "Dimmer";
        var description = "The count from MainActivity.";
        var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Min)
        {
            Description = description,
        };

        channel.SetSound(null, null);

        var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;

        notificationManager.CreateNotificationChannel(channel);
    }


    internal static Notification StartNotification(
        Context context,
        MediaMetadata mediaMetadata,
        AndroidMedia.Session.MediaSession mediaSession, // this is the same mediaSession as defined earlier being passed here
        Bitmap? largeIcon,
        bool isPlaying)
    {

        try
        {
            Intent intent = new(context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.NewTask);

            var pendingIntentFlags = (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable
                : PendingIntentFlags.UpdateCurrent;

            var pendingIntent = PendingIntent.GetActivity(context, 2, intent, pendingIntentFlags);
                        
            MediaMetadata currentTrack = mediaMetadata;

            MediaStyle style = new MediaStyle();
            style.SetMediaSession(mediaSession.SessionToken);
            var builder = new Builder(context, CHANNEL_ID)
               .SetStyle(style)
               .SetContentTitle(currentTrack.GetString(MediaMetadata.MetadataKeyTitle))
               .SetContentText(currentTrack.GetString(MediaMetadata.MetadataKeyArtist))
               .SetSubText(currentTrack.GetString(MediaMetadata.MetadataKeyAlbum))
               .SetSmallIcon(Drawable.IcMediaPlay)
               .SetContentIntent(pendingIntent)
               .SetShowWhen(false)
               
               //.SetTicker($"Now Playing {currentTrack.GetString(MediaMetadata.MetadataKeyTitle)}")
               .SetOngoing(isPlaying).SetVisibility(NotificationVisibility.Public);
            //builder.AddAction(GenerateActionCompat(context, Drawable.IcMediaPrevious, "Previous", MediaPlayerService.ActionPrevious));
            //AddPlayPauseActionCompat(builder, context, isPlaying);
            //builder.AddAction(GenerateActionCompat(context, Drawable.IcMediaNext, "Next", MediaPlayerService.ActionNext));
            //builder.AddAction(GenerateActionCompat(context, Drawable.ButtonStar, "Fav", MediaPlayerService.ActionNext));

            //style.SetShowActionsInCompactView(0, 1, 2, 3, 4);
            
            
            if (largeIcon is not null)
            {
                builder.SetLargeIcon(largeIcon);
            }
            var notif = builder.Build();
            return notif;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message + " ISSUEEEE");
            return new Notification();
        }
        
    }
    internal static Notification GenerateActionCompat(Context context, int icon, string title, string intentAction)
    {
        Intent intent = new Intent(context, typeof(MediaPlayerService));
        intent.SetAction(intentAction);
        PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
        if (intentAction.Equals(MediaPlayerService.ActionStop))
            flags = PendingIntentFlags.CancelCurrent;
        flags |= PendingIntentFlags.Mutable;
        PendingIntent pendingIntent = PendingIntent.GetService(context, 1, intent, flags)!;
        return new Notification.Builder(context, CHANNEL_ID).Build();
    }

    
}
