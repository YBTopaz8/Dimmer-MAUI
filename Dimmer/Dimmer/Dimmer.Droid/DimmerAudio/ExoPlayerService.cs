#region Using Directives
// Android Core
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using AndroidX.Media3.UI;
using Uri = Android.Net.Uri;

// AndroidX Core & Media
using AndroidX.Core.App;
using AndroidX.Core.Content;

// Media3 Core Components - !! ENSURE NUGETS ARE CORRECT !!
using AndroidX.Media3.Common; // Provides IPlayer, MediaItem, MediaMetadata, Rating, etc.
using AndroidX.Media3.ExoPlayer; // Provides IExoPlayer, ExoPlayer
using AndroidX.Media3.Session; // Provides MediaSessionService, MediaSession, SessionCommand, SessionResult, etc.

// Java Interop
using Java.Lang;
using Object = Java.Lang.Object;

// AndroidX Concurrent Futures - For CallbackToFutureAdapter
using AndroidX.Concurrent.Futures; // <<-- 
using Google.Common.Util.Concurrent; // <<-- PROVIDES IListenableFuture INTERFACE needed by CallbackToFutureAdapter

// System & IO
using System;
using System.IO;
using System.Collections.Generic;
#endregion

// Your App specific using
using Dimmer.Droid; // Make sure this namespace is correct for MainActivity and HeartRating
using AndroidX.Media3.Common.Text;
// using Exception = Java.Lang.Exception; // Can use System.Exception generally
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using Exception = Java.Lang.Exception;
using AndroidX.Media3.ExoPlayer.Source.Preload;
using static Android.Text.Style.TtsSpan;
using System.Diagnostics;
using Android.Media;
using AndroidX.Media3.UI;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using Rating = AndroidX.Media3.Common.Rating;
using AndroidX.Media3.ExoPlayer.Source.Ads;
using static Android.Provider.CalendarContract;
using Android.Util;
using Java.Util.Concurrent;


namespace Dimmer.DimmerAudio; // Make sure this namespace is correct

[Service(Name = "com.yvanbrunel.dimmer.ExoPlayerService", // Ensure this matches AndroidManifest.xml if needed
         Enabled = true, Exported = true,
         ForegroundServiceType = ForegroundService.TypeMediaPlayback)]


public class ExoPlayerService : MediaSessionService
{
    // --- Components ---
    private MediaSession? mediaSession;
    private IExoPlayer? player;
    private MediaPlaybackSessionCallback? sessionCallback; // Use concrete type

    // --- Constants ---
    public const string CommandPreparePlay = "com.yvanbrunel.dimmer.action.PREPARE_PLAY";
    public const string CommandSetFavorite = "com.yvanbrunel.dimmer.action.SET_FAVORITE";
    public const string KeyMediaPlayDataUrl = "KEY_URL";
    public const string KeyMediaPlayDataTitle = "KEY_TITLE";
    public const string KeyMediaPlayDataArtist = "KEY_ARTIST";
    public const string KeyMediaPlayDataAlbum = "KEY_ALBUM";
    public const string KeyMediaPlayDataImagePath = "KEY_IMAGE_PATH";
    public const string KeyMediaPlayDataDuration = "KEY_DURATION";
    public const string KeyMediaPlayDataPosition = "KEY_POSITION";
    public const string KeyMediaPlayDataIsFavorite = "KEY_IS_FAVORITE";
    //private const string MetadataKeyDurationString = MetadataCompat.MetadataKeyDuration; // Use constant from support lib if available

    // --- Internal State ---
    private MediaItem? currentMediaItem;
    private bool currentFavoriteStatus = false;
    private const int NotificationId = 010899; // Choose a unique ID

    // --- Service Lifecycle ---
    private ExoPlayerServiceBinder? _binder;

