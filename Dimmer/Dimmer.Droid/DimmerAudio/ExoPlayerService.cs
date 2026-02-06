#region Using Directives
// Android Core
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

using Java.Util.Concurrent;

using Android.Media;

using MediaController = AndroidX.Media3.Session.MediaController;

using AndroidX.Lifecycle;
using Exception = System.Exception;

namespace Dimmer.DimmerAudio; // Make sure this namespace is correct

[Service(Name = "com.yvanbrunel.dimmer.ExoPlayerService", // Ensure this matches AndroidManifest.xml if needed
         Enabled = true, Exported = true,
         ForegroundServiceType = ForegroundService.TypeMediaPlayback)]


public partial class ExoPlayerService : MediaSessionService
{

    // --- Components ---
    private MediaSession? mediaSession;
    private IExoPlayer? player;
    private MediaPlaybackSessionCallback? sessionCallback; // Use concrete type
    public static SessionCommand FavoriteSessionCommand = new SessionCommand(ActionFavorite, Bundle.Empty);
    // --- Constants ---
    public const string ActionLyrics = "ACTION_LYRICS";
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


    public const string ActionFavorite = "ACTION_FAVORITE";
    public const string ActionShuffle = "ACTION_SHUFFLE";
    public const string ActionRepeat = "ACTION_REPEAT";
    //private const string MetadataKeyDurationString = MetadataCompat.MetadataKeyDuration; // Use constant from support lib if available

    // --- Internal State ---
    internal static MediaItem? currentMediaItem; // Choose a unique ID
    public static SongModelView? CurrentSongContext; // Choose a unique ID
    public static SongModelView? CurrentSongExposed => CurrentSongContext;
    
    // Static methods to expose state for notification
    internal static bool ShuffleStateInternal = false;
    internal static int RepeatModeInternal = 1; // 0=All, 1=Off, 2=One
    
    public static bool GetShuffleState() => ShuffleStateInternal;
    public static int GetRepeatMode() => RepeatModeInternal;
    // ---  Service Lifecycle ---
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

    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<(double newVol,bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;
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

    internal void RaiseVolumeChanged(float newVolume) =>
        VolumeChanged?.Invoke(this, newVolume);

    internal void RaiseOnDeviceVolumeChanged(float newVolume, bool IsMuted)
    {
        var devMavVol = GetMaxVolumeLevel();
        DeviceVolumeChanged?.Invoke(this, (newVolume, IsMuted,devMavVol));
    }

    PlayerNotificationManager? _notifMgr;
    private Runnable? _positionRunnable;
    private MediaController? mediaController;

    public ExoPlayerServiceBinder? Binder { get => _binder; set => _binder = value; }

    public void RefreshNotification()
    {
        _notifMgr?.Invalidate();
    }
    public void PrepareNext(SongModelView nextSong)
    {
        if (player == null) return;

        // 1. Convert SongModelView to MediaItem
        var mediaItem = new MediaItem.Builder()?
            .SetUri(nextSong.FilePath)?
            .SetMediaId(nextSong.Id.ToString()) ?
            .SetMediaMetadata(new MediaMetadata.Builder()?
                .SetTitle(nextSong.Title)?
                .SetArtist(nextSong.ArtistName)?
                .SetAlbumTitle(nextSong.AlbumName)?
                .Build())?
            .Build();


        //int nextIndex = player.CurrentMediaItemIndex + 1;
        // player.AddMediaItem(nextIndex, mediaItem);

        Console.WriteLine($"[ExoPlayerService] Queued next song: {nextSong.Title}");
    }


    internal static int GetMaxVolumeLevel()
    {
        // 1) grab the Android AudioManager
        var audioManager = Platform.AppContext
            .GetSystemService(AudioService) as AudioManager;
        if (audioManager == null) return 0;
        return audioManager.GetStreamMaxVolume(Android.Media.Stream.Music);
        
    }
    public static AudioOutputDevice GetCurrentAudioOutputDevice()
    {
        var audioManager = Platform.AppContext.GetSystemService(AudioService) as AudioManager;
        var devices = audioManager?
            .GetDevices(GetDevicesTargets.Outputs)
            ?? [];
        var currentDevice = devices.FirstOrDefault(d => d.IsSource);
        if (currentDevice is not null)
        {
            return new AudioOutputDevice
            {
                Id = currentDevice.Id.ToString(),
                ProductName = currentDevice.ProductNameFormatted?.ToString() ?? currentDevice.Type.ToString(),
                Name = currentDevice.ProductName,
                Type = currentDevice.Type.ToString(),
                IsSource = currentDevice.IsSource
                
            };
        }
        return new AudioOutputDevice
        {
            Id = "Unknown",
            Name = "Unknown",
            Type = "Unknown",
            IsSource = false
        };
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

    public static List<AudioDeviceInfo> GetAvailableAudioOutputs()
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
            if (player is null) return false;
            player.SetPreferredAudioDevice(specDev);
            return true;
        }
        return false;
    }
    private bool _isPolling = false;



