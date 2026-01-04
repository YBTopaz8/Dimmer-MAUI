using Android.Content;

using AndroidX.Core.App;
using AndroidX.Media3.Common;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;

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


        var customActionReceiver = new DimmerActionReceiver(service);

        var descrAdapter = new LyricsMediaDescriptionAdapter(pi, service as ExoPlayerService);


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
        var actionList = new List<string> {
        DimmerActionReceiver.ActionFavorite,
        DimmerActionReceiver.ActionShuffle,
        DimmerActionReceiver.ActionLyrics
    };

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
    class DimmerActionReceiver : Java.Lang.Object, PlayerNotificationManager.ICustomActionReceiver
    {
        private readonly ExoPlayerService _service;
        private readonly Context _ctx;
        public const string ActionLyrics = "ACTION_LYRICS";
        private const string ServiceAction = "com.yvanbrunel.dimmer.ACTION_LYRICS";
        public DimmerActionReceiver(Context ctx) => _ctx = ctx;
        public const string ActionFavorite = "ACTION_FAVORITE";
        public const string ActionShuffle = "ACTION_SHUFFLE";

        public IList<string> GetCustomActions(IPlayer? player)
        {
            return new List<string> { ActionFavorite, ActionShuffle, ActionLyrics };
        }

        // 2. Create the actual Button UI (C# logic)
        public NotificationCompat.Action? GetCustomAction(Context context, string action)
        {
            switch (action)
            {
                case ActionFavorite:
                    // Check your VM for current favorite status to pick icon
                    int heartIcon = ExoPlayerService.CurrentSongContext?.IsFavorite == true
                        ? Resource.Drawable.media3_icon_heart_filled
                        : Resource.Drawable.heart;
                    return CreateAction(context, heartIcon, "Favorite", ActionFavorite);

                case ActionShuffle:
                    return CreateAction(context, Resource.Drawable.shuffle, "Shuffle", ActionShuffle);

                case ActionLyrics:
                    return CreateAction(context, Resource.Drawable.lyrics, "Lyrics", ActionLyrics);
            }
            return null;
        }

        private NotificationCompat.Action CreateAction(Context context, int icon, string title, string action)
        {
            var intent = new Intent(context, typeof(ExoPlayerService)).SetAction(action);
            var pi = PendingIntent.GetService(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            return new NotificationCompat.Action(icon, new Java.Lang.String(title), pi);
        }

        // 3. Handle the Click in C#
        public void OnCustomAction(IPlayer? player, string? action, Intent? intent)
        {
            //if (action == ActionFavorite)
            //{
            //    _service.MyViewModel.ToggleFavorite(_service.CurrentSongContext);
            //    _service.RefreshNotification(); // Helper to redraw the heart
            //}
            //else if (action == ActionShuffle)
            //{
            //    _service.ToggleShuffle();
            //}
            //else if (action == ActionLyrics)
            //{
            //    // Trigger your C# lyrics overlay
            //}
        }
        private NotificationCompat.Action BuildNotificationAction(Context context, int icon, string title, string action)
        {
            // Create an intent that points back to your ExoPlayerService
            var intent = new Intent(context, typeof(ExoPlayerService)).SetAction(action);

            // For Android 12+, we MUST use Immutable or Mutable. Background actions are usually Immutable.
            var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var pi = PendingIntent.GetService(context, 0, intent, flags);

            return new NotificationCompat.Action(icon, new Java.Lang.String(title), pi);
        }
        public IDictionary<string, NotificationCompat.Action>? CreateCustomActions(Context? context, int p1)
        {
            var actions = new Dictionary<string, NotificationCompat.Action>();

            // 1. Create the Favorite Action
            // We check the ViewModel state to decide which icon to "register"
            //bool isFav = _service.MyViewModel.CurrentPlayingSongView?.IsFavorite ?? false;
            bool isFav = true;
            int heartIcon = isFav ? Resource.Drawable.media3_icon_heart_filled : Resource.Drawable.heartbroken;
            actions.Add(ActionFavorite, BuildNotificationAction(context, heartIcon, "Favorite", ActionFavorite));

            // 2. Create the Shuffle Action
            actions.Add(ActionShuffle, BuildNotificationAction(context, Resource.Drawable.shuffle, "Shuffle", ActionShuffle));

            // 3. Create the Lyrics Action
            actions.Add(ActionLyrics, BuildNotificationAction(context, Resource.Drawable.lyrics, "Lyrics", ActionLyrics));

            return actions;
        }
        public NotificationCompat.Action? CreateCustomAction(Context context, string action, int instanceId)
        {
            switch (action)
            {
                case ActionFavorite:
                    // Check C# state for the heart icon
                    //bool isFav = _service.MyViewModel.CurrentPlayingSongView?.IsFavorite ?? false;
                    //int heartIcon = isFav ? Resource.Drawable.heart_filled : Resource.Drawable.heart_outline;
                    //return BuildAction(context, heartIcon, "Favorite", ActionFavorite);

                case ActionShuffle:
                    return BuildAction(context, Resource.Drawable.shuffle, "Shuffle", ActionShuffle);

                case ActionLyrics:
                    //return BuildAction(context, Resource.Drawable.lyrics_icon, "Lyrics", ActionLyrics);
                    break;
            }
            return null;
        }
        private NotificationCompat.Action BuildAction(Context context, int icon, string title, string action)
        {
            var intent = new Intent(context, typeof(ExoPlayerService)).SetAction(action);
            var pi = PendingIntent.GetService(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            return new NotificationCompat.Action(icon, new Java.Lang.String(title), pi);
        }

    }

    // Custom media description adapter that displays lyrics
    class LyricsMediaDescriptionAdapter : Java.Lang.Object, PlayerNotificationManager.IMediaDescriptionAdapter
    {
        private readonly PendingIntent _pendingIntent;
        private readonly ExoPlayerService? _service;
        private string? _cachedLyricText;
        private IDisposable? _lyricSubscription;

        public LyricsMediaDescriptionAdapter(PendingIntent pendingIntent, ExoPlayerService? service)
        {
            _pendingIntent = pendingIntent;
            _service = service;
            
            // Subscribe to lyrics updates to cache the current text
            try
            {
                var lyricsMgtFlow = MainApplication.ServiceProvider?.GetService<LyricsMgtFlow>();
                if (lyricsMgtFlow != null)
                {
                    _lyricSubscription = lyricsMgtFlow.CurrentLyric
                        .Subscribe(
                            lyricLine =>
                            {
                                if (lyricLine != null && !string.IsNullOrWhiteSpace(lyricLine.Text))
                                {
                                    _cachedLyricText = lyricLine.Text;
                                }
                                else
                                {
                                    _cachedLyricText = null;
                                }
                            },
                            ex => Console.WriteLine($"[LyricsMediaDescriptionAdapter] Error: {ex.Message}")
                        );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LyricsMediaDescriptionAdapter] Failed to subscribe: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lyricSubscription?.Dispose();
                _lyricSubscription = null;
            }
            base.Dispose(disposing);
        }

        public Java.Lang.ICharSequence? GetCurrentContentText(IPlayer? player)
        {
            // Use cached lyric text if available
            if (_service != null && 
                ExoPlayerService.CurrentSongContext?.HasSyncedLyrics == true && 
                !string.IsNullOrWhiteSpace(_cachedLyricText))
            {
                return new Java.Lang.String(_cachedLyricText);
            }

            // Fall back to artist name if no lyrics
            var metadata = player?.MediaMetadata;
            return metadata?.Artist ?? new Java.Lang.String("Unknown Artist");
        }

        public Java.Lang.ICharSequence? GetCurrentContentTitle(IPlayer? player)
        {
            var metadata = player?.MediaMetadata;
            return metadata?.Title ?? new Java.Lang.String("Unknown Title");
        }

        public Bitmap? GetCurrentLargeIcon(IPlayer? player, PlayerNotificationManager.BitmapCallback? callback)
        {
            // Try to get album artwork
            var metadata = player?.MediaMetadata;
            if (metadata?.ArtworkUri != null)
            {
                try
                {
                    var artworkPath = metadata.ArtworkUri.Path;
                    if (!string.IsNullOrEmpty(artworkPath) && File.Exists(artworkPath))
                    {
                        return BitmapFactory.DecodeFile(artworkPath);
                    }
                }
                catch
                {
                    // Fall through to default
                }
            }
            return null;
        }

        public PendingIntent? CreateCurrentContentIntent(IPlayer? player)
        {
            return _pendingIntent;
        }
    }


}