    public event StatusChangedEventHandler? StatusChanged;
    public event BufferingEventHandler? Buffering;
    public event CoverReloadedEventHandler? CoverReloaded;
    public event PlayingEventHandler? Playing;
    public event PlayingChangedEventHandler? PlayingChanged;
    public event PositionChangedEventHandler? PositionChanged; public event EventHandler<PlaybackExceptionEventArgs>? ErrorOccurred;
    public event EventHandler<long>? DurationChanged;

    public event EventHandler? SeekCompleted;

    const string CHANNEL_ID = "dimmer_media_playback";
    const int NOTIF_ID = 1001;
    PlayerNotificationManager _notifMgr; 
    
    private Handler _positionHandler;
    private Runnable _positionRunnable;
    public ExoPlayerServiceBinder? Binder { get => _binder; set => _binder = value; }
    private void StartPositionUpdates() // Renamed from previous example if different
    {
        StopPositionUpdates(); // Ensure only one timer is running
        if (_positionHandler != null && _positionRunnable != null)
        {
            _positionHandler.Post(_positionRunnable);
        }
    }

    private void StopPositionUpdates() // Renamed from previous example if different
    {
        _positionHandler?.RemoveCallbacks(_positionRunnable!);
    }

    internal void RaiseStatusChanged(int playbackState)
    {
        Console.WriteLine(playbackState);
        StatusChanged?.Invoke(this, new EventArgs());
        // Manage timer based on state
        
    }

    internal void RaiseIsPlayingChanged(bool isPlaying)
    {
        PlayingChanged?.Invoke(this, isPlaying);
        if (isPlaying)
            StartPositionUpdates();
        else
            StopPositionUpdates();
    }

    internal void RaiseIsBufferingChanged(bool isBuffering)
    {
        //Buffering?.Invoke(this, isBuffering);
    }

    internal void RaiseDurationChanged(long durationMs)
    {
        //DurationChanged?.Invoke(this, durationMs);
    }

    internal void RaiseCoverReloaded()
    {
        CoverReloaded?.Invoke(this, EventArgs.Empty);
    }

    internal void RaiseErrorOccurred(PlaybackException error)
    {
        //ErrorOccurred?.Invoke(this, new PlaybackExceptionEventArgs(error));
        StopPositionUpdates();
    }

    internal void RaiseSeekCompleted()
    {
        Console.WriteLine("RaiseSeekCompleted");
        SeekCompleted?.Invoke(this, EventArgs.Empty);
    
    }

    public MediaController mediaController;
    