    //private IPlayer? notificationPlayer;
    public async override void OnCreate()
    {
        base.OnCreate();




        try
        {

            var audioAttributes = new AudioAttributes.Builder()!
            .SetUsage(C.UsageMedia)! // Specify this is media playback
            .SetContentType(C.AudioContentTypeMusic)! // Specify the content is music
            .SetIsContentSpatialized(true).Build();

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


            player?.AddListener(new PlayerEventListener(this));

            sessionCallback = new MediaPlaybackSessionCallback(this); // Use concrete type


            Intent nIntent = new Intent(Platform.AppContext, typeof(TransitionActivity));

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S) // Or BuildVersionCodes.M for broader compatibility with Immutable
            {
                flags |= PendingIntentFlags.Immutable;
            }
            PendingIntent? pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, flags);
            FavoriteSessionCommand.CommandCode = 88;

            var heartButton = new CommandButton.Builder(CommandButton.IconUndefined)
                .SetDisplayName("Favorite")
                .SetSessionCommand(FavoriteSessionCommand)
                //.SetPlayerCommand(88)
                //.SetSlots()
                
                .SetCustomIconResId(Resource.Drawable.heart)
                .SetEnabled(true)
                
                .Build();
                
            mediaSession = new MediaSession.Builder(this, player)!
                .SetSessionActivity(pendingIntent)!
                .SetCallback(sessionCallback)!
                .SetId("Dimmer_MediaSession_Main")!
                //.SetCommandButtonsForMediaItems(commandButtons: new List<CommandButton> { heartButton })
                //.SetMediaButtonPreferences(mediaButtonPreferences:new List<CommandButton> { heartButton })
    .SetCustomLayout(customLayout: new List<CommandButton> { heartButton})
                .Build();
            
            _binder = new ExoPlayerServiceBinder(this);

            // 2) NotificationManager
            NotificationHelper.CreateChannel(this);
            _notifMgr = NotificationHelper.BuildManager(this, mediaSession!, CurrentSongContext);

            _notifMgr.SetPlayer(player);



            await InitializeMediaControllerAsync(); // Fire and forget, handle result in the async method

