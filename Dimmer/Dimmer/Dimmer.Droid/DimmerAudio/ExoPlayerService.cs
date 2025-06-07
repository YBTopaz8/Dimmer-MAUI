#region Using Directives
// Android Core
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Media3.UI;
using Uri = Android.Net.Uri;

// AndroidX Core & Media

using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;

// Java Interop
using Java.Lang;
using Object = Java.Lang.Object;

// AndroidX Concurrent Futures - For CallbackToFutureAdapter

// System & IO
#endregion

// Your App specific using
using AndroidX.Media3.Common.Text;
// using Exception = Java.Lang.Exception; // Can use System.Exception generally
using DeviceInfo = AndroidX.Media3.Common.DeviceInfo;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using Android.Util;
using Java.Util.Concurrent;
using Android.Media;
using Dimmer.Activities;
using MediaController = AndroidX.Media3.Session.MediaController;
using Dimmer.Interfaces.Services.Interfaces;
using System.Threading.Tasks;
using Dimmer.Utilities.Events;
using System.Diagnostics;


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
    internal static MediaItem? currentMediaItem; // Choose a unique ID
    internal static SongModelView? CurrentSongItem; // Choose a unique ID

    // --- Service Lifecycle ---
    private ExoPlayerServiceBinder? _binder;

    public event StatusChangedEventHandler? StatusChanged;
    public event BufferingEventHandler? Buffering;
    public event CoverReloadedEventHandler? CoverReloaded;

    public event PlayingChangedEventHandler? PlayingChanged;
    public event PositionChangedEventHandler? PositionChanged;
    public event SeekCompletedEventHandler? SeekCompleted;
    public event PlayNextEventHandler? PlayNextPressed; // Triggered by MediaKeyNextPressed
    public event PlayPreviousEventHandler? PlayPreviousPressed; // Triggered by MediaKeyPreviousPressed
    public event EventHandler<long>? DurationChanged;
    //public event EventHandler<double>? SeekCompleted; // Triggered after a seek operation completes




    PlayerNotificationManager? _notifMgr;

    private Handler? _positionHandler;
    private Runnable? _positionRunnable;
    private MediaController? mediaController;

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

    internal void RaiseStatusChanged(DimmerPlaybackState state)
    {
        //Console.WriteLine(state);


        var pbEvents = new PlaybackEventArgs(CurrentSongItem);
        pbEvents.EventType = state;
        StatusChanged?.Invoke(this, pbEvents);


    }

    internal void RaiseIsPlayingChanged(bool isPlaying)
    {
        var pbEvents = new PlaybackEventArgs(CurrentSongItem);
        pbEvents.IsPlaying = isPlaying;
        pbEvents.EventType = isPlaying ? DimmerPlaybackState.Playing : DimmerPlaybackState.PausedDimmer;
        PlayingChanged?.Invoke(player, pbEvents);
        if (isPlaying)
            StartPositionUpdates();
        else
            StopPositionUpdates();
    }

    internal void RaiseIsBufferingChanged(bool isBuffering)
    {
        Buffering?.Invoke(this, EventArgs.Empty);
    }

    internal void RaiseDurationChanged(long durationMs)
    {
        DurationChanged?.Invoke(this, durationMs);
    }

    internal void RaiseCoverReloaded()
    {
        CoverReloaded?.Invoke(this, EventArgs.Empty);
    }

    internal void RaiseErrorOccurred(PlaybackException error)
    {
        StopPositionUpdates();
    }

    internal void RaiseSeekCompleted(double? newPosition)
    {

        //Console.WriteLine("RaiseSeekCompleted");
        if (newPosition is null)
        {
            return;
        }
        SeekCompleted?.Invoke(this, (double)newPosition);

    }

    internal void RaisePlayNextEventHandler()
    {

        var eventArgs = new PlaybackEventArgs(CurrentSongItem) { EventType=DimmerPlaybackState.PlayNextUser };

        PlayNextPressed?.Invoke(this, eventArgs);

    }

    internal void RaisePlayPreviousEventHandler()
    {


        var eventArgs = new PlaybackEventArgs(CurrentSongItem) { EventType=DimmerPlaybackState.PlayPreviousUser };
        PlayPreviousPressed?.Invoke(this, eventArgs);

    }


    public async override void OnCreate()
    {
        base.OnCreate();
        //Console.WriteLine("[ExoPlayerService] OnCreate");
        try
        {
            player = new ExoPlayerBuilder(this)
                .SetAudioAttributes(AudioAttributes.Default, true)!
                .SetHandleAudioBecomingNoisy(true)!
                .SetWakeMode(C.WakeModeNetwork)!
                .SetSkipSilenceEnabled(true)!
                .SetDeviceVolumeControlEnabled(true)!
                .SetSuppressPlaybackOnUnsuitableOutput(false)!

                .Build();

            player.AddListener(new PlayerEventListener(this));

            sessionCallback = new MediaPlaybackSessionCallback(this); // Use concrete type


            Intent nIntent = new Intent(Platform.AppContext, typeof(MainActivity));

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Or BuildVersionCodes.M for broader compatibility with Immutable
            {
                flags |= PendingIntentFlags.Immutable;
            }
            PendingIntent? pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, flags);


            mediaSession = new MediaSession.Builder(this, player)!
                .SetSessionActivity(pendingIntent)!
                .SetCallback(sessionCallback)!

                .SetId("Dimmer_MediaSession_Main")!
                .Build();

            _binder = new ExoPlayerServiceBinder(this);

            // 2) NotificationManager
            NotificationHelper.CreateChannel(this);
            _notifMgr = NotificationHelper.BuildManager(this, mediaSession!);

            _notifMgr.SetPlayer(player);
            // 3) Poll position every second
            _positionHandler = new Handler(Looper.MainLooper!);
            _positionRunnable = new Runnable(() =>
            {
                if (player != null && player.IsPlaying) // Add null check for player
                {
                    PositionChanged?.Invoke(this, player.CurrentPosition);
                    _positionHandler?.PostDelayed(_positionRunnable, 1000); // Check _positionHandler for null too
                }
            });
            _positionHandler.Post(_positionRunnable);

            await InitializeMediaControllerAsync(); // Fire and forget, handle result in the async method

            System.Diagnostics.Debug.WriteLine("MY_APP_TRACE: ExoPlayerService.OnCreate END (Initialization logic dispatched)");


        }
        catch (Java.Lang.Throwable ex) { HandleInitError("JAVA INITIALIZATION", ex); StopSelf(); }

    }
    private async Task InitializeMediaControllerAsync()
    {
        System.Diagnostics.Debug.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync START");
        try
        {
            if (mediaSession?.Token == null)
            {
                System.Diagnostics.Debug.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync - MediaSession token is null, cannot build controller.");
                return;
            }

            var controllerFuture = new MediaController.Builder(this, mediaSession.Token).BuildAsync();
            var controllerObject = await controllerFuture.GetAsync(); // Await here on a background context
            mediaController = (MediaController)controllerObject;
            System.Diagnostics.Debug.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync END - Controller built");
        }
        catch (Java.Lang.Throwable ex)
        {
            HandleInitError("MEDIA CONTROLLER INIT (Async)", ex);
            // Decide if you need to StopSelf() here or if the service can function without a controller initially
        }
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        base.OnStartCommand(intent, flags, startId);


        var notification = NotificationHelper.BuildMinimalNotification(this);

        StartForeground(NotificationHelper.NotificationId, notification);



        return StartCommandResult.Sticky;
    }


    public override IBinder OnBind(Intent? intent)
    {
        return _binder;
    }



    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? p0)
    {
        // Called by controllers connecting to the session. Return the session instance.
        if (mediaSession == null)
        {
            LogInitWarning($"OnGetSession from {p0?.PackageName}");
        }
        else
        {
            //Console.WriteLine($"[ExoPlayerService] OnGetSession: Returning session instance to {p0?.PackageName}.");
        }
        return mediaSession;
    }



    public override void OnDestroy()
    {

        //Console.WriteLine("[ExoPlayerService] OnDestroy");
        // Release resources in reverse order of creation
        mediaSession?.Release();
        player?.Release();
        mediaSession = null;
        player = null;
        sessionCallback = null; // Not strictly needed but good practice
        base.OnDestroy();

        StopPositionUpdates(); // Stop timer

        _positionRunnable?.Dispose(); // Dispose Runnable if needed


        _notifMgr?.SetPlayer(null); // Detach player
        _notifMgr?.Dispose();
        _binder = null;
    }

    // --- Public Accessors ---
    public IExoPlayer? GetPlayerInstance()
    {
        return player;
    }

    public MediaSession? GetMediaSessionInstance()
    {
        return mediaSession;
    }

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

        return PendingIntent.GetActivity(this, 0, launchIntent, flags)!;
    }

    private static void LogInitWarning(string stage)
    {
        //Console.WriteLine($"[ExoPlayerService] Warning: {stage} called but initialization may have failed or is incomplete.");
        //Console.WriteLine("---> Check previous logs for errors and verify NuGet packages are correct! <---");
    }
    private static void HandleInitError(string type, Java.Lang.Throwable ex)
    {
        //Console.WriteLine($"[ExoPlayerService] !!! CRITICAL JAVA {type} ERROR: {ex.Class.Name} - {ex.LocalizedMessage} !!!");
        //Console.WriteLine($"[ExoPlayerService] Java Stack Trace: {Log.GetStackTraceString(ex)}");
        if (ex.Cause != null)
        {
            //Console.WriteLine($"[ExoPlayerService] Java Cause: {ex.Cause}");
        }
        //Console.WriteLine("---> CHECK NuGet Packages: Media3.*, AndroidX.Concurrent.Futures, Guava/ListenableFuture <---");
        //Console.WriteLine("---> Check for Java class/method not found errors - often due to mismatched bindings. <---");
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
        SongModelView song,
        string? imagePath = null,
        long startPositionMs = 0)
    {
        CurrentSongItem=song;
        if (player is null)
        {

            throw new ArgumentException("Player not initialized.");

        }

        MediaMetadata.Builder metadataBuilder = new MediaMetadata.Builder()!
            .SetTitle(title)!
            .SetArtist(artist)!

            .SetAlbumTitle(album)!
            .SetUserRating(new HeartRating(true))! // Ensure HeartRating class exists
            .SetUserRating(new ThumbRating(true))! // Ensure HeartRating class exists
            .SetMediaType(new Java.Lang.Integer(MediaMetadata.MediaTypeMusic))! // Use Java Integer wrapper
            .SetIsPlayable(Java.Lang.Boolean.True)!; // Use Java Boolean wrapper

        // Set user rating (favorite status)
        //metadataBuilder.SetUserRating(new HeartRating(isFavorite)); // Ensure HeartRating class exists

        // Set artwork URI if available
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                metadataBuilder.SetArtworkUri(Uri.FromFile(new Java.IO.File(imagePath)));
            }
            catch (System.Exception ex)
            {
                //Console.WriteLine($"[ExoPlayerService] Warning: Failed to set ArtworkUri from path '{imagePath}': {ex.Message}");
            }
        }
        else if (!string.IsNullOrEmpty(imagePath))
        {
        }

        try
        {
            currentMediaItem = new MediaItem.Builder()!
               .SetMediaId(url)! // Use URL as Media ID for simplicity
               .SetUri(Uri.Parse(url))!
               .SetMediaMetadata(metadataBuilder!.Build())!
               .Build();

            //Console.WriteLine($"[ExoPlayerService] Setting MediaItem: ID={currentMediaItem.MediaId}, Pos={0}");
            player.SetMediaItem(currentMediaItem, 0); // Set item and start position

            player.AddMediaItem(currentMediaItem);
            player.AddMediaItem(currentMediaItem);
            player.Prepare();

            //player.Play(); // Start playback immediately

            //Console.WriteLine("[ExoPlayerService] Player Prepare() called.");

        }
        catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }


        return Task.CompletedTask;
    }

    public static async Task<List<AudioOutputDevice>> GetAvailableAudioOutputs()
    {
        // 1) grab the Android AudioManager
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;

        // 2) query all output devices (API 23+)
        var devices = audioManager?
            .GetDevices(GetDevicesTargets.Outputs)
            ?? [];

        // 3) map to your cross-platform model
        return [.. devices.Select(d => new AudioOutputDevice
        {
            Id   = d.Id.ToString(),
            Name = d.ProductNameFormatted?.ToString() ?? d.Type.ToString()


        })];
    }

    // --- Player Event Listener ---
    sealed class PlayerEventListener : Object, IPlayerListener // Use specific IPlayer.Listener
    {

        private readonly ExoPlayerService service;
        public PlayerEventListener(ExoPlayerService service) { this.service = service; }



        //[Obsolete("Media3 now prefers OnPlaybackStateChanged(int), but this must still be implemented.")]

        //public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
        //{
        //    var stateString = playbackState switch
        //    {
        //        1 => "Idle",
        //        2 => "Buffering",
        //        3 => "Ready",
        //        4 => "Ended",
        //        _ => "Unknown"
        //    };
        ////    Console.WriteLine($"[PlayerEventListener] PlayerStateChanged: PlayWhenReady={playWhenReady}, State={stateString} ({playbackState})");
        //    // Forward to your service or session as needed...

        //    service.RaiseStatusChanged(playbackState); // Use your service method to raise events
        //}
        public void OnPositionDiscontinuity(global::AndroidX.Media3.Common.PlayerPositionInfo? oldPosition, global::AndroidX.Media3.Common.PlayerPositionInfo? newPosition, int reason)
        {
            //Console.WriteLine($"[PlayerEventListener] OnPositionDiscontinuity:");

            //Console.WriteLine($"  Old Position: {oldPosition?.PositionMs}ms");
            //Console.WriteLine($"  New Position: {newPosition?.PositionMs}ms");

            Log.WriteLine(LogPriority.Info, "MyAppSeekDebug", $"*** OnPositionDiscontinuity Entered! Reason={reason} ***");
            Log.Debug("PlayerEventListener", $"OnPositionDiscontinuity Detail: Reason={reason}, From={oldPosition?.PositionMs ?? -1}, To={newPosition?.PositionMs ?? -1}");

            if (reason == 1)
            {
                //Console.WriteLine($"  Reason: {reason} == Seek normally"); // Check this against Player.DISCONTINUITY_REASON_ constants
                service.RaiseSeekCompleted((double)newPosition?.PositionMs);
            }

        }
        public void OnPlaybackStateChanged(int playbackState)
        {
            var stateString = playbackState switch
            {
                1 => "Idle",
                2 => "Buffering",
                3 => "Ready",
                4 => "Ended",
                _ => "Unknown"
            };
            //Console.WriteLine($"[PlayerEventListener] State={stateString} ({playbackState})");
            // Forward to your service or session as needed...
            if (playbackState == 4)
            {
                service.RaiseStatusChanged(DimmerPlaybackState.PlayCompleted); // Use your service method to raise events
            }
        }
        public void OnLoadingChanged(bool isLoading)
        {

            //Console.WriteLine("new is loading: "+isLoading);
        }

        public void OnIsPlayingChanged(bool isPlaying)
        {
            if (isPlaying)
            {
                //QuickSettingsTileService.RequestTileUpdate(Platform.AppContext.ApplicationContext);
                //NotificationHelper.ShowPlaybackBubble(Platform.AppContext.ApplicationContext, service.player.MediaMetadata.Title.ToString());
            }
            //Console.WriteLine($"[PlayerEventListener] IsPlayingChanged: {isPlaying}");

            service.RaiseIsPlayingChanged(isPlaying);
        }

        public void OnPlayerError(PlaybackException? error) // Use specific PlaybackException
        {
            if (error is null)
                return;
            //Console.WriteLine($"[PlayerEventListener] PlayerError: Code={error.ErrorCodeName}, ChatMessage={error.Message}");
            // Log the full error

            // Optionally stop the player or service on error
            service.RaiseErrorOccurred(error);
            service.player?.Stop();
            service.StopSelf();
        }

        public void OnPlayerErrorChanged(PlaybackException? error) // Nullable error
        {
            if (error != null)
            {
                //Console.WriteLine($"[PlayerEventListener] PlayerErrorChanged: Code={error.ErrorCodeName}, ChatMessage={error.Message}");


                service.RaiseErrorOccurred(error);
            }
            else
            {
                //Console.WriteLine("[PlayerEventListener] PlayerErrorChanged: Error cleared.");
            }
        }

        public void OnMediaMetadataChanged(MediaMetadata? mediaMetadata)
        {
            service.RaiseCoverReloaded();

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
            //Console.WriteLine($"[PlayerEventListener] MediaItemTransition: Item='{mediaItem?.MediaId ?? "None"}', Reason={reasonString} ({reason})");
            //QuickSettingsTileService.RequestTileUpdate(Platform.AppContext.ApplicationContext);

        }

        // --- Other IPlayer.Listener Methods (Implement if needed, stubs are often sufficient) ---
        public void OnAudioAttributesChanged(AudioAttributes? audioAttributes) { /* Log if needed */ }
        public void OnAudioSessionIdChanged(int p0) { /* Log if needed */ }
        public void OnAvailableCommandsChanged(PlayerCommands? availableCommands)
        {

        }
        public void OnCues(CueGroup? p0) { /* Handle subtitles/cues */ }
        public void OnDeviceInfoChanged(DeviceInfo? deviceInfo)
        {

            /* Log if needed */
        }
        public void OnDeviceVolumeChanged(int volume, bool muted)
        {
            //Console.WriteLine($"[PlayerEventListener] DeviceVolumeChanged: Volume={volume}, Muted={muted}");
            /* Log if needed */
        }
        public void OnEvents(IPlayer? player, PlayerEvents? events)
        {

            if (events == null || player == null)
                return;
            // Example checks (Constants are in IPlayer):
            var siz = events.Size();
            //Console.WriteLine(events.Size());

            for (int i = 0; i < siz; i++)
            {
                var eventType = events.Get(i);
                //Console.WriteLine($"Event {i}: {eventType}");
                if (eventType == 11)
                {
                    //Console.WriteLine($"Song ended {player.MediaMetadata.Title} {DateTime.Now}");
                }
            }
            // Example inspection:

            // ... other checks

        }
        public void OnIsLoadingChanged(bool isLoading)
        {
            //Console.WriteLine($"[PlayerEventListener] IsLoadingChanged: {isLoading}");
        }

        public void OnMaxSeekToPreviousPositionChanged(long p0) { /* Log if needed */ }
        // public void OnMetadata(MediaMetadata? volume) {} // Superseded by OnMediaMetadataChanged? Check docs.
        //public void OnPlayWhenReadyChanged(bool playWhenReady, int reason) { Console.WriteLine($"[PlayerEventListener] PlayWhenReadyChanged: {playWhenReady}, Reason={reason}"); }
        public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters)
        {
            //Console.WriteLine($"[PlayerEventListener] PlaybackParametersChanged: {playbackParameters?.Speed}, {playbackParameters?.Pitch}");
            /* Log if needed */
        }
        //public void OnPlaybackSuppressionReasonChanged(int reason) { Console.WriteLine($"[PlayerEventListener] PlaybackSuppressionReasonChanged: {reason}"); }

        //public void OnRenderedFirstFrame() { Console.WriteLine($"[PlayerEventListener] RenderedFirstFrame"); }
        public void OnRepeatModeChanged(int p0) { /* Log if needed */ }
        public void OnSeekBackIncrementChanged(long p0) { /* Log if needed */ }
        public void OnSeekForwardIncrementChanged(long p0) { /* Log if needed */ }
        public void OnShuffleModeEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSkipSilenceEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSurfaceSizeChanged(int p0, int p1) { /* Video related */ }
        public void OnTimelineChanged(Timeline? timeline, int reason)
        {
            //Console.WriteLine(timeline.IsEmpty);
            //Console.WriteLine(timeline.WindowCount);
            //Console.WriteLine(timeline.PeriodCount);

            //Console.WriteLine("SKIP??");
            //Console.WriteLine($"[PlayerEventListener] TimelineChanged: Reason={reason}");

        }
        public void OnTrackSelectionParametersChanged(TrackSelectionParameters? p0) { /* Log if needed */ }
        public void OnTracksChanged(Tracks? tracks)
        {
            //Console.WriteLine(tracks?.ToString());
            /* Log if needed */
        }
        public void OnVideoSizeChanged(VideoSize? p0) { /* Video related */ }
        public void OnVolumeChanged(float volume)
        {
            //Console.WriteLine("New Volume "+volume);
        }

    } // End PlayerEventListener


    // --- Media Session Callback (Handles Controller Commands) ---
    sealed class MediaPlaybackSessionCallback : Object, MediaSession.ICallback
    {

        readonly ExoPlayerService service;
        public MediaPlaybackSessionCallback(ExoPlayerService service)
        {
            this.service = service;
        }

        const string TAG = "MediaSessionCallback";
        // --- Connection Handling ---
        public MediaSession.ConnectionResult OnConnect(
  MediaSession? session,
  MediaSession.ControllerInfo? controller)
        {
            //Console.WriteLine($"[{TAG}] OnConnect from {controller?.PackageName}");

            var sessionCommands = new SessionCommands.Builder()
                   .Add(SessionCommand.CommandCodeSessionSetRating)!
                  .Build();
            var playerCommands = new PlayerCommands.Builder()
              .AddAllCommands()!
              .Build();
            //Console.WriteLine();
            return MediaSession.ConnectionResult.Accept(sessionCommands, playerCommands)!;
        }
        public void OnPostConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        {

            //Console.WriteLine($"[SessionCallback] OnPostConnect: Controller {controller?.PackageName} connected.");

        }

        public void OnDisconnected(MediaSession? session, MediaSession.ControllerInfo? controller)
        {

            //Console.WriteLine($"[SessionCallback] OnDisconnected: Controller {controller?.PackageName} disconnected.");

        }

        // --- Command Handling ---


        public async static Task<bool> OnMediaButtonEvent(global::AndroidX.Media3.Session.MediaSession? session, global::AndroidX.Media3.Session.MediaSession.ControllerInfo? controllerInfo, global::Android.Content.Intent? intent)
        {

            if (intent == null || intent.Action == null)
                return false;

            //Console.WriteLine($"[SessionCallback] OnMediaButtonEvent: Received intent {intent.Action}");
            await Shell.Current.DisplayAlert("Media Button Event", $"Received intent: {intent.Action}", "OK");
            return true;
        }
        // Decide whether to allow a specific PLAYER command requested by a controllerInfo
        public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controller, int playerCommand)
        {
            //Console.WriteLine($"[SessionCallback] OnPlayerCommandRequest: Command={playerCommand} from. Allowing.");

            return SessionResult.ResultSuccess;
        }
        public void OnPlayerInteractionFinished(MediaSession? session, MediaSession.ControllerInfo? controllerInfo, PlayerCommands? playerCommands)
        {
            //Console.WriteLine($"[SessionCallback] OnPlayerInteractionFinished: Controller={controllerInfo?.PackageName} Commands={playerCommands?.Size() ?? 0}");

            #region Old code. still useful in case
            if (playerCommands == null || playerCommands.Size() == 0)
            {
                //Console.WriteLine("[SessionCallback] No specific playerCommands reported (system change?)");
                return;
            }

            for (int i = 0; i < playerCommands.Size(); i++)
            {
                int command = playerCommands.Get(i);

                switch (command)
                {
                    //case 0:
                    ////    //Console.WriteLine("[SessionCallback] User pressed PLAY/PAUSE button.");
                    //    break;

                    //case 1:
                    ////    Console.WriteLine("[SessionCallback] User pressed PLAY/PAUSE button.");
                    //    break;

                    //case 5:
                    ////    Console.WriteLine("[SessionCallback] User seeked to a position. "+session?.Player?.CurrentPosition);
                    //    service.RaiseSeekCompleted(session?.Player?.CurrentPosition ?? 0);

                    //    break;

                    case 9:
                        service.player!.Stop();
                        service.RaisePlayNextEventHandler();
                        //Console.WriteLine("[SessionCallback] User pressed NEXT button.");
                        break;

                    case 7:
                        service.player!.Stop();
                        service.RaisePlayPreviousEventHandler();

                        break;

                    default:
                        //Console.WriteLine($"[SessionCallback] Unknown command: {command}");
                        break;
                }
            }

            #endregion
        }




    }

    private IPlaybackBubbleUpdateListener? _bubbleListener;



} // End ExoPlayerService class

public class ExoPlayerServiceBinder : Binder
{
    public ExoPlayerService Service { get; }


    internal ExoPlayerServiceBinder(ExoPlayerService service)
    {
        Service = service;
    }
}