    public async override void OnCreate()
    {
        base.OnCreate();
        Console.WriteLine("[ExoPlayerService] OnCreate");
        try
        {
            player = new ExoPlayerBuilder(this)
                .SetAudioAttributes(AudioAttributes.Default, true) // handleAudioFocus=true
                .SetHandleAudioBecomingNoisy(true)
                .SetWakeMode(C.WakeModeNetwork)
                
                .Build();
            
            player.AddListener(new PlayerEventListener(this));

            sessionCallback = new MediaPlaybackSessionCallback(this); // Use concrete type


            Intent nIntent = new Intent(Platform.AppContext, typeof(MainActivity));
            PendingIntent? pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, PendingIntentFlags.Mutable);
            var comss = new List<SessionCommand>
                {
    new SessionCommand(CommandPreparePlay, Bundle.Empty),
    new SessionCommand(CommandSetFavorite, Bundle.Empty)
                };


            //var e = new SessionCommand(ExoPlayerService.CommandPreparePlay, Bundle.Empty);
            //var w = new SessionCommand(ExoPlayerService.ActionNext, Bundle.Empty);
            //var ww = new SessionCommand(ExoPlayerService.ActionStop, Bundle.Empty);
            //List<SessionCommand> mediaButtonCommands = new List<SessionCommand>();
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionPlay, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionPause, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionTogglePlayback, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionNext, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionPrevious, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionSeekTo, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionSetRating, Bundle.Empty));
            //mediaButtonCommands.Add(new SessionCommand(ExoPlayerService.ActionStop, Bundle.Empty));




            mediaSession = new MediaSession.Builder(this, player)
                .SetSessionActivity(pendingIntent)
                .SetCallback(sessionCallback)
                //.SetPeriodicPositionUpdateEnabled(true) as global::AndroidX.Media3.Session.MediaSession.Builder;
            //var medSess2 = medSess
                //.SetShowPlayButtonIfPlaybackIsSuppressed(true)
                .SetId("Dimmer_MediaSession_Main") // Choose a unique ID
                //.SetMediaButtonPreferences(mediaButtonCommands) as global::AndroidX.Media3.Session.MediaSession.Builder;

                ////.SetCommandButtonsForMediaItems(comss)
                //medSess2
                .Build();
            //mediaSession = medSess2.Build();

            _binder = new ExoPlayerServiceBinder(this);

            // 2) NotificationManager
            NotificationHelper.CreateChannel(this);
            _notifMgr = NotificationHelper.BuildManager(this,  mediaSession!);

            _notifMgr.SetPlayer(player);    
            // 3) Poll position every second
            _positionHandler = new Handler(Looper.MainLooper);
            _positionRunnable = new Runnable(() =>
            {
                // fire your PositionChanged event (you already have one in IAudioActivity)
                PositionChanged?.Invoke(this, player!.CurrentPosition);
                if (player.IsPlaying)
                    _positionHandler.PostDelayed(_positionRunnable, 1000);
            });
            _positionHandler.Post(_positionRunnable);

            Console.WriteLine("[ExoPlayerService] Initialization successful.");
            var controllerFuture = new MediaController.Builder(this, mediaSession.Token!).BuildAsync();
            
            var controllerObject = await controllerFuture.GetAsync();
            mediaController = (MediaController)controllerObject;

        }
        catch (Java.Lang.Throwable ex) { HandleInitError("JAVA INITIALIZATION", ex); StopSelf(); }
        catch (System.Exception ex) { HandleInitError("SYSTEM INITIALIZATION", ex); StopSelf(); }
    }


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        //HandleIntent(intent);
        base.OnStartCommand(intent, flags, startId);
        return StartCommandResult.Sticky;
    }


    public override IBinder OnBind(Intent? intent)
        => _binder;



    // … rest of your service (StartForeground, OnStartCommand, cleanup, etc.) …