            StartPositionPolling();

        }
        catch (Java.Lang.Throwable ex) { HandleInitError("JAVA INITIALIZATION", ex); StopSelf(); }

    }
    private async void StartPositionPolling()
    {
        if (_isPolling) return;
        _isPolling = true;

        while (player != null && player.IsPlaying && _isPolling)
        {
            RaisePositionChanged(player.CurrentPosition);
            await Task.Delay(1000);
        }
        _isPolling = false;
    }

    private Handler? positionHandler;
    private Runnable? positionRunnable;
    private async Task InitializeMediaControllerAsync()
    {
        Console.WriteLine("DIMMERTRACE: ExoPlayerService.InitializeMediaControllerAsync START");
        try
        {
            if (mediaSession?.Token == null)
            {
                Console.WriteLine("DIMMERTRACE: ExoPlayerService.InitializeMediaControllerAsync - MediaSession token is null, cannot build controller.");
                return;
            }

            var controllerFuture = new MediaController.Builder(this, mediaSession.Token).BuildAsync();
            if(controllerFuture is not null)
            {

                var controllerObject = await controllerFuture.GetAsync(); // Await here on a background context
                mediaController = (MediaController?)controllerObject;
                Console.WriteLine("DIMMERTRACE: ExoPlayerService.InitializeMediaControllerAsync END - Controller built");

            }
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


        //var notification = NotificationHelper.BuildMinimalNotification(this);

        //StartForeground(NotificationHelper.NotificationId, notification);

        string? action = intent?.Action;
        if (action is not null)
        {
            switch (action)
            {
                case ActionFavorite:
                    HandleFavoriteAction();
                    break;
                case ActionShuffle:
                    HandleShuffleAction();
                    break;
                case ActionRepeat:
                    HandleRepeatAction();
                    break;
                case ActionLyrics:
                    HandleLyricsAction();
                    break;
            }
        }

        return StartCommandResult.Sticky;
    }
    
    private void HandleFavoriteAction()
    {
        if (CurrentSongContext != null)
        {
            // Get the ViewModel and toggle favorite state
            var viewModel = MainApplication.ServiceProvider.GetService<BaseViewModel>();
            if (viewModel != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (CurrentSongContext.IsFavorite)
                        {
                            await viewModel.RemoveSongFromFavorite(CurrentSongContext);
                        }
                        else
                        {
                            await viewModel.AddFavoriteRatingToSong(CurrentSongContext);
                        }
                        
                        RxSchedulers.UI.ScheduleTo(() => RefreshNotification());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ExoPlayerService] Error toggling favorite: {ex.Message}");
                    }
                });
            }
        }
    }
    
    private void HandleShuffleAction()
    {
        var viewModel = MainApplication.ServiceProvider.GetService<BaseViewModel>();
        if (viewModel != null)
        {
            RxSchedulers.UI.ScheduleTo(() =>
            {
                try
                {
                    viewModel.ToggleShuffle();
                    ShuffleStateInternal = viewModel.IsShuffleActive;
                    RefreshNotification();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExoPlayerService] Error toggling shuffle: {ex.Message}");
                }
            });
        }
    }
    
    private void HandleRepeatAction()
    {
        var viewModel = MainApplication.ServiceProvider.GetService<BaseViewModel>();
        if (viewModel != null)
        {
            RxSchedulers.UI.ScheduleTo(() =>
            {
                try
                {
                    viewModel.ToggleRepeatMode();
                    RepeatModeInternal = (int)viewModel.CurrentRepeatMode;
                    RefreshNotification();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ExoPlayerService] Error toggling repeat: {ex.Message}");
                }
            });
        }
    }
    
    private void HandleLyricsAction()
    {
        // Open the app to the lyrics view
        var intent = new Intent(this, typeof(TransitionActivity));
        intent.SetAction(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);
        intent.AddFlags(ActivityFlags.NewTask);
        intent.PutExtra("ShowLyrics", true);
        StartActivity(intent);
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
        // 1. Stop polling first
        positionHandler?.RemoveCallbacksAndMessages(null);
        positionHandler = null;

        // 2. Release Notification Manager (this detaches player safely)
        if (_notifMgr != null)
        {
            _notifMgr.SetPlayer(null);
            _notifMgr.Dispose();
            _notifMgr = null;
        }

        // 3. Release Session
        if (mediaSession != null)
        {
            mediaSession.Release();
            mediaSession = null;
        }

        // 4. Release Player LAST
        if (player != null)
        {
            player.Stop();
            player.Release();
            player = null;
        }

        base.OnDestroy();
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

        if (player == null) return Task.CompletedTask;

        CurrentSongContext = song;

        if (player.CurrentMediaItem?.MediaId == song.Id.ToString())
        {
            //player.PlaybackState == IPlayer.StateEnded ||
            Console.WriteLine(player.PlaybackState);

            if ( !player.IsPlaying)
            {
                player.SeekTo(startPositionMs);
                player.Play();
            }
            return Task.CompletedTask;
        }

        try
        {

            // Sync shuffle and repeat state from ViewModel
            var viewModel = MainApplication.ServiceProvider?.GetService<BaseViewModel>();
            if (viewModel != null)
            {
                ShuffleStateInternal = viewModel.IsShuffleActive;
                RepeatModeInternal = (int)viewModel.CurrentRepeatMode;
            }
            
        if (player is null)
        {

            throw new ArgumentException("Player not initialized.");

        }
        var genre = song.Genre?.Name;
        //player.Stop();
        player.ClearMediaItems();

        //player.PlaybackLooper

        MediaMetadata.Builder? metadataBuilder = new MediaMetadata.Builder()!
            .SetTitle(title)!
            .SetArtist(artist)!
            .SetAlbumTitle(album)!
            .SetMediaType(new Java.Lang.Integer(MediaMetadata.MediaTypeMusic))! // Use Java Integer wrapper
            .SetGenre(genre)!
            .SetArtworkUri(string.IsNullOrEmpty(imagePath) ? null : Uri.FromFile(new Java.IO.File(imagePath)))

            .SetIsPlayable(Java.Lang.Boolean.True)!; // Use Java Boolean wrapper




            currentMediaItem = new MediaItem.Builder()!
               .SetMediaId(song.Id.ToString())! 
               .SetUri(Uri.Parse(url))!
               .SetMediaMetadata(metadataBuilder!.Build())!
               .Build();

            player.SetMediaItem(currentMediaItem, startPositionMs);

            player.SetMediaItem(currentMediaItem, 0); // Set item and start position
            player.AddMediaItem(currentMediaItem);
            
            player.Prepare();

            player.Play();

            Console.WriteLine(player.AvailableCommands?.GetType());
                return Task.CompletedTask;
            
           

        }
        catch (Java.Lang.Throwable jex) { HandleInitError("PreparePlay SetMediaItem/Prepare", jex); }


        return Task.CompletedTask;
    }

    internal static void UpdateFavoriteState(SongModelView song)
    {
        Toast.MakeText(Platform.AppContext, "Opening synced lyrics...", ToastLength.Short)?.Show();

    }

    public void UpdateMediaSessionLayout()
    {
        bool isFav = CurrentSongContext?.IsFavorite ?? false;

        var heartBtn = new CommandButton.Builder(CommandButton.IconUndefined)
            .SetDisplayName("Favorite")
            .SetSessionCommand(new SessionCommand(ActionFavorite, Bundle.Empty))
            .SetCustomIconResId(isFav ? Resource.Drawable.media3_icon_heart_filled : Resource.Drawable.heart)
            .Build();

        bool isShuffleOn = ShuffleStateInternal;
        var shuffleBtn = new CommandButton.Builder(CommandButton.IconUndefined)
            .SetDisplayName("Shuffle")
            .SetSessionCommand(new SessionCommand(ActionShuffle, Bundle.Empty))
            .SetCustomIconResId(isShuffleOn ? Resource.Drawable.media3_icon_shuffle_on : Resource.Drawable.media3_icon_shuffle_off)
            .Build();

        int repeatMode = RepeatModeInternal;
        int repeatIcon = repeatMode switch
        {
            2 => Resource.Drawable.media3_icon_repeat_one,  // Repeat One
            0 => Resource.Drawable.media3_icon_repeat_all,  // Repeat All
            _ => Resource.Drawable.media3_icon_repeat_off   // Repeat Off
        };
        var repeatBtn = new CommandButton.Builder(CommandButton.IconUndefined)
            .SetDisplayName("Repeat")
            .SetSessionCommand(new SessionCommand(ActionRepeat, Bundle.Empty))
            .SetCustomIconResId(repeatIcon)
            .Build();

        var layout = new List<CommandButton> { heartBtn, shuffleBtn, repeatBtn };
        mediaSession?.SetCustomLayout((System.Collections.Generic.IList<CommandButton>)layout);

        _notifMgr?.Invalidate();
    }
    // --- Player Event Listener ---
    sealed class PlayerEventListener : Object, IPlayerListener // Use specific IPlayer.Listener
    {

        private readonly ExoPlayerService service;
        public PlayerEventListener(ExoPlayerService service) { this.service = service; }

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


            service.UpdateMediaSessionLayout();

            ////// Update service context (look up full SongModelView by ID)
            ////var newSongContext = SongRepository.GetById(newId);
            ////service.CurrentSongContext = newSongContext;

            Console.WriteLine($"[ExoPlayerService] MediaItemTransition: Reason={reason}");
        }

        public void OnEvents(global::AndroidX.Media3.Common.IPlayer? player, global::AndroidX.Media3.Common.PlayerEvents? events)
        {

        }
        public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
        {
            Console.WriteLine("Current player playback state int "+playbackState);
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
            service.RaiseIsPlayingChanged(isPlaying);
            if (isPlaying)
            {

                service.StartPositionPolling();
            }
            else
            {
                service._isPolling = false;
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
            
            service.RaiseOnDeviceVolumeChanged(volume,muted);
            /* Log if needed */
        }

        public void OnIsLoadingChanged(bool isLoading)
        {

        }

        public void OnMaxSeekToPreviousPositionChanged(long p0) { /* Log if needed */ }
        // public void OnMetadata(MediaMetadata? volume) {} // Superseded by OnMediaMetadataChanged? Check docs.
       public void OnMediaMetadataChanged(global::AndroidX.Media3.Common.MediaMetadata? mediaMetadata)
        {

        }
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
            service.RaiseVolumeChanged(volume);
        }

        
        public void OnPlayerError(PlaybackException? error)
        {
            // It's crucial to have this method to handle errors.
            // At a minimum, you should log it.
            Console.WriteLine($"[ExoPlayerService] PLAYER ERROR: {error?.Message}");
            
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

        // --- Connection Handling ---
        public MediaSession.ConnectionResult OnConnect(
  MediaSession? session,
  MediaSession.ControllerInfo? controller)
        {


            //var sessionCommands = new SessionCommands.Builder()
            //       .Add(SessionCommand.CommandCodeSessionSetRating)!
            //       .Add(new SessionCommand(ExoPlayerService.ActionFavorite, Bundle.Empty))
            //       .Add(new SessionCommand(ExoPlayerService.ActionShuffle, Bundle.Empty))!
            //       .Add(new SessionCommand(ExoPlayerService.ActionRepeat, Bundle.Empty))!
            //      .Build();


            //var playerCommands = new PlayerCommands.Builder()
            //    .Add(SessionCommand.CommandCodeSessionSetRating)

            //  .AddAllCommands()!
            //  .Build();
            var sessionCommands = MediaSession.ConnectionResult.DefaultSessionCommands.BuildUpon()
           .Add(ExoPlayerService.FavoriteSessionCommand)
           .Add(new SessionCommand(ExoPlayerService.ActionShuffle, Bundle.Empty))
           .Add(new SessionCommand(ExoPlayerService.ActionRepeat, Bundle.Empty))
           .Build();


            MediaSession.ConnectionResult? e = new MediaSession.ConnectionResult.AcceptedResultBuilder(session!)
                .SetAvailableSessionCommands(sessionCommands)
                .Build();

            return e;
        }
        public void OnPostConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        {


        }

        public void OnDisconnected(MediaSession? session, MediaSession.ControllerInfo? controller)
        {



        }

        // --- Command Handling ---


        public bool OnMediaButtonEvent(global::AndroidX.Media3.Session.MediaSession? session, global::AndroidX.Media3.Session.MediaSession.ControllerInfo? controllerInfo, global::Android.Content.Intent? intent)
        {

            if (intent?.Action != Intent.ActionMediaButton)
                return false;

            var keyEvent = (KeyEvent?)intent.GetParcelableExtra(Intent.ExtraKeyEvent, Java.Lang.Class.FromType(typeof(KeyEvent)));

            if (keyEvent == null || keyEvent.Action != KeyEventActions.Down)
                return false;

            switch (keyEvent.KeyCode)
            {
                case Keycode.MediaNext:
                    service.RaisePlayNextPressed();
                    return true; // We handled it

                case Keycode.MediaPrevious:
                    service.RaisePlayPreviousPressed();
                    return true;

                case Keycode.MediaPlay:
                case Keycode.MediaPause:
                case Keycode.MediaPlayPause:
                    // Toggle Play/Pause logic
                    if (service.player.IsPlaying) service.player.Pause();
                    else service.player.Play();
                    return true;
                case Keycode.Headsethook:
                    //HandleHeadsetHookMultiClick(); // Custom logic for 1, 2, or 3 clicks
                    return true;

                case Keycode.ThumbsUp:
                    //service.MarkCurrentSongAsFavorite(true);
                    return true;

                case Keycode.ThumbsDown:
                    //service.SkipAndDislike();
                    return true;

                case Keycode.MediaSkipForward:
                    //service.SeekRelative(30000); // Forward 30s
                    //service.SeekRelative(30000); // Forward 30s
                    return true;

                case Keycode.Info:
                    //service.ToggleLyricsOverlay();
                    return true;

                case Keycode.Music:
                    //service.BringAppToForegroundAndPlay();
                    return true;

                case Keycode.MediaAudioTrack:
                    //service.CycleAudioEqualizerPresets(); // A niche use for this key
                    return true;

            }

            return false; // Let default logic handle other buttons (like volume)
        }


        public SessionResult OnCustomCommand(MediaSession? session, MediaSession.ControllerInfo? controller, SessionCommand? command, Bundle? args)
        {
            if (command == null)
            {
                return (SessionResult)SessionResult.ResultErrorBadValue;
            }
            if (command.CustomAction is not null && command.CustomAction.Equals(ExoPlayerService.FavoriteSessionCommand.CustomAction))
            {

                return (SessionResult)SessionResult.ResultSuccess;
            }
            switch (command.CustomAction)
            {
                
                case ExoPlayerService.ActionFavorite:
                    // Toggle favorite state
                    var currentSong = ExoPlayerService.CurrentSongContext;
                    if (currentSong != null)
                    {
                        var viewModel = MainApplication.ServiceProvider.GetService<BaseViewModel>();
                        if (viewModel != null)
                        {
                            Task.Run(async () =>
                            {
                                try
                                {
                                    if (currentSong.IsFavorite)
                                    {
                                        await viewModel.RemoveSongFromFavorite(currentSong);
                                    }
                                    else
                                    {
                                        await viewModel.AddFavoriteRatingToSong(currentSong);
                                    }
                                    
                                    RxSchedulers.UI.ScheduleTo(() => service.UpdateMediaSessionLayout());
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ExoPlayerService] Error toggling favorite: {ex.Message}");
                                }
                            });
                        }
                    }
                    return (SessionResult)SessionResult.ResultSuccess;
                    
                case ExoPlayerService.ActionShuffle:
                    // Toggle shuffle mode
                    var vm = MainApplication.ServiceProvider.GetService<BaseViewModel>();
                    if (vm != null)
                    {
                        RxSchedulers.UI.ScheduleTo(() =>
                        {
                            try
                            {
                                vm.ToggleShuffle();
                                ExoPlayerService.ShuffleStateInternal = vm.IsShuffleActive;
                                service.UpdateMediaSessionLayout();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ExoPlayerService] Error toggling shuffle: {ex.Message}");
                            }
                        });
                    }
                    return (SessionResult)SessionResult.ResultSuccess;
                    
                case ExoPlayerService.ActionRepeat:
                    // Toggle repeat mode
                    var vmRepeat = MainApplication.ServiceProvider.GetService<BaseViewModel>();
                    if (vmRepeat != null)
                    {
                        RxSchedulers.UI.ScheduleTo(() =>
                        {
                            try
                            {
                                vmRepeat.ToggleRepeatMode();
                                ExoPlayerService.RepeatModeInternal = (int)vmRepeat.CurrentRepeatMode;
                                service.UpdateMediaSessionLayout();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ExoPlayerService] Error toggling repeat: {ex.Message}");
                            }
                        });
                    }
                    return (SessionResult)SessionResult.ResultSuccess;
                    
                default:
                    return (SessionResult)SessionResult.ResultErrorUnknown;
            }
        }
        //IPlayer Player = service.player;
        // Decide whether to allow a specific PLAYER command requested by a controllerInfo
        public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controller, int playerCommand)
        {

            // playerCommand 9 = CommandNextMediaItem
            // playerCommand 7 = CommandPreviousMediaItem

            switch (playerCommand)
            {
                case 9:
                case 8:
                    service.RaisePlayNextPressed();
                    return SessionResult.ResultSuccess;

                case 6:
                case 7:
                    service.RaisePlayPreviousPressed();
                    return SessionResult.ResultSuccess;

                case 4:
                    // Handle Play/Pause
                    return SessionResult.ResultSuccess;
            }

            return SessionResult.ResultSuccess;
        }
        public void OnPlayerInteractionFinished(MediaSession? session, MediaSession.ControllerInfo? controllerInfo, PlayerCommands? playerCommands)
        {


            //#region Old code. still useful in case
            //if (playerCommands == null || playerCommands.Size() == 0)
            //{

            //    return;
            //}

            //for (int i = 0; i < playerCommands.Size(); i++)
            //{
            //    int command = playerCommands.Get(i);

            //    switch (command)
            //    {

            //        case 9:
            //            service.player?.Pause();
            //            service.RaisePlayNextPressed();
            //            break;

            //        case 7:
            //            service.player?.Pause();
            //            service.player?.Pause();
            //            service.RaisePlayPreviousPressed();

            //            break;

            //        default:

            //            break;
            //    }
            //}

            //#endregion
        }




    }


} // End ExoPlayerService class





public partial class ExoPlayerServiceBinder : Binder
{
    public ExoPlayerService Service { get; }


    internal ExoPlayerServiceBinder(ExoPlayerService service)
    {
        Service = service;
    }
}

