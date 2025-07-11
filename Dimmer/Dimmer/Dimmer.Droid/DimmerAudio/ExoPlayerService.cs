#region Using Directives
// Android Core
using Android.App;
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

using MediaController = AndroidX.Media3.Session.MediaController;

using Dimmer.Utilities.Events;
using Dimmer.ViewModel;
using Dimmer.Orchestration;

using static Android.Icu.Text.CaseMap;
using static Android.Provider.MediaStore.Audio;

using Java.Net;


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
    public SongModelView? CurrentSongContext; // Choose a unique ID

    // --- Service Lifecycle ---
    private ExoPlayerServiceBinder? _binder;

    //public event StatusChangedEventHandler? StatusChanged;
    //public event BufferingEventHandler? Buffering;
    //public event CoverReloadedEventHandler? CoverReloaded;

    //public event PlayingChangedEventHandler? PlayingChanged;
    //public event PlayingChangedEventHandler? PlayingEnded;
    //public event PlayingChangedEventHandler? PlayListEnded;
    //public event PositionChangedEventHandler? PositionChanged;
    //public event SeekCompletedEventHandler? SeekCompleted;
    //public event PlayNextEventHandler? PlayNextPressed; // Triggered by MediaKeyNextPressed
    //public event PlayPreviousEventHandler? PlayPreviousPressed; // Triggered by MediaKeyPreviousPressed
    //public event EventHandler<long>? DurationChanged;
    //public event EventHandler<double>? SeekCompleted; // Triggered after a seek operation completes

    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayingEnded;
    public event EventHandler<long>? PositionChanged; // Changed to long (ms) for directness from player
    public event EventHandler<double>? SeekCompleted; // Changed to double (seconds) for the ViewModel
    public event EventHandler<PlaybackEventArgs>? PlayNextPressed;
    public event EventHandler<PlaybackEventArgs>? PlayPreviousPressed;

    internal void RaisePlaybackStateChanged(DimmerPlaybackState state) =>
    PlaybackStateChanged?.Invoke(this, new PlaybackEventArgs(CurrentSongContext) { EventType = state });

    internal void RaiseIsPlayingChanged(bool isPlaying) =>
        IsPlayingChanged?.Invoke(this, new PlaybackEventArgs(CurrentSongContext) { IsPlaying = isPlaying });

    internal void RaisePlayingEnded() =>
        PlayingEnded?.Invoke(this, new PlaybackEventArgs(CurrentSongContext));

    internal void RaisePositionChanged(long positionMs) =>
        PositionChanged?.Invoke(this, positionMs);

    internal void RaiseSeekCompleted(long? positionMs) =>
        SeekCompleted?.Invoke(this, (positionMs ?? 0) / 1000.0);

    internal void RaisePlayNextPressed() =>
        PlayNextPressed?.Invoke(this, new PlaybackEventArgs(CurrentSongContext));

    internal void RaisePlayPreviousPressed() =>
        PlayPreviousPressed?.Invoke(this, new PlaybackEventArgs(CurrentSongContext));

    PlayerNotificationManager? _notifMgr;

    private Handler? _positionHandler;
    private Runnable? _positionRunnable;
    private MediaController? mediaController;

    public ExoPlayerServiceBinder? Binder { get => _binder; set => _binder = value; }

    internal static void GetMaxVolumeLevel()
    {
        // 1) grab the Android AudioManager
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;
        var ss = audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
        Console.WriteLine($"Max Volume Level: {ss}");
    }


    public List<AudioOutputDevice> GetAvailableAudioOutputMAUI()
    {
        // 1) grab the Android AudioManager
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;

        // 2) query all output devices (API 23+)
        var devices = audioManager?
            .GetDevices(GetDevicesTargets.Outputs)
            ?? [];


        // 3) map to your cross-platform model
        var we = devices.Select(d => new AudioOutputDevice
        {
            Id   = d.Id.ToString(),
            Name = d.ProductNameFormatted?.ToString() ?? d.Type.ToString(),
            Type =d.Type.ToString(),
            IsSource=d.IsSource
        });
        foreach (var item in we)
        {
            Console.WriteLine(item.Id);
            Console.WriteLine(item.Name);
        }
        return [.. we];
    }

    internal static List<AudioDeviceInfo> GetAvailableAudioOutputs()
    {
        // 1) grab the Android AudioManager
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;

        // 2) query all output devices (API 23+)
        AudioDeviceInfo[]? devices = audioManager?
            .GetDevices(GetDevicesTargets.Outputs)
            ?? [];

        return [.. devices];

    }
    public bool SetPreferredDevice(AudioOutputDevice dev)
    {
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;

        // 2) query all output devices (API 23+)
        AudioDeviceInfo[]? devices = audioManager?
            .GetDevices(GetDevicesTargets.Outputs)
            ?? [];
        var specDev = devices.FirstOrDefault(x => x.Id== int.Parse(dev.Id));
        if (specDev is not null)
        {
            player.SetPreferredAudioDevice(specDev);
            return true;
        }
        return false;
    }




    public async override void OnCreate()
    {
        base.OnCreate();

        //Console.WriteLine("[ExoPlayerService] OnCreate");

        try
        {
            var audioAttributes = new AudioAttributes.Builder()!
            .SetUsage(C.UsageMedia)! // Specify this is media playback
            .SetContentType(C.AudioContentTypeMusic)! // Specify the content is music
            .Build();

            player = new ExoPlayerBuilder(this)
                .SetAudioAttributes(audioAttributes, true)!
                .SetHandleAudioBecomingNoisy(true)!
                .SetWakeMode(C.WakeModeNetwork)!
                .SetSkipSilenceEnabled(false)!
                //.SetSeekParameters(new SeekParameters(10,10))
                .SetDeviceVolumeControlEnabled(true)!
                .SetSuppressPlaybackOnUnsuitableOutput(false)!

                .Build();

            player?.AddListener(new PlayerEventListener(this));

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
        string? title,
        string? artist,
        string? album,
        SongModelView song,
        string? imagePath = null,
        long startPositionMs = 0)
    {
        CurrentSongContext=song;
        if (player is null)
        {

            throw new ArgumentException("Player not initialized.");

        }
        var genre = song.Genre?.Name;
        player.Stop();
        player.ClearMediaItems();



        MediaMetadata.Builder metadataBuilder = new MediaMetadata.Builder()!
            .SetTitle(title)
            .SetArtist(artist)
            .SetAlbumTitle(album)
            .SetMediaType(new Java.Lang.Integer(MediaMetadata.MediaTypeMusic))! // Use Java Integer wrapper
            .SetGenre(genre)

            .SetIsPlayable(Java.Lang.Boolean.True)!; // Use Java Boolean wrapper

        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                metadataBuilder.SetArtworkUri(Uri.FromFile(new Java.IO.File(imagePath)));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ExoPlayerService] Warning: Failed to set ArtworkUri from path '{imagePath}': {ex.Message}");
            }
        }
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                metadataBuilder.SetArtworkUri(Uri.FromFile(new Java.IO.File(imagePath)));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"[ExoPlayerService] Warning: Failed to set ArtworkUri from path '{imagePath}': {ex.Message}");
            }
        }


        try
        {
            currentMediaItem = new MediaItem.Builder()!
               .SetMediaId(url)! // Use URL as Media ID for simplicity
               .SetUri(Uri.Parse(url))!
               .SetMediaMetadata(metadataBuilder!.Build())!
               .Build();


            player.SetMediaItem(currentMediaItem, 0); // Set item and start position
            player.AddMediaItem(currentMediaItem);
            player.Prepare();

        }
        catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }


        return Task.CompletedTask;
    }


    // --- Player Event Listener ---
    sealed class PlayerEventListener : Object, IPlayerListener // Use specific IPlayer.Listener
    {

        private readonly ExoPlayerService service;
        public PlayerEventListener(ExoPlayerService service) { this.service = service; }



        public void OnPositionDiscontinuity(global::AndroidX.Media3.Common.PlayerPositionInfo? oldPosition, global::AndroidX.Media3.Common.PlayerPositionInfo? newPosition, int reason)
        {
            if (reason == 1)
            {
                service.RaiseSeekCompleted(newPosition.PositionMs);
            }

        }
        public void OnPlaybackStateChanged(int playbackState)
        {
            if (playbackState == 4)
            {
                service.RaisePlayingEnded();
            }
        }
        public void OnLoadingChanged(bool isLoading)
        {

            //Console.WriteLine("new is loading: "+isLoading);
        }

        public void OnIsPlayingChanged(bool isPlaying)
        {
            service.RaiseIsPlayingChanged(isPlaying);

            if (isPlaying)
            {
                var state = (service.player?.CurrentPosition > 500)
                    ? DimmerPlaybackState.Resumed
                    : DimmerPlaybackState.Playing;
                service.RaisePlaybackStateChanged(state);
            }
            else
            {
                if (service.player?.PlaybackState != 4)
                {
                    service.RaisePlaybackStateChanged(DimmerPlaybackState.PausedUser);
                }
            }
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
            if (events.Contains(11))
            {
                service.RaisePositionChanged(player.CurrentPosition);
            }

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

        public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
        {
            // This is vital for playlists. When a song finishes and the next one starts automatically...
            if (reason == 1 && mediaItem != null)
            {
                // ...you need to update the service's context.
                // This requires a way to look up the full SongModelView from the ID.
                // For now, a placeholder shows the concept:

                // Find the full song details from a repository or a cached list
                // SongModelView newSongContext = MySongRepository.GetById(mediaItem.MediaId);
                // service.CurrentSongContext = newSongContext;

                System.Diagnostics.Debug.WriteLine($"[ExoPlayerService] Transitioned to new song: {mediaItem.MediaId}");
            }
        }
        public void OnPlayerError(PlaybackException? error)
        {
            // It's crucial to have this method to handle errors.
            // At a minimum, you should log it.
            System.Diagnostics.Debug.WriteLine($"[ExoPlayerService] PLAYER ERROR: {error.Message}");
            // You could also raise a service event here to notify the UI.
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

                    case 9:
                        service.RaisePlayNextPressed();
                        break;

                    case 7:
                        service.RaisePlayPreviousPressed();

                        break;

                    default:
                        //Console.WriteLine($"[SessionCallback] Unknown command: {command}");
                        break;
                }
            }

            #endregion
        }




    }
    public void PreparePlaylist(SongModelView songToPlay, IEnumerable<SongModelView> songs)
    {
        if (player is null)
            return;

        // Set the initial context to the song that should start playing.
        CurrentSongContext = songToPlay;

        // Convert all your SongModelViews into ExoPlayer MediaItems
        var mediaItems = songs.Select(s =>
        {
            var metadata = new MediaMetadata.Builder()
                .SetTitle(s.Title)
                .SetArtist(s.ArtistName)
                .Build(); // Add more metadata as needed

            return new MediaItem.Builder()
                .SetMediaId(s.Id.ToString()) // Use a unique ID
                .SetUri(Uri.Parse(s.FilePath))
                .SetMediaMetadata(metadata)
                .Build();
        }).ToList();

        // Find the index of the song we want to start with
        int startIndex = songs.ToList().FindIndex(s => s.Id == songToPlay.Id);
        if (startIndex == -1)
        {
            startIndex = 0; // Default to the start if not found
        }

        // Give the entire playlist to the player and tell it where to start.
        player.SetMediaItems(mediaItems, startIndex, C.TimeUnset);
        player.Prepare();
    }

} // End ExoPlayerService class

public class ExoPlayerServiceBinder : Binder
{
    public ExoPlayerService Service { get; }


    internal ExoPlayerServiceBinder(ExoPlayerService service)
    {
        Service = service;
    }
}