class NotifListener : Java.Lang.Object, PlayerNotificationManager.INotificationListener
    {
        readonly MediaSessionService _svc;
        public NotifListener(MediaSessionService svc) => _svc = svc;

        public void OnNotificationPosted(int notificationId, Notification notification, bool ongoing)
        {
            if (ongoing)
                _svc.StartForeground(notificationId, notification);
            else
                _svc.StopForeground(false);
        }

        public void OnNotificationCancelled(int notificationId, bool dismissedByUser)
            => _svc.StopForeground(true);
    }

    public void Play() { 
        
        player.Play(); 
         }
    public void Pause() { 
        player.Pause();

        
    }
    //public bool IsPlaying => /*…*/

  
    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
    {
        // Called by controllers connecting to the session. Return the session instance.
        if (mediaSession == null)
        {
            LogInitWarning($"OnGetSession from {controllerInfo.PackageName}");
        }
        else
        {
            Console.WriteLine($"[ExoPlayerService] OnGetSession: Returning session instance to {controllerInfo.PackageName}.");
        }
        return mediaSession;
    }
    public class PlaybackStateEventArgs : EventArgs { public int PlaybackState { get; } public PlaybackStateEventArgs(int state) { PlaybackState = state; } }
    public class PlaybackExceptionEventArgs : EventArgs { public PlaybackException Error { get; } public PlaybackExceptionEventArgs(PlaybackException error) { Error = error; } }
    



    public override void OnDestroy()
    {
        Console.WriteLine("[ExoPlayerService] OnDestroy");
        // Release resources in reverse order of creation
        mediaSession?.Release();
        player?.Release();
        mediaSession = null;
        player = null;
        sessionCallback = null; // Not strictly needed but good practice
        base.OnDestroy();

        StopPositionUpdates(); // Stop timer
        _positionHandler = null;
        _positionRunnable?.Dispose(); // Dispose Runnable if needed
        _positionRunnable = null;

        _notifMgr?.SetPlayer(null); // Detach player
        _notifMgr = null; // Release reference
     _binder = null; // ADDED: Clear binder
    }

    // --- Public Accessors ---
    public IExoPlayer? GetPlayerInstance() => player;
    public MediaSession? GetMediaSessionInstance() => mediaSession;

    // --- Private Helpers ---
    private PendingIntent GetMainActivityPendingIntent()
    {
        // Intent to launch your main UI when the notification/session is tapped
        
        var launchIntent = new Intent(this, typeof(MainActivity)); // Ensure MainActivity is correct
        launchIntent.SetAction(Intent.ActionMain);
        launchIntent.AddCategory(Intent.CategoryLauncher);
        // Flags ensure existing instance is brought forward or new one started cleanly
        launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

        // Use Immutable flag for security on Android S+
        PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            flags |= PendingIntentFlags.Immutable;
        }

        return PendingIntent.GetActivity(this, 0, launchIntent, flags);
    }

    private void LogInitWarning(string stage)
    {
        Console.WriteLine($"[ExoPlayerService] Warning: {stage} called but initialization may have failed or is incomplete.");
        Console.WriteLine("---> Check previous logs for errors and verify NuGet packages are correct! <---");
    }
    private void HandleInitError(string type, System.Exception ex)
    {
        // Log critical errors - consider using a more robust logging framework
        Console.WriteLine($"[ExoPlayerService] !!! CRITICAL {type} ERROR: {ex.GetType().Name} - {ex.Message} !!!");
        Console.WriteLine($"[ExoPlayerService] Stack Trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[ExoPlayerService] Inner Exception: {ex.InnerException}");
        }
        Console.WriteLine("---> CHECK NuGet Packages: Media3.*, AndroidX.Concurrent.Futures, Guava/ListenableFuture <---");
        Console.WriteLine("---> Ensure target framework and dependencies are compatible. <---");
    }
    private static void HandleInitError(string type, Java.Lang.Throwable ex)
    {
        Console.WriteLine($"[ExoPlayerService] !!! CRITICAL JAVA {type} ERROR: {ex.Class.Name} - {ex.LocalizedMessage} !!!");
        Console.WriteLine($"[ExoPlayerService] Java Stack Trace: {Android.Util.Log.GetStackTraceString(ex)}");
        if (ex.Cause != null)
        {
            Console.WriteLine($"[ExoPlayerService] Java Cause: {ex.Cause}");
        }
        Console.WriteLine("---> CHECK NuGet Packages: Media3.*, AndroidX.Concurrent.Futures, Guava/ListenableFuture <---");
        Console.WriteLine("---> Check for Java class/method not found errors - often due to mismatched bindings. <---");
    }


    /// <summary>
    /// Sends a prepare-and-play command to the ExoPlayerService.
    /// </summary>
    /// <param name="url">URL or file path of the media.</param>
    /// <param name="title">Track title (for metadata).</param>
    /// <param name="artist">Artist name (for metadata).</param>
    /// <param name="album">Album name (for metadata).</param>
    /// <param name="imagePath">Optional local path to artwork.</param>
    /// <param name="startPositionMs">Where to start in milliseconds.</param>
    public Task Prepare(
        string url,
        string title,
        string artist,
        string album,
        string? imagePath = null,
        long startPositionMs = 0)
    {


        var metadataBuilder = new MediaMetadata.Builder()!
            .SetTitle(title)!
            .SetArtist(artist)!
            .SetAlbumTitle(album)!
            .SetUserRating(new HeartRating(true))! // Ensure HeartRating class exists
            .SetUserRating(new ThumbRating(true))! // Ensure HeartRating class exists
            .SetMediaType(new Java.Lang.Integer(MediaMetadata.MediaTypeMusic))! // Use Java Integer wrapper
            .SetIsPlayable(Java.Lang.Boolean.True); // Use Java Boolean wrapper

        // Set user rating (favorite status)
        //metadataBuilder.SetUserRating(new HeartRating(isFavorite)); // Ensure HeartRating class exists

        // Set artwork URI if available
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                metadataBuilder.SetArtworkUri(Uri.FromFile(new Java.IO.File(imagePath)));
                Console.WriteLine($"[ExoPlayerService] Set ArtworkUri: {imagePath}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ExoPlayerService] Warning: Failed to set ArtworkUri from path '{imagePath}': {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(imagePath))
        {
            Console.WriteLine($"[ExoPlayerService] Warning: Artwork file not found: {imagePath}");
        }

        try
        {
            currentMediaItem = new MediaItem.Builder()!
               .SetMediaId(url)! // Use URL as Media ID for simplicity
               .SetUri(Uri.Parse(url))!
               .SetMediaMetadata(metadataBuilder!.Build())!
               .Build();

            Console.WriteLine($"[ExoPlayerService] Setting MediaItem: ID={currentMediaItem.MediaId}, Pos={0}");
            player.SetMediaItem(currentMediaItem, 0); // Set item and start position
            player.AddMediaItem(currentMediaItem); 
            //player.SetMediaItems(new[] { currentMediaItem,currentMediaItem }); // Set item and start position
            player.Prepare();
            
            //player.Play(); // Start playback immediately
            
            Console.WriteLine("[ExoPlayerService] Player Prepare() called.");
            
        }
        catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }
        catch (System.Exception ex) { HandleInitError("PreparePlay SetMediaItem/Prepare", ex); }

        return Task.CompletedTask;
    }



    // --- Player Event Listener ---
    sealed class PlayerEventListener : Object,  IPlayerListener // Use specific IPlayer.Listener
    {
        private bool _seekInProgress = false;
        private long _lastReportedDuration = C.TimeUnset;
        private readonly ExoPlayerService service;
        public PlayerEventListener(ExoPlayerService service) { this.service = service; }

        // --- Key Event Implementations ---    
        
        // ← NEW: called when seek() actually finishes
        public void OnSeekProcessed()
        {
            
            Console.WriteLine("[PlayerEventListener] Seek completed");
        }
        // ← NEW: required for abstract onPlayerStateChanged(boolean,int)
        [Obsolete("Media3 now prefers OnPlaybackStateChanged(int), but this must still be implemented.")]

        public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
        {
            var stateString = playbackState switch
            {
                1 => "Idle",
                2 => "Buffering",
                3 => "Ready",
                4 => "Ended",
                _ => "Unknown"
            };
            Console.WriteLine($"[PlayerEventListener] PlayerStateChanged: PlayWhenReady={playWhenReady}, State={stateString} ({playbackState})");
            // Forward to your service or session as needed...
        }
        public void OnPositionDiscontinuity(global::AndroidX.Media3.Common.PlayerPositionInfo? oldPosition, global::AndroidX.Media3.Common.PlayerPositionInfo? newPosition, int reason)
        {
            Console.WriteLine($"[PlayerEventListener] OnPositionDiscontinuity:");
            Console.WriteLine($"  Reason: {reason}"); // Check this against Player.DISCONTINUITY_REASON_ constants
            Console.WriteLine($"  Old Position: {oldPosition.PositionMs}ms (Window: {oldPosition.WindowIndex})");
            Console.WriteLine($"  New Position: {newPosition.PositionMs}ms (Window: {newPosition.WindowIndex})");

            Log.WriteLine(LogPriority.Info, "MyAppSeekDebug", $"*** OnPositionDiscontinuity Entered! Reason={reason} ***");
            Log.Debug("PlayerEventListener", $"OnPositionDiscontinuity Detail: Reason={reason}, From={oldPosition?.PositionMs ?? -1}, To={newPosition?.PositionMs ?? -1}");
            
        }
        public void OnPlaybackStateChanged(int playbackState)
        {

            Console.WriteLine("new "+playbackState);
        }
        public void OnLoadingChanged(bool isLoading)
        {

            Console.WriteLine("new "+isLoading);
        }

        public void OnIsPlayingChanged(bool isPlaying)
        {
            Console.WriteLine($"[PlayerEventListener] IsPlayingChanged: {isPlaying}");
            service.RaiseIsPlayingChanged(isPlaying); 
        }

        public void OnPlayerError(PlaybackException? error) // Use specific PlaybackException
        {
            Console.WriteLine($"[PlayerEventListener] PlayerError: Code={error.ErrorCodeName}, Message={error.Message}");
            // Log the full error
            service.HandleInitError("PLAYER_ERROR", error);
            // Optionally stop the player or service on error
            service.RaiseErrorOccurred(error); // ADDED
            service.player?.Stop();
            // service.StopSelf(); // Consider if service should stop on error
        }

        public void OnPlayerErrorChanged(PlaybackException? error) // Nullable error
        {
            if (error != null)
            {
                Console.WriteLine($"[PlayerEventListener] PlayerErrorChanged: Code={error.ErrorCodeName}, Message={error.Message}");

                service.HandleInitError("PLAYER_ERROR_CHANGED", error);
                service.RaiseErrorOccurred(error); // ADDED
            }
            else
            {
                Console.WriteLine("[PlayerEventListener] PlayerErrorChanged: Error cleared.");
            }
        }

        public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata)
        {
            //Console.WriteLine($"[PlayerEventListener] MediaMetadataChanged: Title='{mediaMetadata?.Title ?? "N/A"}', Favorite={mediaMetadata?.UserRating is HeartRating hr && hr.IsHeart}");
            if (mediaMetadata?.UserRating is HeartRating hrr)
            { service.currentFavoriteStatus = hrr.IsHeart; } // Keep if using HeartRating
            service.RaiseCoverReloaded(); // ADDED
            CheckDurationUpdate(); // ADDED
        }
        private void CheckDurationUpdate()
        {
            long currentDuration = service.player?.Duration ?? C.TimeUnset;
            if (currentDuration > 0 && currentDuration != _lastReportedDuration)
            {
                _lastReportedDuration = currentDuration;
                service.RaiseDurationChanged(currentDuration);
            }
            else if (currentDuration <= 0 && _lastReportedDuration != C.TimeUnset)
            {
                _lastReportedDuration = C.TimeUnset;
                service.RaiseDurationChanged(C.TimeUnset);
            }
        }
        public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
        {
            string reasonString = reason switch
            {
                1 => "Repeat",
                2 => "Auto",
                3 => "Seek",
                4 => "PlaylistChanged",
                _ => "Unknown"
            };
            Console.WriteLine($"[PlayerEventListener] MediaItemTransition: Item='{mediaItem?.MediaId ?? "None"}', Reason={reasonString} ({reason})");
            service.currentMediaItem = mediaItem; // Update the service's current mediaItem reference
                                             // Reset favorite status based on the new mediaItem's metadata if available
            //if (mediaItem?.MediaMetadata?.UserRating is HeartRating hr)
            //{
            //    service.currentFavoriteStatus = hr.IsHeart;
            //}
            //else
            //{
            //    service.currentFavoriteStatus = false; // Default if no rating
            //}
        }

        // --- Other IPlayer.Listener Methods (Implement if needed, stubs are often sufficient) ---
        public void OnAudioAttributesChanged(AudioAttributes? p0) { /* Log if needed */ }
        public void OnAudioSessionIdChanged(int p0) { /* Log if needed */ }
        public void OnAvailableCommandsChanged(PlayerCommands? availableCommands)
        {
          
        }
        public void OnCues(CueGroup? p0) { /* Handle subtitles/cues */ }
        public void OnDeviceInfoChanged(DeviceInfo? p0) { /* Log if needed */ }
        public void OnDeviceVolumeChanged(int p0, bool p1) { /* Log if needed */ }
        public void OnEvents(IPlayer? player, PlayerEvents? events) 
        {

            if (events == null || player == null)
                return;
            // Example checks (Constants are in IPlayer):
            var siz = events.Size();
            Console.WriteLine(events.Size());

            for (int i = 0; i < siz; i++)
            {
                var eventType = events.Get(i);
                Console.WriteLine($"Event {i}: {eventType}");
                
            }
            // Example inspection:

            // ... other checks

        }
        public void OnIsLoadingChanged(bool isLoading) { Console.WriteLine($"[PlayerEventListener] IsLoadingChanged: {isLoading}"); }
        public void OnMaxSeekToPreviousPositionChanged(long p0) { /* Log if needed */ }
        // public void OnMetadata(MediaMetadata? p0) {} // Superseded by OnMediaMetadataChanged? Check docs.
        public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { Console.WriteLine($"[PlayerEventListener] PlayWhenReadyChanged: {playWhenReady}, Reason={reason}"); }
        public void OnPlaybackParametersChanged(PlaybackParameters? p0) { /* Log if needed */ }
        public void OnPlaybackSuppressionReasonChanged(int reason) { Console.WriteLine($"[PlayerEventListener] PlaybackSuppressionReasonChanged: {reason}"); }
        
        public void OnRenderedFirstFrame() { Console.WriteLine($"[PlayerEventListener] RenderedFirstFrame"); }
        public void OnRepeatModeChanged(int p0) { /* Log if needed */ }
        public void OnSeekBackIncrementChanged(long p0) { /* Log if needed */ }
        public void OnSeekForwardIncrementChanged(long p0) { /* Log if needed */ }
        public void OnShuffleModeEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSkipSilenceEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSurfaceSizeChanged(int p0, int p1) { /* Video related */ }
        public void OnTimelineChanged(Timeline? p0, int reason) 
        {
            Console.WriteLine(p0.IsEmpty);
            Console.WriteLine(p0.WindowCount);
            Console.WriteLine(p0.PeriodCount);
            
            
            Console.WriteLine($"[PlayerEventListener] TimelineChanged: Reason={reason}");
            CheckDurationUpdate(); // ADDED
        }
        public void OnTrackSelectionParametersChanged(TrackSelectionParameters? p0) { /* Log if needed */ }
        public void OnTracksChanged(Tracks? p0) { /* Log if needed */ }
        public void OnVideoSizeChanged(VideoSize? p0) { /* Video related */ }
        public void OnVolumeChanged(float p0) { /* Log if needed */ }

    } // End PlayerEventListener


    // --- Media Session Callback (Handles Controller Commands) ---
    sealed class MediaPlaybackSessionCallback : Object, MediaSession.ICallback
    {
        readonly IExoPlayer player;
        private readonly ExoPlayerService service;
        private const string CallbackDebugTag = "MediaSessionCallback"; // For CallbackToFutureAdapter

        public MediaPlaybackSessionCallback(ExoPlayerService service)
        {
            this.service = service;
            player = service.GetPlayerInstance();
        }

        const string TAG = "MediaSessionCallback";
        // --- Connection Handling ---
        public MediaSession.ConnectionResult OnConnect(
  MediaSession? session,
  MediaSession.ControllerInfo? controller)
        {
            Console.WriteLine($"[{TAG}] OnConnect from {controller.PackageName}");

            //var customList = new List<SessionCommand> {
            //new SessionCommand(ExoPlayerService.CommandSetFavorite, Bundle.Empty)
            //};
            var sessionCommands = new SessionCommands.Builder()
                   .Add(SessionCommand.CommandCodeSessionSetRating)  
                  .Build();
            var playerCommands = new PlayerCommands.Builder()
              .AddAllCommands()
              .Build();
            Console.WriteLine();
            return MediaSession.ConnectionResult.Accept(sessionCommands, playerCommands)!;
        }
        public void OnPostConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        {
            // Called after connection is established. Good place for post-connection logic if needed.
            Console.WriteLine($"[SessionCallback] OnPostConnect: Controller {controller.PackageName} connected.");
            // base.OnPostConnect(session, controller); // Call base if overridden
        }

        public void OnDisconnected(MediaSession? session, MediaSession.ControllerInfo? controller)
        {
            // Called when a controller disconnects.
            Console.WriteLine($"[SessionCallback] OnDisconnected: Controller {controller.PackageName} disconnected.");
            // base.OnDisconnected(session, controller); // Call base if overridden
        }

        // --- Command Handling ---

        // Handle CUSTOM commands sent via controller.sendCustomCommand(...)
      
        public bool OnMediaButtonEvent(global::AndroidX.Media3.Session.MediaSession? session, global::AndroidX.Media3.Session.MediaSession.ControllerInfo? controllerInfo, global::Android.Content.Intent? intent)
        {

            if (intent == null || intent.Action == null)
                return false;

            Console.WriteLine($"[SessionCallback] OnMediaButtonEvent: Received intent {intent.Action}");

            return true;
        }
        // Decide whether to allow a specific PLAYER command requested by a controller
        public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controllerInfo, int playerCommand)
        {
            // Default: Allow any player command that was declared available in OnConnect.
            // You could add logic here to disallow specific commands based on state or controller.
            // Example: if (playerCommand == PlayerCommands.CommandSeekTo && !isSeekAllowed) return SessionResult.ResultErrorNotSupported;
            Console.WriteLine($"[SessionCallback] OnPlayerCommandRequest: Command={playerCommand} from. Allowing.");


            var ply = session.Player;
           
            //return playerCommand;
            return SessionResult.ResultSuccess; 
        }
        public void OnPlayerInteractionFinished(MediaSession? session, MediaSession.ControllerInfo? controller, PlayerCommands? commands)
        {
            Console.WriteLine($"[SessionCallback] OnPlayerInteractionFinished: Controller={controller.PackageName} Commands={commands?.Size() ?? 0}");


            if (commands == null || commands.Size() == 0)
            {
                Console.WriteLine("[SessionCallback] No specific commands reported (system change?)");
                return;
            }

            for (int i = 0; i < commands.Size(); i++)
            {
                int command = commands.Get(i);

                switch (command)
                {
                    case 0:
                        Console.WriteLine("[SessionCallback] User pressed PLAY/PAUSE button.");
                        break;
                        
                    case 1:
                        Console.WriteLine("[SessionCallback] User pressed PLAY/PAUSE button.");
                        break;

                    case 5:
                        Console.WriteLine("[SessionCallback] User seeked to a position. "+session.Player.CurrentPosition);
                        break;

                    //case 7:
                    //    Console.WriteLine("[SessionCallback] User pressed SEEK BACK button.");
                    //    break;

                    //case 9:
                    //    Console.WriteLine("[SessionCallback] User pressed SEEK FORWARD button.");
                    //    break;

                    case 9:
                        Console.WriteLine("[SessionCallback] User pressed NEXT button.");
                        break;

                    case 7:
                        Console.WriteLine("[SessionCallback] User pressed PREVIOUS button.");
                        break;

                    default:
                        Console.WriteLine($"[SessionCallback] Unknown command: {command}");
                        break;
                }
            }
        }


       

    } // End MediaPlaybackSessionCallback

} // End ExoPlayerService class

sealed class Resolver : Java.Lang.Object, CallbackToFutureAdapter.IResolver
{
    readonly Action<CallbackToFutureAdapter.Completer> _action;
    readonly string _tag;

    public Resolver(Action<CallbackToFutureAdapter.Completer> action, string tag)
    {
        _action = action;
        _tag = tag;
    }

    public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
    {
        try
        { _action(completer); }
        catch (Exception ex) { completer.SetException(ex); }
        return _tag;
    }
}
public class ExoPlayerServiceBinder : Binder
{
    public ExoPlayerService Service { get; }


    internal ExoPlayerServiceBinder(ExoPlayerService service)
    {
        Service = service;
    }
}