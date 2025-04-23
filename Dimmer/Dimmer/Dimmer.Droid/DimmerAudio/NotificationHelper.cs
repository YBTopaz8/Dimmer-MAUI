using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Media3.ExoPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerAudio;
public static class NotificationHelper
{
    public const string ChannelId = "dimmer_media";
    public static void CreateChannel(Context ctx)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;
        var chan = new NotificationChannel(
            ChannelId, "Dimmer player", NotificationImportance.Low)
        { Description = "Media playback" };
        chan.SetSound(null, null);
        var mgr = (NotificationManager)ctx.GetSystemService(Context.NotificationService);
        mgr.CreateNotificationChannel(chan);
    }

    public static Notification BuildNotification(
        Context ctx,
        Android.Support.V4.Media.Session.MediaSessionCompat.Token token,
        SimpleExoPlayer player)
    {
        var metadata = player.CurrentMediaItem?.GetMediaMetadata();
        var builder = new NotificationCompat.Builder(ctx, ChannelId)
            .SetStyle(new NotificationCompat.MediaStyle()
                .SetMediaSession(token)
                .SetShowActionsInCompactView(0, 1, 2))
            .SetSmallIcon(Resource.Drawable.ic_media_play)
            .SetContentTitle(metadata?.Title.ToString())
            .SetContentText(metadata?.Artist.ToString())
            .SetOngoing(player.PlayWhenReady);

        // Play/Pause
        var playPauseAction = new NotificationCompat.Action(
            player.PlayWhenReady ? Resource.Drawable.ic_pause : Resource.Drawable.ic_play,
            player.PlayWhenReady ? "Pause" : "Play",
            PendingIntent.GetService(
                ctx, 1,
                new Intent(ctx, typeof(ExoPlayerService)).SetAction(
                    player.PlayWhenReady
                      ? ExoPlayerService.ActionPause
                      : ExoPlayerService.ActionPlay),
                PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable)
        );
        builder.AddAction(
            new NotificationCompat.Action(
                Resource.Drawable.ic_skip_previous, "Prev",
                PendingIntent.GetService(
                    ctx, 2,
                    new Intent(ctx, typeof(ExoPlayerService))
                      .SetAction(ExoPlayerService.ActionPrevious),
                    PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable))
        );
        builder.AddAction(playPauseAction);
        builder.AddAction(
            new NotificationCompat.Action(
                Resource.Drawable.ic_skip_next, "Next",
                PendingIntent.GetService(
                    ctx, 3,
                    new Intent(ctx, typeof(ExoPlayerService))
                      .SetAction(ExoPlayerService.ActionNext),
                    PendingIntentFlags.UpdateCurrent|PendingIntentFlags.Immutable))
        );

        return builder.Build();
    }
}