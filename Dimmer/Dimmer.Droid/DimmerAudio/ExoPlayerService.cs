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
    public static SongModelView? CurrentSongContext; // Choose a unique ID
    public SongModelView? CurrentSongExposed => CurrentSongContext;
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




    private IPlayer? notificationPlayer;
    public async override void OnCreate()
    {
        base.OnCreate();




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
                .SetPauseAtEndOfMediaItems(true )
                //.SetPauseAtEndOfMediaItems(true) could use this in combo with 
                //is play changed but i'll need to expose the player position
                
                .Build();

            notificationPlayer = new QueueEnablingPlayerWrapper(player!);


            player?.AddListener(new PlayerEventListener(this));

            sessionCallback = new MediaPlaybackSessionCallback(this); // Use concrete type


            Intent nIntent = new Intent(Platform.AppContext, typeof(MainActivity));

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Or BuildVersionCodes.M for broader compatibility with Immutable
            {
                flags |= PendingIntentFlags.Immutable;
            }
            PendingIntent? pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, flags);


            mediaSession = new MediaSession.Builder(this, notificationPlayer)!
                .SetSessionActivity(pendingIntent)!
                .SetCallback(sessionCallback)!
                .SetId("Dimmer_MediaSession_Main")!
                .Build();

            _binder = new ExoPlayerServiceBinder(this);

            // 2) NotificationManager
            NotificationHelper.CreateChannel(this);
            _notifMgr = NotificationHelper.BuildManager(this, mediaSession!, CurrentSongContext);

            _notifMgr.SetPlayer(notificationPlayer);



            await InitializeMediaControllerAsync(); // Fire and forget, handle result in the async method

            positionHandler = new Handler(Looper.MainLooper!);
            positionRunnable = new Runnable(() =>
            {
                // This is the polling loop
                if (player != null && player.IsPlaying)
                {
                    // Raise our event with the current position
                    RaisePositionChanged(player.CurrentPosition);
                    // Schedule the next check
                    positionHandler?.PostDelayed(positionRunnable, 1000); // Poll every 1 second
                }
            });

        }
        catch (Java.Lang.Throwable ex) { HandleInitError("JAVA INITIALIZATION", ex); StopSelf(); }

    }
    private Handler? positionHandler;
    private Runnable? positionRunnable;
    private async Task InitializeMediaControllerAsync()
    {
        Console.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync START");
        try
        {
            if (mediaSession?.Token == null)
            {
                Console.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync - MediaSession token is null, cannot build controller.");
                return;
            }

            var controllerFuture = new MediaController.Builder(this, mediaSession.Token).BuildAsync();
            var controllerObject = await controllerFuture.GetAsync(); // Await here on a background context
            mediaController = (MediaController?)controllerObject;
            Console.WriteLine("MY_APP_TRACE: ExoPlayerService.InitializeMediaControllerAsync END - Controller built");
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


    public override IBinder? OnBind(Intent? intent)
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

        }
        return mediaSession;
    }



    public override void OnDestroy()
    {
        positionHandler?.RemoveCallbacks(positionRunnable!);

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


    }
    private static void HandleInitError(string type, Java.Lang.Throwable ex)
    {


        if (ex.Cause != null)
        {

        }


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


        try
        {
            CurrentSongContext =song;
        if (player is null)
        {

            throw new ArgumentException("Player not initialized.");

        }
        var genre = song.Genre?.Name;
        player.Stop();
        player.ClearMediaItems();

        //player.PlaybackLooper

        MediaMetadata.Builder? metadataBuilder = new MediaMetadata.Builder()!
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


            currentMediaItem = new MediaItem.Builder()!
               .SetMediaId(song.Id.ToString())! 
               .SetUri(Uri.Parse(url))!
               .SetMediaMetadata(metadataBuilder!.Build())!
               .Build();


            player.SetMediaItem(currentMediaItem, 0); // Set item and start position
            player.AddMediaItem(currentMediaItem);
            
            player.Prepare();

            player.Play();
            player.SeekTo(startPositionMs);
            if (player.CurrentMediaItem is null || player.CurrentMediaItem.MediaMetadata is null)
            {
                return Task.CompletedTask;
            }
            var awDT = player.CurrentMediaItem.MediaMetadata.ArtworkUri;
            var awDTT = player.CurrentMediaItem.MediaMetadata.ArtworkData;
            if (awDT is not null && !string.IsNullOrEmpty(awDT.Path))
            {
                CurrentSongContext.CoverImagePath=awDT.Path;
            }
            else
            {
                //CurrentSongContext.CoverImagePath = string.Empty;

            }
        }
        catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }


        return Task.CompletedTask;
    }

    internal static void UpdateFavoriteState(SongModelView song)
    {
        Toast.MakeText(Platform.AppContext, "Opening synced lyrics...", ToastLength.Short)?.Show();

    }


    // --- Player Event Listener ---
    sealed class PlayerEventListener : Object, IPlayerListener // Use specific IPlayer.Listener
    {

        private readonly ExoPlayerService service;
        public PlayerEventListener(ExoPlayerService service) { this.service = service; }

        private bool _endRaisedForThisItem = false;
        private string? _lastMediaId;
        public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
        {


            //if (mediaItem == null) return;

            //_endRaisedForThisItem = false;
            //string? newId = mediaItem.MediaId;

            //// Reason 1 = AUTO / NEXT transition (ExoPlayer constants)
            //// If same song reappears (duplicate entry in playlist)
            //if (newId == _lastMediaId)
            //{
            //    service.player?.Pause(); // immediate cut — no 1s replay
            //    service.RaisePlayingEnded();
            //    Console.WriteLine($"[ExoPlayerService] Duplicate transition intercepted → {newId}");
            //    return;
            //}

            //// Normal transition
            //_lastMediaId = newId;

            ////// Update service context (look up full SongModelView by ID)
            ////var newSongContext = SongRepository.GetById(newId);
            ////service.CurrentSongContext = newSongContext;

            Console.WriteLine($"[ExoPlayerService] MediaItemTransition: Reason={reason}");
        }

        public void OnPositionDiscontinuity(global::AndroidX.Media3.Common.PlayerPositionInfo? oldPosition, global::AndroidX.Media3.Common.PlayerPositionInfo? newPosition, int reason)
        {
            // reason = 0 AUTO, 1 SEEK, 4 REMOVE, etc.
            Console.WriteLine($"{DateTime.Now} ► PositionDiscontinuity reason={reason}");

            //// Safety fallback: duplicate transition via auto-advance
            //if (reason == 0 &&
            //    oldPosition?.MediaItem?.MediaId != null &&
            //    newPosition?.MediaItem?.MediaId == oldPosition.MediaItem.MediaId)
            //{
            //    service.player?.Pause();
            //    service.RaisePlayingEnded();
            //    Console.WriteLine("[ExoPlayerService] Duplicate discontinuity stopped early");
            //    return;
            //}

            if (reason == 1 && newPosition != null)
            {
                service.RaiseSeekCompleted(newPosition.PositionMs);
                return;
            }

            // (Keep logs if needed)
            if (oldPosition?.MediaItem != null)
                Console.WriteLine($"Old={oldPosition.MediaItem.MediaId} Pos={oldPosition.PositionMs}ms");
            if (newPosition?.MediaItem != null)
                Console.WriteLine($"New={newPosition.MediaItem.MediaId} Pos={newPosition.PositionMs}ms");
        }
        public void OnPlaybackStateChanged(int playbackState)
        {
            Console.WriteLine($"{DateTime.Now}ccccccccccccccccccccccccccccccccccccccc state changed and new state is {playbackState}");

            //if (playbackState == 4)
            //{
            //    Console.WriteLine($"{DateTime.Now}!!!!!!!!!!!!!!!!!!!!!!!!! state changed and new state is {playbackState}");
            //}
            //    //    service.player?.Pause(); // pause instead of stop for smoother state handover
            //    //    service.RaisePlayingEnded();
            //    //    Console.WriteLine("[ExoPlayerService] Playback ended → event raised");
            //    //}
        }
        public void OnLoadingChanged(bool isLoading)
        {


        }

        public void OnIsPlayingChanged(bool isPlaying)
        {
            if (isPlaying)
            {
                // When playback starts, kick off the position polling runnable.
                service.positionHandler?.Post(service.positionRunnable!);
            }
            else
            {
                // When playback stops (pause or end), remove the callback to stop polling.
                service.positionHandler?.RemoveCallbacks(service.positionRunnable!);
            }

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

        public void OnPlayWhenReadyChanged(bool playWhenReady, int reason)
        {
            Console.WriteLine($"[PlayerEventListener]$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$ PlayWhenReadyChanged: {playWhenReady}, Reason={reason}");
            const int END_OF_ITEM = 5;

            if (!playWhenReady && reason == END_OF_ITEM)
            {

                // We’re exactly at the last frame of the CURRENT item.
                // Stop/pause to prevent Exo from starting the duplicate entry.
                
                service.RaisePlayingEnded();

                Console.WriteLine("[ExoPlayerService] END_OF_MEDIA_ITEM intercepted (pauseAtEndOfMediaItems)");
            }
        }

        public void OnPlaybackSuppressionReasonChanged(int reason)
        {
            Console.WriteLine($"{DateTime.Now}@@@@@@@@@@@@@@@@@@@@@@@@@ [PlayerEventListener] PlaybackSuppressionReasonChanged: {reason}");
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

            /* Log if needed */
        }

        public void OnIsLoadingChanged(bool isLoading)
        {

        }

        public void OnMaxSeekToPreviousPositionChanged(long p0) { /* Log if needed */ }
        // public void OnMetadata(MediaMetadata? volume) {} // Superseded by OnMediaMetadataChanged? Check docs.
       
        public void OnPlaybackParametersChanged(PlaybackParameters? playbackParameters)
        {

            /* Log if needed */
        }

        //public void OnRenderedFirstFrame() { Console.WriteLine($"[PlayerEventListener] RenderedFirstFrame"); }
        public void OnRepeatModeChanged(int p0) { /* Log if needed */ }
        public void OnSeekBackIncrementChanged(long p0) { /* Log if needed */ }
        public void OnSeekForwardIncrementChanged(long p0) { /* Log if needed */ }
        public void OnShuffleModeEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSkipSilenceEnabledChanged(bool p0) { /* Log if needed */ }
        public void OnSurfaceSizeChanged(int p0, int p1) { /* Video related */ }
        public void OnTimelineChanged(Timeline? timeline, int reason)
        {
            Console.WriteLine($"{DateTime.Now}  [PlayerEventListener] TimelineChanged: {timeline?.ToString()} Reason={reason}");
        }
        public void OnTrackSelectionParametersChanged(TrackSelectionParameters? p0) { /* Log if needed */ }
        public void OnTracksChanged(Tracks? tracks)
        {
            Console.WriteLine($"{DateTime.Now} [PlayerEventListener] TracksChanged: {tracks?.ToString()}");
            /* Log if needed */
        }
        public void OnVideoSizeChanged(VideoSize? p0) { /* Video related */ }
        public void OnVolumeChanged(float volume)
        {

        }
        
        public void OnPlayerError(PlaybackException? error)
        {
            // It's crucial to have this method to handle errors.
            // At a minimum, you should log it.
            Console.WriteLine($"[ExoPlayerService] PLAYER ERROR: {error.Message}");
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


            var sessionCommands = new SessionCommands.Builder()
                   .Add(SessionCommand.CommandCodeSessionSetRating)!
                  .Build();
            var playerCommands = new PlayerCommands.Builder()
              .AddAllCommands()!
              .Build();

            return MediaSession.ConnectionResult.Accept(sessionCommands, playerCommands)!;
        }
        public void OnPostConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        {



        }

        public void OnDisconnected(MediaSession? session, MediaSession.ControllerInfo? controller)
        {



        }

        // --- Command Handling ---


        public async static Task<bool> OnMediaButtonEvent(global::AndroidX.Media3.Session.MediaSession? session, global::AndroidX.Media3.Session.MediaSession.ControllerInfo? controllerInfo, global::Android.Content.Intent? intent)
        {

            if (intent == null || intent.Action == null)
                return false;


            await Shell.Current.DisplayAlert("Media Button Event", $"Received intent: {intent.Action}", "OK");
            return true;
        }
        // Decide whether to allow a specific PLAYER command requested by a controllerInfo
        public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controller, int playerCommand)
        {


            return SessionResult.ResultSuccess;
        }
        public void OnPlayerInteractionFinished(MediaSession? session, MediaSession.ControllerInfo? controllerInfo, PlayerCommands? playerCommands)
        {


            #region Old code. still useful in case
            if (playerCommands == null || playerCommands.Size() == 0)
            {

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

                        break;
                }
            }

            #endregion
        }




    }

    //frowarding player to allow queueing
    public class QueueEnablingPlayerWrapper : Java.Lang.Object, IPlayer
    {
        private readonly IPlayer _realPlayer;

        public QueueEnablingPlayerWrapper(IPlayer realPlayer)
        {
            _realPlayer = realPlayer;
        }


        // Properties
        public Looper? ApplicationLooper => _realPlayer.ApplicationLooper;
        public AudioAttributes? AudioAttributes => _realPlayer.AudioAttributes;
        public PlayerCommands? AvailableCommands
        {
            get
            {
                // 1. Get the commands the real ExoPlayer instance thinks it has.
                var realCommands = _realPlayer.AvailableCommands;

                // 2. Create a new builder based on the real commands.
                var builder = new PlayerCommands.Builder();

                builder.AddAll(realCommands);

                // 3. Forcefully add the commands for Next and Previous,
                //    because WE know how to handle them in our app.
                //builder.Add(_realPlayer.);
                //builder.Add(IPlayer.CommandSeekToPrevious);
                // You can also use CommandSeekToNextMediaItem if you prefer
                // builder.Add(IPlayer.CommandSeekToNextMediaItem); 
                // builder.Add(IPlayer.CommandSeekToPreviousMediaItem);

                // 4. Build and return the new, augmented set of commands.
                return builder.Build();
            }
        }
        public int BufferedPercentage => _realPlayer.BufferedPercentage;
        public long BufferedPosition => _realPlayer.BufferedPosition;
        public long ContentBufferedPosition => _realPlayer.ContentBufferedPosition;
        public long ContentDuration => _realPlayer.ContentDuration;
        public long ContentPosition => _realPlayer.ContentPosition;
        public int CurrentAdGroupIndex => _realPlayer.CurrentAdGroupIndex;
        public int CurrentAdIndexInAdGroup => _realPlayer.CurrentAdIndexInAdGroup;
        public CueGroup? CurrentCues => _realPlayer.CurrentCues;
        public long CurrentLiveOffset => _realPlayer.CurrentLiveOffset;
        public Java.Lang.Object? CurrentManifest => _realPlayer.CurrentManifest;
        public MediaItem? CurrentMediaItem => _realPlayer.CurrentMediaItem;
        public int CurrentMediaItemIndex => _realPlayer.CurrentMediaItemIndex;
        public int CurrentPeriodIndex => _realPlayer.CurrentPeriodIndex;
        public long CurrentPosition => _realPlayer.CurrentPosition;
        public Timeline? CurrentTimeline => _realPlayer.CurrentTimeline;
        public Tracks? CurrentTracks => _realPlayer.CurrentTracks;
        [System.Obsolete] public int CurrentWindowIndex => _realPlayer.CurrentWindowIndex;
        public DeviceInfo? DeviceInfo => _realPlayer.DeviceInfo;
        public bool IsDeviceMuted { get => _realPlayer.DeviceMuted; set => _realPlayer.DeviceMuted = value; }
        public int DeviceVolume { get => _realPlayer.DeviceVolume; set => _realPlayer.DeviceVolume = value; }
        public long Duration => _realPlayer.Duration;
        //[System.Obsolete] public bool HasNext => _realPlayer.HasNext;
        //[System.Obsolete] public bool HasNextWindow => _realPlayer.HasNextWindow;
        public bool IsCurrentMediaItemDynamic => _realPlayer.IsCurrentMediaItemDynamic;
        public bool IsCurrentMediaItemLive => _realPlayer.IsCurrentMediaItemLive;
        public bool IsCurrentMediaItemSeekable => _realPlayer.IsCurrentMediaItemSeekable;
        [System.Obsolete] public bool IsCurrentWindowDynamic => _realPlayer.IsCurrentWindowDynamic;
        [System.Obsolete] public bool IsCurrentWindowLive => _realPlayer.IsCurrentWindowLive;
        [System.Obsolete] public bool IsCurrentWindowSeekable => _realPlayer.IsCurrentWindowSeekable;
        public bool IsLoading => _realPlayer.IsLoading;
        public bool IsPlaying => _realPlayer.IsPlaying;
        public bool IsPlayingAd => _realPlayer.IsPlayingAd;
        public long MaxSeekToPreviousPosition => _realPlayer.MaxSeekToPreviousPosition;
        public int MediaItemCount => _realPlayer.MediaItemCount;
        public MediaMetadata? MediaMetadata => _realPlayer.MediaMetadata;
        public int NextMediaItemIndex => _realPlayer.NextMediaItemIndex;
        [System.Obsolete] public int NextWindowIndex => _realPlayer.NextWindowIndex;
        public bool PlayWhenReady { get => _realPlayer.PlayWhenReady; set => _realPlayer.PlayWhenReady = value; }
        public PlaybackParameters? PlaybackParameters { get => _realPlayer.PlaybackParameters; set => _realPlayer.PlaybackParameters = value; }
        public int PlaybackState => _realPlayer.PlaybackState;
        public int PlaybackSuppressionReason => _realPlayer.PlaybackSuppressionReason;
        public PlaybackException? PlayerError => _realPlayer.PlayerError;
        public MediaMetadata? PlaylistMetadata { get => _realPlayer.PlaylistMetadata; set => _realPlayer.PlaylistMetadata = value; }
        public int PreviousMediaItemIndex => _realPlayer.PreviousMediaItemIndex;
        [System.Obsolete] public int PreviousWindowIndex => _realPlayer.PreviousWindowIndex;
        public int RepeatMode { get => _realPlayer.RepeatMode; set => _realPlayer.RepeatMode = value; }
        public long SeekBackIncrement => _realPlayer.SeekBackIncrement;
        public long SeekForwardIncrement => _realPlayer.SeekForwardIncrement;
        public bool ShuffleModeEnabled { get => _realPlayer.ShuffleModeEnabled; set => _realPlayer.ShuffleModeEnabled = value; }
        public AndroidX.Media3.Common.Util.Size? SurfaceSize => _realPlayer.SurfaceSize;
        public long TotalBufferedDuration => _realPlayer.TotalBufferedDuration;
        public TrackSelectionParameters? TrackSelectionParameters { get => _realPlayer.TrackSelectionParameters; set => _realPlayer.TrackSelectionParameters = value; }
        public VideoSize? VideoSize => _realPlayer.VideoSize;
        public float Volume { get => _realPlayer.Volume; set => _realPlayer.Volume = value; }
        public bool DeviceMuted
        {
            get => _realPlayer.DeviceMuted;
            set => _realPlayer.DeviceMuted = value;
        }

        bool IPlayer.HasNextMediaItem => true;

        bool IPlayer.HasPreviousMediaItem => true;

        public bool HasNextMediaItem() => true;


        public bool HasPreviousMediaItem() => true;


        public bool IsCommandAvailable(int command)
        {
            return _realPlayer.IsCommandAvailable(command);
        }

        public void AddListener(IPlayerListener? listener) => _realPlayer.AddListener(listener);
        public void AddMediaItem(MediaItem? item) => _realPlayer.AddMediaItem(item);
        public void AddMediaItem(int index, MediaItem? item) => _realPlayer.AddMediaItem(index, item);
        public void AddMediaItems(int index, IList<MediaItem>? mediaItems) => _realPlayer.AddMediaItems(index, mediaItems);
        public void AddMediaItems(IList<MediaItem>? mediaItems) => _realPlayer.AddMediaItems(mediaItems);
        public bool CanAdvertiseSession() => _realPlayer.CanAdvertiseSession();
        public void ClearMediaItems() => _realPlayer.ClearMediaItems();
        public void ClearVideoSurface() => _realPlayer.ClearVideoSurface();
        public void ClearVideoSurface(Surface? surface) => _realPlayer.ClearVideoSurface(surface);
        public void ClearVideoSurfaceHolder(ISurfaceHolder? surfaceHolder) => _realPlayer.ClearVideoSurfaceHolder(surfaceHolder);
        public void ClearVideoSurfaceView(SurfaceView? surfaceView) => _realPlayer.ClearVideoSurfaceView(surfaceView);
        public void ClearVideoTextureView(TextureView? textureView) => _realPlayer.ClearVideoTextureView(textureView);
        [System.Obsolete] public void DecreaseDeviceVolume() => _realPlayer.DecreaseDeviceVolume();
        public void DecreaseDeviceVolume(int flags) => _realPlayer.DecreaseDeviceVolume(flags);
        public MediaItem? GetMediaItemAt(int index) => _realPlayer.GetMediaItemAt(index);
        [System.Obsolete] public void IncreaseDeviceVolume() => _realPlayer.IncreaseDeviceVolume();
        public void IncreaseDeviceVolume(int flags) => _realPlayer.IncreaseDeviceVolume(flags);


        public void MoveMediaItem(int currentIndex, int newIndex) => _realPlayer.MoveMediaItem(currentIndex, newIndex);
        public void MoveMediaItems(int fromIndex, int toIndex, int newIndex) => _realPlayer.MoveMediaItems(fromIndex, toIndex, newIndex);
        //[System.Obsolete] public void Next() => _realPlayer.Next();
        public void Pause() => _realPlayer.Pause();
        public void Play() => _realPlayer.Play();
        public void Prepare() => _realPlayer.Prepare();
        public void Release() => _realPlayer.Release();
        public void RemoveListener(IPlayerListener? listener) => _realPlayer.RemoveListener(listener);
        public void RemoveMediaItem(int index) => _realPlayer.RemoveMediaItem(index);
        public void RemoveMediaItems(int fromIndex, int toIndex) => _realPlayer.RemoveMediaItems(fromIndex, toIndex);
        public void ReplaceMediaItem(int index, MediaItem? mediaItem) => _realPlayer.ReplaceMediaItem(index, mediaItem);
        public void ReplaceMediaItems(int fromIndex, int toIndex, IList<MediaItem>? mediaItems) => _realPlayer.ReplaceMediaItems(fromIndex, toIndex, mediaItems);
        public void SeekBack() => _realPlayer.SeekBack();
        public void SeekForward() => _realPlayer.SeekForward();
        public void SeekTo(int mediaItemIndex, long positionMs) => _realPlayer.SeekTo(mediaItemIndex, positionMs);
        public void SeekTo(long positionMs) => _realPlayer.SeekTo(positionMs);
        public void SeekToDefaultPosition() => _realPlayer.SeekToDefaultPosition();
        public void SeekToDefaultPosition(int mediaItemIndex) => _realPlayer.SeekToDefaultPosition(mediaItemIndex);
        public void SeekToNext() => _realPlayer.SeekToNext();
        public void SeekToNextMediaItem() => _realPlayer.SeekToNextMediaItem();
        //[System.Obsolete] public void SeekToNextWindow() => _realPlayer.SeekToNextWindow();
        public void SeekToPrevious() => _realPlayer.SeekToPrevious();
        public void SeekToPreviousMediaItem() => _realPlayer.SeekToPreviousMediaItem();
        //[System.Obsolete] public void SeekToPreviousWindow() => _realPlayer.SeekToPreviousWindow();
        public void SetAudioAttributes(AudioAttributes? attrs, bool handleAudioFocus) => _realPlayer.SetAudioAttributes(attrs, handleAudioFocus);
        //[System.Obsolete] public void SetDeviceMuted(bool muted) => _realPlayer.SetDeviceMuted(muted);
        public void SetDeviceMuted(bool muted, int flags) => _realPlayer.SetDeviceMuted(muted, flags);
        //[System.Obsolete] public void SetDeviceVolume(int volume) => _realPlayer.SetDeviceVolume(volume);
        public void SetDeviceVolume(int volume, int flags) => _realPlayer.SetDeviceVolume(volume, flags);
        public void SetMediaItem(MediaItem? item) => _realPlayer.SetMediaItem(item);
        public void SetMediaItem(MediaItem? item, bool resetPosition) => _realPlayer.SetMediaItem(item, resetPosition);
        public void SetMediaItem(MediaItem? item, long startPositionMs) => _realPlayer.SetMediaItem(item, startPositionMs);
        public void SetMediaItems(IList<MediaItem>? mediaItems) => _realPlayer.SetMediaItems(mediaItems);
        public void SetMediaItems(IList<MediaItem>? mediaItems, bool resetPosition) => _realPlayer.SetMediaItems(mediaItems, resetPosition);
        public void SetMediaItems(IList<MediaItem>? mediaItems, int startIndex, long startPositionMs) => _realPlayer.SetMediaItems(mediaItems, startIndex, startPositionMs);
        public void SetPlaybackSpeed(float speed) => _realPlayer.SetPlaybackSpeed(speed);
        public void SetVideoSurface(Surface? surface) => _realPlayer.SetVideoSurface(surface);
        public void SetVideoSurfaceHolder(ISurfaceHolder? surfaceHolder) => _realPlayer.SetVideoSurfaceHolder(surfaceHolder);
        public void SetVideoSurfaceView(SurfaceView? surfaceView) => _realPlayer.SetVideoSurfaceView(surfaceView);
        public void SetVideoTextureView(TextureView? textureView) => _realPlayer.SetVideoTextureView(textureView);
        public void Stop() => _realPlayer.Stop();

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

