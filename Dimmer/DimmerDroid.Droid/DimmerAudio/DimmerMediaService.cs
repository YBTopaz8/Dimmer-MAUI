using AndroidX.Core.App;
using AndroidX.Media3.Session;

using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.DimmerAudio;

[Service(Name = "com.yvanbrunel.dimmer.DimmerMediaService",
         Enabled = true, Exported = true,
         ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter(new[] { "androidx.media3.session.MediaSessionService" })]
public partial class DimmerMediaService : MediaSessionService
{

    private const int NotificationId = 999;
    private const string ChannelId = "dimmer_media_channel";

    private MediaSession? _mediaSession;
    private OwnAudioPlayerAdapter? _playerAdapter;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
           .SetContentTitle("Dimmer")?
           .SetContentText("Audio engine ready")?
           // Use your app's actual icon here (e.g., Resource.Drawable.app_icon)
           // If you don't have one handy, ApplicationInfo.Icon is a safe fallback
           .SetSmallIcon(Resource.Drawable.musicalbum)?
           .SetPriority(NotificationCompat.PriorityLow)?
           .SetOngoing(true)?
           .Build();

        // Android 10+ (API 29) requires passing the ForegroundServiceType
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }
        // 1. Get your cross-platform OwnAudio Engine
        var audioService = OwnAudioService.Current;

        // 2. Wrap it in your Adapter
        _playerAdapter = new OwnAudioPlayerAdapter(audioService);

        // 3. Create Intent to open the app when the notification is tapped
        Intent intent = new Intent(Platform.AppContext, typeof(MainActivity));
        PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        PendingIntent? pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, intent, flags);

        // 4. Build the Media Session
        _mediaSession = new MediaSession.Builder(this, _playerAdapter)!
            .SetSessionActivity(pendingIntent)!
            .SetId("Dimmer_OwnAudio_Session")!
            .Build();

        // Tell Media3 to automatically manage the notification
        var notificationProvider = new DefaultMediaNotificationProvider.Builder(this).Build();
        this.SetMediaNotificationProvider(notificationProvider);
    }
    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var channelName = "Dimmer Playback";
        var channelDescription = "Controls for Dimmer Audio Playback";
        var channel = new NotificationChannel(ChannelId, channelName, NotificationImportance.Low)
        {
            Description = channelDescription
        };

        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        notificationManager?.CreateNotificationChannel(channel);
    }
    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
    {
        return _mediaSession;
    }

    public override void OnDestroy()
    {
        _mediaSession?.Release();
        _mediaSession = null;
        _playerAdapter?.Release();
        base.OnDestroy();
    }
}