using Android.App;
using Android.Content;
using Android.Media;
using Android.Net.Wifi;
using Android.OS;
using Android.Media.Session;
using Android.Graphics;
using Activity = Android.App.Activity;
using Application = Android.App.Application;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;
using MediaSession = AndroidX.Media3.Session.MediaSession;
using AudioOffloadPreferences = AndroidX.Media3.Common.TrackSelectionParameters.AudioOffloadPreferences;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using Action = AndroidX.Core.App.NotificationCompat.Action;
//using NotificationCompat = AndroidX.Media.App.NotificationCompat;

using PlayerNotificationManager = AndroidX.Media3.UI.PlayerNotificationManager;
using BuilderUI = AndroidX.Media3.UI.PlayerNotificationManager.Builder;
using IMediaDescriptionAdapter = AndroidX.Media3.UI.PlayerNotificationManager.IMediaDescriptionAdapter;
using AndroidX.Core.Util; // You might need to add this for MoreExecutors
using Android.Content.PM;
using System.Reflection;
using Android.Net;
using Uri = Android.Net.Uri;
using Exception = System.Exception;
using System.IO;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using Java.Util.Concurrent;
using static AndroidX.Media3.Common.BasePlayer;
using PlaybackStateCompat = Android.Support.V4.Media.Session.PlaybackStateCompat;
using MediaController = AndroidX.Media3.Session.MediaController;
using Android.Runtime;
using Google.Common.Util.Concurrent;
using AndroidX.Media.App;
using AndroidX.Core.App;
using Java.Interop;
using AndroidX.ConstraintLayout.Helper.Widget;
using static AndroidX.Media3.Session.MediaLibraryService;
using Rating = AndroidX.Media3.Common.Rating;
using System.Collections.Immutable;
using static AndroidX.Media3.UI.PlayerNotificationManager;

using AndroidX.Media3.UI;
using System.Runtime.InteropServices;
using Java.Lang;
using static Android.Provider.CalendarContract;
using System.Diagnostics;
using Java.Lang.Annotation;
using static Android.Provider.ContactsContract.CommonDataKinds;
using Android.Widget;
using Android.Provider;
using Android.Util;
using Android.Views;
using Timer = System.Timers.Timer;
using static Microsoft.Maui.ApplicationModel.Platform;
using Intent = Android.Content.Intent;
using static AndroidX.Media3.Session.MediaSession;
using Android.Bluetooth;
using Android.Media.Midi;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

[Service(Enabled = true, Exported = true, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter(new[] {
    ActionPlay,
    ActionPause,
    ActionStop,
    ActionTogglePlayback,
    ActionNext,
    ActionPrevious,
    ActionSeekTo,
    ActionSetRating,
    ActionFavorite
})]
public class MediaPlayerService : MediaSessionService, IPlayerListener, IMediaController,
    AudioManager.IOnAudioFocusChangeListener, IMediaDescriptionAdapter, INotificationListener,
    ICustomActionReceiver
{
    //Actions
    public const string ActionPlay = "com.xamarin.action.PLAY";
    public const string ActionPause = "com.xamarin.action.PAUSE";
    public const string ActionStop = "com.xamarin.action.STOP";
    public const string ActionTogglePlayback = "com.xamarin.action.TOGGLEPLAYBACK";
    public const string ActionNext = "com.xamarin.action.NEXT";
    public const string ActionPrevious = "com.xamarin.action.PREVIOUS";
    public const string ActionSeekTo = "com.xamarin.action.ActionSeekTo";
    public const string ActionSetRating = "com.xamarin.ActionSetRating";

    public const string ActionFavorite = "com.xamarin.action.FAVORITE";

    // For managing general notifications and creating notification channels
    private NotificationManager notificationManager;

    private PlayerNotificationManager _playerNotificationManager;
    public event StatusChangedEventHandler StatusChanged;
    public event CoverReloadedEventHandler CoverReloaded;
    public event PlayingEventHandler Playing;
    public event BufferingEventHandler Buffering;
    public event PlayingChangedEventHandler PlayingChanged;
    public event EventHandler<bool> IsPlayingChanged;
    public event EventHandler<long> IsSeekedFromNotificationBar;
    public event EventHandler TaskPlayEnded;
    public event EventHandler TaskPlayNext;
    public event EventHandler TaskPlayPrevious;

    public IExoPlayer mediaPlayer;
    private MediaSession mediaSession;

    bool IsFavorite { get; set; }

    public override IBinder OnBind(Intent intent)
    {


        return base.OnBind(intent)!;
    }

    public override bool OnUnbind(Intent intent)
    {
        NotificationHelper.StopNotification(Platform.AppContext);
        return base.OnUnbind(intent);
    }

    /// <summary>
    /// Properly cleanup of your player by releasing resources
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();
        if (mediaPlayer != null)
        {
            mediaPlayer.Release();

            NotificationHelper.StopNotification(Platform.AppContext);
            StopForeground(StopForegroundFlags.Detach);

            //UnregisterMediaSessionCompat();
        }
    }
    HomePageVM ViewModel;
    public void PrepareAndPlayMediaPlayer(SongModelView Song)
    {
        if(mediaPlayer is not null && mediaPlayer.IsPlaying)
        {
            mediaPlayer.Stop();
            mediaPlayer.ClearMediaItems();            
        }
        if (mediaPlayer == null)
        {
            InitializeMediaPlayer();
        }
        if (notificationManager is null)
        {
            CreateNotificationChannel();
        }
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
       

        if (OperatingSystem.IsAndroidVersionAtLeast(21))
        {
            //TODO FINISH IMPLE, MAKE SO NOTIF TAPS ARE TOLD TO VM TOO AND MAKE SURE WE PASS RIGHT ORDER OF SONGS
            //WE ALSO NEED TO ENSRUE LIST PASSED IS RGHT ORDER
            mediaPlayer.ClearMediaItems();
            var song = ViewModel.TemporarilyPickedSong;
            

            var allSongs = ViewModel.CurrPL.Take(30).ToList();            
            allSongs.Remove(song);
            
            var metadataBuilder1 = new MediaMetadata.Builder()
            .SetTitle(Song.Title)
            //.SetDurationMs(new Long(curSong.DurationInSeconds/1000))!
            .SetTotalTrackCount(new Integer(3))
            .SetAlbumTitle(Song.AlbumName)
                .SetArtist(Song.ArtistName)
            .SetArtworkUri(Uri.FromFile(new Java.IO.File(Song.CoverImagePath)))
            .SetStation(Song.LocalDeviceId);
            var SongMD = new MediaItem.Builder()

                .SetUri(Uri.FromFile(new Java.IO.File(Song.FilePath)))
                .SetMediaMetadata(metadataBuilder1.Build())
                .Build();
            mediaPlayer.AddMediaItem(SongMD);

            foreach (var item in allSongs)
            {

                var metadataBuilder = new MediaMetadata.Builder()
                .SetTitle(item.Title)
                //.SetDurationMs(new Long(curitem.DurationInSeconds/1000))!
                .SetTotalTrackCount(new Integer(3))
                .SetAlbumTitle(item.AlbumName)
                    .SetArtist(item.ArtistName)
                .SetArtworkUri(Uri.FromFile(new Java.IO.File(item.CoverImagePath)))
                .SetStation(item.LocalDeviceId);
                var itemMD = new MediaItem.Builder()

                    .SetUri(Uri.FromFile(new Java.IO.File(item.FilePath)))
                    .SetMediaMetadata(metadataBuilder.Build())
                    .Build();
                mediaPlayer.AddMediaItem(itemMD);   
            }



            //mediaPlayer.ClearMediaItems(); can use this
            
            
            //mediaPlayer.SetShuffleOrder(global::AndroidX.)

        }


        //mediaPlayer.current
        mediaPlayer.Volume = 1.0f;
        mediaPlayer.Prepare();
        mediaPlayer.PlayWhenReady = true;
        mediaPlayer.Play();

        // https://developer.android.com/reference/androidx/media3/ui/PlayerView
        //PlayerView playerView = new PlayerView(Platform.AppContext);
        //playerView.Player = mediaPlayer;



        //https://developer.android.com/reference/androidx/media3/ui/PlayerControlView
        //PlayerControlView playerControlView = new PlayerControlView(Platform.AppContext);


        //https://developer.android.com/reference/androidx/media3/ui/PlayerNotificationManager

        // and https://developer.android.com/reference/androidx/media3/ui/PlayerNotificationManager.BitmapCallback

        Console.WriteLine("is okkk "+mediaPlayer.IsPlaying);
        //i guess i'll only need
        //https://developer.android.com/reference/android/widget/RemoteViews
    }
    //repeate modes
    //0 = repeat off
    //1 = repeat one
    //2 = repeat all

    public const string CHANNEL_ID = "DimmerMusic";
    public const int NOTIFICATION_ID = 0108; // Changed to avoid potential conflicts

    public void PlaySong()
    {
        mediaPlayer.Play();
    }
    public void SkipToNextMediaItem()
    {
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
        ViewModel.PlayNextSong();
        //mediaPlayer.SeekToNextMediaItem();
    }
    public void SkipToPreviousMediaItem()
    {
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
        ViewModel.PlayPreviousSong();
        //mediaPlayer.SeekToPreviousMediaItem();
    }
    // interesting : https://developer.android.com/media/media3/exoplayer/customization
    public void OnTracksChanged(Tracks? trackss)
    {

        // add bp here and it breaks
        Console.WriteLine("OnTracksChanged");
        //Console.WriteLine("Current media "+ mediaPlayer.CurrentMediaItem.MediaMetadata.Title);
        //Console.WriteLine("Current media "+ mediaPlayer.CurrentMediaItem.MediaMetadata.Artist);
        
        //HandlePlayerState(mediaPlayer);
    }

    // interesting : https://developer.android.com/media/media3/exoplayer/customization
    // more : OnRepeatModeChanged(int repeatMode)   
    public void OnTimelineChanged(Timeline? tL, int reason)
    {
    }

    private void InitializeMediaPlayer()
    {
        //this.OnSeekForwardIncrementChanged(2222);
        // Define buffer sizes (in milliseconds)
        long minBufferMs = 15000; // 15 seconds
        long maxBufferMs = 50000; // 50 seconds
        long bufferForPlaybackMs = 2500; // 2.5 seconds
        long bufferForPlaybackAfterRebufferMs = 5000; // 5 seconds

        // read here https://developer.android.com/reference/androidx/media3/common/TrackSelectionParameters.AudioOffloadPreferences

        var AudioOffloadPreferences = new AudioOffloadPreferences.Builder()
            .SetAudioOffloadMode(1)
            .Build();

        // Create a DefaultLoadControl with custom buffer settings
        var loadControl = new DefaultLoadControl.Builder()
            .SetBufferDurationsMs(
                (int)minBufferMs,
                (int)maxBufferMs,
                (int)bufferForPlaybackMs,
                (int)bufferForPlaybackAfterRebufferMs)
            .Build();


        var audioAttributes = new AudioAttributes.Builder()!
            .SetUsage(C.UsageMedia)!
            .SetContentType(2)!
            //.SetFlags()
            .Build();
        if (mediaPlayer is null)
        {
            mediaPlayer = new ExoPlayerBuilder(Platform.AppContext)!
                .SetAudioAttributes(audioAttributes, true)!
                .SetWakeMode(1)
                .SetDeviceVolumeControlEnabled(true)
                
                .SetLoadControl(loadControl)
                
                .Build();
            mediaPlayer.AddListener(this);
        }
        
        

        if (mediaSession is null)
        {

            Intent nIntent = new Intent(Platform.AppContext, typeof(MainActivity));
            var mediaSessionBuilder = new MediaSession.Builder(Platform.AppContext, mediaPlayer);


            var pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, PendingIntentFlags.Mutable);


            var build2 = mediaSessionBuilder
                .SetPeriodicPositionUpdateEnabledBuilder(false) as MediaSession.Builder;
            MyCB = new MediaSessionCB(this);
            build2.SetSessionActivity(pendingIntent);
            
            build2.SetCallback(MyCB);
            
            //.Build();
            
            //.SetAvailablePlayerCommands(null)
            //.SetAvailableSessionCommands(null)
            //.SetPeriodicPositionUpdateEnabled(true) ;

            //.SetCallback(this);

            mediaSession = build2.Build()!;

        }
        SetupNotification();
        //CreateNotificationChannel();
    }

    
    MediaSessionCB MyCB;
    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(CHANNEL_ID, "Dimmer_Music", NotificationImportance.High)
            
            {
                Description = "Music By Dimmer"
            };
            channel.SetShowBadge(true);
            channel.LockscreenVisibility = NotificationVisibility.Public;
            channel.SetAllowBubbles(true);
            channel.Importance = NotificationImportance.Min;
            
            var w = Platform.AppContext.GetSystemService(Context.NotificationService) as NotificationManager;
            notificationManager = w;
            //notificationManager = (NotificationManager)GetSystemService("notification");

            if (notificationManager != null)
            {
                notificationManager.CreateNotificationChannel(channel);
                Log.Debug("CreateNotificationChannel", "Notification channel created successfully.");
            }
            else
            {
                Log.Error("CreateNotificationChannel", "NotificationManager is null.");
            }
         
        }
    }

    //public override IBinder OnBind(Intent intent)
    //{
    //    return null;
    //}
    public override StartCommandResult OnStartCommand (Intent? intent, StartCommandFlags flags, int startId)
    {
        //WORKS!!! 
        if (intent is not null)
        {
            var action = intent.Action;
            if (action is not null)
            {
                switch (action)
                {
                    case ActionPlay:
                        PlaySong();
                        break;
                    case ActionPause:
                        Pause();
                        break;
                    case ActionStop:
                        Stop();
                        break;
                    case ActionTogglePlayback:
                        if (mediaPlayer.IsPlaying)
                        {
                            Pause();
                        }
                        else
                        {
                            PlaySong();
                        }
                        break;
                    case ActionNext:
                        SkipToNextMediaItem();
                        break;
                    case ActionPrevious:
                        SkipToPreviousMediaItem();
                        break;
                    case ActionSeekTo:
                        if (intent.HasExtra("position"))
                        {
                            var position = intent.GetIntExtra("position", 0);
                            Seek(position);
                        }
                        break;
                    case ActionSetRating:
                        if (intent.HasExtra("isFavorite"))
                        {
                            IsFavorite = intent.GetBooleanExtra("isFavorite", false);
                        }
                        break;
                    case ActionFavorite:
                        IsFavorite = !IsFavorite;
                        break;
                    default:
                        break;
                }
            }
        }
        return StartCommandResult.Sticky;
    }

    private void SetupNotification()
    {
        //CreateNotificationChannel();
        if (_playerNotificationManager == null)
        {

            _playerNotificationManager = new BuilderUI(Platform.AppContext, NOTIFICATION_ID, CHANNEL_ID)
            .SetMediaDescriptionAdapter(this)!
            .SetNotificationListener(this)!

            //.SetPlayActionIconResourceId(global::Android.Resource.Drawable.IcMediaPlay)!
            //.SetPauseActionIconResourceId(global::Android.Resource.Drawable.IcMediaPause)!
            //.SetPreviousActionIconResourceId(global::Android.Resource.Drawable.IcMenuAdd)!
            
            //.SetPlayActionIconResourceId(global::Android.Resource.Drawable.IcMediaPlay)
            //.SetPlayActionIconResourceId(global::Android.Resource.Drawable.IcMediaPause)
            //.SetNextActionIconResourceId(global::Android.Resource.Drawable.IcMediaNext)!
            .SetCustomActionReceiver(this)!
            .SetChannelImportance(3)!        //https://developer.android.com/reference/androidx/core/app/NotificationManagerCompat#IMPORTANCE_HIGH()
            .Build()!;

            _playerNotificationManager.SetPlayer(null);
            _playerNotificationManager.SetUseNextAction(true);
            _playerNotificationManager.SetUseFastForwardActionInCompactView(false);
            _playerNotificationManager.SetUseRewindActionInCompactView(false);
            _playerNotificationManager.SetUseFastForwardAction(false);
            _playerNotificationManager.SetUseRewindAction(false);
            _playerNotificationManager.SetUsePreviousActionInCompactView(true);

            _playerNotificationManager.SetUseNextActionInCompactView(true);
            //_playerNotificationManager.SetUsePreviousActionInCompactView(true);
            /*_playerNotificationManager.SetUsePreviousAction(true);
            _playerNotificationManager.SetUsePlayPauseActions(true);
            _playerNotificationManager.SetShowPlayButtonIfPlaybackIsSuppressed(true);
            _playerNotificationManager.SetUseNextActionInCompactView(true);
            _playerNotificationManager.SetUsePreviousActionInCompactView(true);


            _playerNotificationManager.SetUseStopAction(true);
            */
            _playerNotificationManager!.SetPlayer(mediaPlayer);
            _playerNotificationManager.SetMediaSessionToken(mediaSession.PlatformToken);

        }
    }

    //private void SubscribeToEvents()
    //{
    //    this.StatusChanged += OnStatusChanged;
    //    this.CoverReloaded += OnCoverReloaded;
    //    this.Playing += OnPlaying;
    //    this.Buffering += OnBuffering;
    //    this.PlayingChanged += OnPlayingChanged;
    //    this.IsPlayingChanged += OnIsPlayingChanged;
    //    this.IsSeekedFromNotificationBar += OnIsSeekedFromNotificationBar;
    //    this.TaskPlayEnded += OnTaskPlayEnded;
    //    this.TaskPlayNext += OnTaskPlayNext;
    //    this.TaskPlayPrevious += OnTaskPlayPrevious;
    //}
    public void StartForegroundService()
    {
        var serviceIntent = new Intent(Platform.AppContext, typeof(MediaPlayerService));
        Platform.AppContext.StartForegroundService(serviceIntent);
    }



    public void Pause()
    {
        if (mediaPlayer == null)
            return;

        if (mediaPlayer.IsPlaying)
            mediaPlayer.Pause();
    }


    public void Stop()
    {
        if (mediaPlayer == null)
            return;

        if (mediaPlayer.IsPlaying)
        {
            mediaPlayer.Stop();
        }

        StopForeground(StopForegroundFlags.Detach);


    }

    IBinder binder;

    public void Seek(int position = 0, PlaybackStateCode playbackStateCode = PlaybackStateCode.Stopped)
    {

        mediaPlayer.SeekTo(position);

        //IsSeekedFromNotificationBar?.Invoke(this, position);

    }

    public PlaybackStateCode? MediaPlayerState
    {
        get
        {
            return (PlaybackStateCode?)mediaPlayer.PlaybackState;
        }
    }
    public MediaPlayerService()
    {
        PlayingHandler = new Handler(Looper.MainLooper);

        // Create a runnable, restarting itself if the status still is "playing"
        PlayingHandlerRunnable = new Runnable(() =>
        {
            if (MediaPlayerState == PlaybackStateCode.Playing)
            {
                PlayingHandler.PostDelayed(PlayingHandlerRunnable, 1000);
            }
        });

        // On Status changed to PLAYING, start raising the Playing event
        StatusChanged += (sender, e) =>
        {
            if (MediaPlayerState == PlaybackStateCode.Playing)
            {
                PlayingHandler.PostDelayed(PlayingHandlerRunnable, 0);
            }
        };

      

        var w = Platform.AppContext.GetSystemService(AudioService) as AudioManager;

        var audioManager = w;
        //wifiManager = (WifiManager)GetSystemService(WifiService)!;
    }
    WifiManager wifiManager;
    public void OnPlayWhenReadyChanged(bool playWhenReady, int reason)
    {

    }
    public void OnPlaybackStateChanged(int playbackState)
    {


    }
    public void OnPlayerError(PlaybackException? error)
    {
        Console.WriteLine($"ExoPlayer Error: {error}");

    }
    public void OnPlayerErrorChanged(PlaybackException? error)
    {
        Console.WriteLine($"ExoPlayer Errorchanged : {error}");
    }

    // Implement other methods of IPlayer.IListener if needed
    public void OnIsPlayingChanged(bool isPlaying)
    {
        IsPlayingChanged?.Invoke(this, isPlaying);

        //mediaPlayer.CurrentMediaItemIndex
        //mediaPlayer.PreviousMediaItemIndex
        //mediaPlayer.NextMediaItemIndex
        //mediaPlayer.GetMediaItemAt(index)

        //// Log current player state (useful for debugging or additional logic)
        //Console.WriteLine($"ExoPlayer IsPlaying: {isPlaying}");
        //Console.WriteLine($"mediaPlayer IsPlaying: {mediaPlayer.IsPlaying}");
        //Console.WriteLine($"mediaPlayer title : {mediaPlayer.CurrentMediaItem.MediaMetadata.Title}");
        //Console.WriteLine($"mediaPlayer artist : {mediaPlayer.CurrentMediaItem.MediaMetadata.Artist}");
        //Console.WriteLine($"mediaPlayer position : {mediaPlayer.CurrentPosition}");

        if (isPlaying)
        {
            if (mediaPlayer.CurrentPosition == 0 && mediaPlayer.PreviousMediaItemIndex != -1)
            {
                // Player has started playing the next track
                Console.WriteLine($"Do action here because player has played NEXT, previous song was: {mediaPlayer.GetMediaItemAt(mediaPlayer.PreviousMediaItemIndex)?.MediaMetadata.Title}");
            }
            else if (mediaPlayer.CurrentPosition == 0 && mediaPlayer.NextMediaItemIndex != -1)
            {
                // Player has restarted the same track
                Console.WriteLine("Do action here because player has restarted the current song.");
            }
            else if (mediaPlayer.CurrentMediaItemIndex != mediaPlayer.PreviousMediaItemIndex && mediaPlayer.PreviousMediaItemIndex != -1)
            {
                // Player has moved to the previous track
                Console.WriteLine($"Do action here because player has played PREVIOUS, next song is: {mediaPlayer.GetMediaItemAt(mediaPlayer.NextMediaItemIndex)?.MediaMetadata.Title}");
            }
        }
        else
        {
            if (mediaPlayer.CurrentPosition > 0 && mediaPlayer.CurrentPosition < mediaPlayer.Duration)
            {
                // Player has been paused
                Console.WriteLine("Do action here because player has PAUSED.");
            }
            else if (mediaPlayer.CurrentPosition >= mediaPlayer.Duration)
            {
                // Player has reached the end of the track
                Console.WriteLine("Do action here because player has ENDED playing the current song.");

                IsPlayingChanged?.Invoke(this, false);
                TaskPlayEnded?.Invoke(this, EventArgs.Empty);
            }
        }

        // Additional logic for seeking
        long lastPosition = 0; // Track the last position for seeking (implement persistence elsewhere)
        if (mediaPlayer.CurrentPosition != lastPosition)
        {
            Console.WriteLine($"Do action here because player has SEEKED to position {mediaPlayer.CurrentPosition}.");
            lastPosition = mediaPlayer.CurrentPosition; // Update the last position
        }

        // Add other checks or actions as needed for additional player states
    }


    public void OnMaxSeekToPreviousPositionChanged(long maxSeekToPreviousPositionMs)
    {
        Console.WriteLine("Seek to now pos " + maxSeekToPreviousPositionMs);
    }

    /// <summary>
    /// Called when playback transitions to a media item or starts repeating a media item according to the current repeat mode.

    ///Note that this callback is also called when the value of getCurrentTimeline becomes non-empty or empty.
    /// </summary>
    /// <param name="mediaItem"></param>
    /// <param name="reason"></param>
    public void OnMediaItemTransition(MediaItem? mediaItem, int reason)
    {
        Console.WriteLine("OnMediaItemTransition => " + mediaItem?.MediaMetadata?.Title);
    }

    /*
     * Called when the value of getMediaMetadata changes.

This method may be called multiple times in quick succession.

onEvents will also be called to report this event along with other events that happen in the same Looper message queue iteration.
    */
    public void OnMediaMetadataChanged(MediaMetadata? native_mediaMetadata)
    {
    }

    public void OnPlaylistMetadataChanged(MediaMetadata? native_mediaMetadata)
    {
    }
    public void OnRepeatModeChanged(int repeatMode)
    {
    }

    public void OnSeekBackIncrementChanged(long seekBackIncrementMs)
    {
        Console.WriteLine("repeatModeSeekBack " + seekBackIncrementMs);
    }

    public void OnSeekForwardIncrementChanged(long seekForwardIncrementMs)
    {
        Console.WriteLine("repeatModeSeekFor " + seekForwardIncrementMs);
    }
    public void OnShuffleModeEnabledChanged(bool shuffleModeEnabled)
    {
        Console.WriteLine("repeatModeshuff " + shuffleModeEnabled);
    }
    
    MediaItem CurrentMediaItem;

    public void OnEvents(IPlayer? player, PlayerEvents? events)
    {
        
        //Console.WriteLine(events.);
        //if (events is not null)
        //{

        //    foreach (var item in items)
        //    {
        //        if (events.Contains(item))
        //        {


        //        }
        //    }
        //}
    }




    public void OnVolumeChanged(float volume)
    {

    }

    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? p0)
    {

        return mediaSession;
    }




    private readonly Handler? PlayingHandler;
    private readonly Runnable? PlayingHandlerRunnable;
    private IListenableFuture listenableFuture;
    //public void OnStart(Intent? intent, int startId, int otherInt)
    //{

    //    var sessionToken = new SessionToken(Platform.AppContext, new ComponentName(this, Java.Lang.Class.FromType(typeof(MediaPlayerService))));

    //    var controllerFuture = new MediaController.Builder(Platform.AppContext, sessionToken).BuildAsync()!;

    //    controllerFuture.AddListener(new Runnable(() => {
    //        try
    //        {
    //            var mainAct = IPlatformApplication.Current!.Services.GetService<MainActivity>()!;
    //            mainAct.CustomMediController = (IMediaController)controllerFuture.Get()!;
    //            var t = controllerFuture.Get()!;
    //            var re = mainAct.MediaController;



    //            Console.WriteLine("MediaController obtained in service.");
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Error getting MediaController in service: {ex}");
    //        }
    //    }), null); // Passing null here implicitly uses the calling thread's looper, which in this case should be the main thread

    //    StartForegroundService();
    //}
    public new virtual void OnCreate()
    {
        //base.OnCreate();

    }
    //    // Initialize ExoPlayer
    //    var audioAttributes = new AudioAttributes.Builder()!
    //        .SetUsage(C.UsageMedia)!
    //        .SetContentType(2)!
    //        .Build()!;
    //    var renderersFactory = new DefaultRenderersFactory(Platform.AppContext)!
    //    .SetEnableDecoderFallback(true)! // Allows fallback to alternative decoders if primary fails
    //.SetEnableAudioFloatOutput(true)! // Enables floating-point audio output
    //.SetEnableAudioTrackPlaybackParams(true); // Enables playback parameters adjustments



        //    mediaPlayer = new ExoPlayerBuilder(Platform.AppContext, renderersFactory)!
        //        .SetAudioAttributes(audioAttributes, true)!
        //        .SetName("Dimmer Music Player")!
        //        .SetWakeMode(1)!
        //        .Build()!;

        //    mediaPlayer.AddListener(this); // Set the listener for player events

        //    CommandButton commandButton = new CommandButton.Builder(global::Android.Resource.Drawable.IcDelete)
        //        .SetDisplayName("Del")
        //        .SetCustomIconResId(global::Android.Resource.Drawable.IcDelete)
        //        .Build();
        //    List<CommandButton> cBB = new();

        //    cBB.Add(commandButton);
        //    // Initialize MediaSession (handled by MediaSessionService, but you can customize it)

        //    // Create an Intent to open MainActivity
        //    Intent intent = new Intent(this, typeof(MainActivity));
        //    intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

        //    // Create a PendingIntent
        //    PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);


        //    var mediaSessionBuilder = new MediaSession.Builder(Platform.AppContext, mediaPlayer)
        //        .SetCallback(this);
        //        //.SetCallback(NotifCB);

        //    //mediaSession = mediaSessionBuilder.Build()!
        //    mediaSessionBuilder.Build().SetSessionActivity(null, pendingIntent);

        //    CreateNotificationChannel();

        //}
    protected virtual void OnTaskPlayEnded()
    {
        IsPlayingChanged?.Invoke(this, false);
        TaskPlayEnded?.Invoke(this, EventArgs.Empty);
    }



    public int Position
    {
        get
        {
            if (mediaPlayer == null
                || MediaPlayerState != PlaybackStateCode.Playing
                    && MediaPlayerState != PlaybackStateCode.Paused)
            {
                return -1; // Indicates an invalid position
            }

            long currentPosition = mediaPlayer.CurrentPosition;

            // Safely check if the position fits into an int
            if (currentPosition > int.MaxValue)
                return int.MaxValue; // Cap at the max int value
            else if (currentPosition < int.MinValue)
                return int.MinValue; // Cap at the min int value (unlikely)

            return (int)currentPosition;
        }
    }

    public int Duration
    {
        get
        {
            if (mediaPlayer == null
                || MediaPlayerState != PlaybackStateCode.Playing
                    && MediaPlayerState != PlaybackStateCode.Paused)
            {
                return -1; // Indicates an invalid position
            }

            long currentDuration = mediaPlayer.Duration;

            // Safely check if the position fits into an int
            if (currentDuration > int.MaxValue)
                return int.MaxValue; // Cap at the max int value
            else if (currentDuration < int.MinValue)
                return int.MinValue; // Cap at the min int value (unlikely)

            return (int)currentDuration;
        }

    }


    private Bitmap? cover;

    public Bitmap? Cover
    {
        get
        {
            return cover;
        }
        set
        {
            cover = value;
        }
    }


    private class MediaControllerListener : Java.Lang.Object, IRunnable
    {
        private readonly MediaPlayerService _service;
        private readonly IListenableFuture _controllerFuture;

        public MediaControllerListener(MediaPlayerService service, IListenableFuture controllerFuture)
        {
            _service = service;
            _controllerFuture = controllerFuture;
        }

        public void Run()
        {
            try
            {
                var mainAct = IPlatformApplication.Current.Services.GetService<MainActivity>();

                mainAct.CustomMediController = (IMediaController)_controllerFuture.Get(); // Or .Get()

                // Now you have the mediaController. You can use it here within the service
                // if you need to perform actions that require a MediaController context.
                // However, since this is the service itself, you usually interact directly
                // with your ExoPlayer instance (the 'player' field).

                Console.WriteLine("MediaController obtained in service.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting MediaController in service: {ex}");
            }
        }
    }

    //public override void OnDestroy()
    //{
    //    base.OnDestroy();
    //    mediaSession?.Release();
    //    mediaSession.Release();
    //    mediaSession=null;
    //    //this.Dispose();
    //}

    public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
    {
        switch (focusChange)
        {
            case AudioFocus.Gain:
                break;
            case AudioFocus.GainTransient:
                break;
            case AudioFocus.GainTransientExclusive:
                break;
            case AudioFocus.GainTransientMayDuck:
                break;
            case AudioFocus.Loss:
                this.mediaPlayer.Pause();
                IsPlayingChanged?.Invoke(this, false);
                break;
            case AudioFocus.LossTransient:
                this.mediaPlayer.Pause();
                IsPlayingChanged?.Invoke(this, false);
                break;
            case AudioFocus.LossTransientCanDuck:
                if (mediaPlayer.IsPlaying)
                {
                    //mediaPlayer.Volume = 0.2F;
                }
                break;
            case AudioFocus.None:
                break;
            default:
                break;
        }
    }
    public class NotifCB: MediaPlayerService, ICallback
    {
        /*
         * 
			// Metadata.xml XPath method reference: path="/api/package[@name='androidx.media3.session']/interface[@name='MediaSession.Callback']/method[@name='onAddMediaItems' and count(parameter)=3 and parameter[1][@type='androidx.media3.session.MediaSession'] and parameter[2][@type='androidx.media3.session.MediaSession.ControllerInfo'] and parameter[3][@type='java.util.List&lt;androidx.media3.common.MediaItem&gt;']]"
			[Register ("onAddMediaItems", "(Landroidx/media3/session/MediaSession;Landroidx/media3/session/MediaSession$ControllerInfo;Ljava/util/List;)Lcom/google/common/util/concurrent/ListenableFuture;", "GetOnAddMediaItems_Landroidx_media3_session_MediaSession_Landroidx_media3_session_MediaSession_ControllerInfo_Ljava_util_List_Handler:AndroidX.Media3.Session.MediaSession/ICallback, Xamarin.AndroidX.Media3.Session")]
			virtual unsafe global::Google.Common.Util.Concurrent.IListenableFuture? OnAddMediaItems (global::AndroidX.Media3.Session.MediaSession? mediaSession, global::AndroidX.Media3.Session.MediaSession.ControllerInfo? controller, global::System.Collections.Generic.IList<global::AndroidX.Media3.Common.MediaItem>? mediaItems)
			{
				const string __id = "onAddMediaItems.(Landroidx/media3/session/MediaSession;Landroidx/media3/session/MediaSession$ControllerInfo;Ljava/util/List;)Lcom/google/common/util/concurrent/ListenableFuture;";
				IntPtr native_mediaItems = global::Android.Runtime.JavaList<global::AndroidX.Media3.Common.MediaItem>.ToLocalJniHandle (mediaItems);
				try {
					JniArgumentValue* __args = stackalloc JniArgumentValue [3];
					__args [0] = new JniArgumentValue ((mediaSession == null) ? IntPtr.Zero : ((global::Java.Lang.Object) mediaSession).Handle);
					__args [1] = new JniArgumentValue ((controller == null) ? IntPtr.Zero : ((global::Java.Lang.Object) controller).Handle);
					__args [2] = new JniArgumentValue (native_mediaItems);
					var __rm = _members.InstanceMethods.InvokeVirtualObjectMethod (__id, this, __args);
					return global::Java.Lang.Object.GetObject<global::Google.Common.Util.Concurrent.IListenableFuture> (__rm.Handle, JniHandleOwnership.TransferLocalRef);
				} finally {
					JNIEnv.DeleteLocalRef (native_mediaItems);
					global::System.GC.KeepAlive (mediaSession);
					global::System.GC.KeepAlive (controller);
					global::System.GC.KeepAlive (mediaItems);
				}
        */


        //public IListenableFuture? OnAddMediaItems(MediaSession? session, MediaSession.ControllerInfo? controller, IList<MediaItem>? mediaItems)
        //{
        //    Console.WriteLine("OnAddMediaItems!!!!!!!!!!");
        //    return listenableFuture;

        //}
        public bool OnMediaButtonEvent(MediaSession? session, MediaSession.ControllerInfo? controller, Intent? intent)
        {
            Console.WriteLine(intent.Action + "Actiiiiiiiiiiooooooooooooooooooonnnnnnnnnn");
            return true;
        }
        //public IListenableFuture OnCustomCommand(MediaSession? session, MediaSession.ControllerInfo? controller, SessionCommand? customCommand, Bundle? args)
        //{
        //    Console.WriteLine("OnCustomCommanddddddddddd");
        //    return listenableFuture;
        //}

        //public ConnectionResult? GetOnConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        //{
        //    Console.WriteLine("Connecteddddddddddddddddd");
        //    var s = new ConnectionResult.AcceptedResultBuilder(session)
        //        .Build();
        //    return s;
        //}
    }
    public ConnectionResult? OnConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
    {
        
        //mediaSession.Player.
        Console.WriteLine("Connecteddddddddddddddddd");
        var s = new ConnectionResult.AcceptedResultBuilder(session)
            .Build();
        return s;
    }
    public bool OnMediaButtonEvent(MediaSession? session, MediaSession.ControllerInfo? controller, Intent? intent)
    {
        Console.WriteLine(intent.Action + "Actiiiiiiiiiiooooooooooooooooooonnnnnnnnnn");
        return true;
    }
    /// <summary>
    /// Defines the intent that will be launched when the user clicks on the notification.
    /// Typically, this opens the main activity of your app.
    /// </summary>
    public PendingIntent? CreateCurrentContentIntent(IPlayer? player)
    {
        Console.WriteLine("CreateCurrentContentIntent called");

        // Create an intent to open your main activity
        var intent = new Intent(Platform.AppContext, typeof(MainActivity)); //see A in mainactivity file
        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        Console.WriteLine("this int act " + intent.Action);
        return PendingIntent.GetActivity(
            Platform.AppContext,
            0,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        
        
    }

    /// <summary>
    /// Provides the secondary text for the notification (e.g., artist name).
    /// </summary>
    public ICharSequence? GetCurrentContentTextFormatted(IPlayer? player)
    {

        // Extract artist name from the current media item
        var metadata = player?.CurrentMediaItem?.MediaMetadata;
        return metadata?.Artist;
    }

    /// <summary>
    /// Provides the primary text for the notification (e.g., song title).
    /// </summary>
    public ICharSequence? GetCurrentContentTitleFormatted(IPlayer? player)
    {

        // Extract title from the current media item
        var metadata = player?.CurrentMediaItem?.MediaMetadata;
        //return metadata?.Title;
        return new Java.Lang.String("YB :D");
    }

    /// <summary>
    /// Provides the large icon (e.g., album art) for the notification.
    /// </summary>
    //public void OnNotificationCancelled(int notificationId, bool dismissedByUser)
    //{

    //}
    public void OnNotificationPosted(int notificationId, Notification? notif, bool ongoing)
    {
        
        //Console.WriteLine($"notif.ContentIntent.IsService {notif.ContentIntent.IsService}");
        //Console.WriteLine($"notif.ContentIntent.IsService {notif.ContentIntent.IntentSender}");
        //Console.WriteLine($"notif.ContentIntent.IsService {notif.ContentIntent.CreatorPackage}");
        //Console.WriteLine($"notif.ContentIntent.IsService {notif.ContentIntent.CreatorUserHandle.DescribeContents()}");
        //Console.WriteLine($"notif.ContentIntent.IsService {notif.ContentIntent.CreatorUserHandle.Class}");
        //Console.WriteLine($"notif is {notif}");
        //Console.WriteLine($"is ongoing {ongoing} ");
        //Console.WriteLine($"is ongoing {notif.Actions.Count} ");
        //Console.WriteLine(mediaPlayer.IsPlaying);
        //Console.WriteLine(mediaPlayer.Volume);
        
    }
    public Bitmap? GetCurrentLargeIcon(IPlayer? player, BitmapCallback? callback)
    {

        var metadata = player?.CurrentMediaItem?.MediaMetadata;
        if (metadata?.ArtworkData != null && metadata.ArtworkData.Count > 0)
        {
            try
            {
                // Convert IList<byte> to byte[]
                byte[] artworkBytes = metadata.ArtworkData.ToArray();

                // Decode byte array to Bitmap
                return BitmapFactory.DecodeByteArray(artworkBytes, 0, artworkBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding artwork: {ex.Message}");
                // Return a default icon if decoding fails
                return BitmapFactory.DecodeResource(Platform.AppContext.Resources, global::Android.Resource.Drawable.IcMenuReportImage);
            }
        }

        //player.CurrentMediaItemIndex
        //player.NextMediaItemIndex
        //player.PreviousMediaItemIndex
        //player.MediaItemCount
        //player.CurrentMediaItem
        
        // Return a default icon if no artwork is available
        return BitmapFactory.DecodeResource(Platform.AppContext.Resources, global::Android.Resource.Drawable.IcMenuReportImage);
    }
   


    public IList<string>? GetCustomActions(IPlayer? player)
    {
        // If the player is playing, show pause, next, favorite, etc.
        if (player?.IsPlaying == true)
        {
            return new List<string>
        {
            "com.xamarin.action.PAUSE",
            "com.xamarin.action.NEXT",
    "com.xamarin.action.PREVIOUS",
            "com.xamarin.action.FAVORITE"
        };
        }
        else
        {
            // If the player is paused, show play, next, favorite, etc.
            return new List<string>
        {
            "com.xamarin.action.PLAY",
            "com.xamarin.action.NEXT",
    "com.xamarin.action.PREVIOUS",
            "com.xamarin.action.FAVORITE"
        };
        }
    }

    

    public void OnAvailableCommandsChangedFromPlayer(int p0, Bundle? p1)
    {
        Console.WriteLine("avcomchangedPlayer");
    }

    public void OnAvailableCommandsChangedFromSession(int p0, Bundle? p1, Bundle? p2)
    {
        Console.WriteLine("OnAvailableCommandsChangedFromSession");
    }

    public void OnChildrenChanged(int p0, string? p1, int p2, Bundle? p3)
    {
        Console.WriteLine("OnChildrenChanged");
    }


    //private static IntPtr n_OnCustomCommand_Landroidx_media3_session_MediaSession_Landroidx_media3_session_MediaSession_ControllerInfo_Landroidx_media3_session_SessionCommand_Landroid_os_Bundle_ (IntPtr jnienv, IntPtr native__this, IntPtr native_session, IntPtr native_controller, IntPtr native_customCommand, IntPtr native_args)

    
    ////BTW IntPtr = void!!!
    //public ConnectionResult OnConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
    //{
    //    Console.WriteLine("Connected");
    //    var s = new ConnectionResult.AcceptedResultBuilder(session)

    //        .Build();
    //    return s;
    //}
    //public IListenableFuture OnPlaybackResumption(MediaSession? session, MediaSession.ControllerInfo? controller)
    //{
    //    Console.WriteLine("OnPlaybackResumption");
    //    return listenableFuture;
    //}
    //public int OnPlayerCommandRequest(MediaSession? session, MediaSession.ControllerInfo? controller, int playerCommand)
    //{

    //    return playerCommand;
    //}
    //public bool OnMediaButtonEvent(MediaSession? session, MediaSession.ControllerInfo? controller, Intent? intentt)
    //{
    //    Console.WriteLine(intentt.Action);
    //    return true;
    //}

    public void OnDisconnected(int p0)
    {
        Console.WriteLine("OnDisconnected");
    }

    public void OnError(int p0, Bundle? p1)
    {
        Console.WriteLine("OnError");
    }

    public void OnExtrasChanged(int p0, Bundle? p1)
    {
        Console.WriteLine("OnExtrasChanged");
    }

    public void OnLibraryResult(int p0, Bundle? p1)
    {
        Console.WriteLine("1dw");
    }

    public void OnPeriodicSessionPositionInfoChanged(int p0, Bundle? p1)
    {
        Console.WriteLine("11a");
    }

    public void OnPlayerInfoChanged(int p0, Bundle? p1, bool p2)
    {
        Console.WriteLine("11");
    }

    public void OnPlayerInfoChangedWithExclusions(int p0, Bundle? p1, Bundle? p2)
    {
        Console.WriteLine("122");
    }

    public void OnRenderedFirstFrame(int p0)
    {
        Console.WriteLine("13");
    }

    public void OnSearchResultChanged(int p0, string? p1, int p2, Bundle? p3)
    {
        Console.WriteLine("12");
    }

    public void OnSessionActivityChanged(int p0, PendingIntent? p1)
    {
        Console.WriteLine("12");
    }

    public void OnSessionResult(int p0, Bundle? p1)
    {
        Console.WriteLine("1");
    }

    public void OnSetCustomLayout(int p0, IList<Bundle>? p1)
    {
        Console.WriteLine("13");
    }

    public void OnSetMediaButtonPreferences(int p0, IList<Bundle>? p1)
    {
        Console.WriteLine("14");
    }

    public IBinder? AsBinder()
    {
        Console.WriteLine("15");
        return null;
    }



    public void OnCustomAction(IPlayer? p0, string? action, Intent? p2)
    {
        Console.WriteLine($"Custom action triggered: {action}");
        switch (action)
        {
            case "com.xamarin.action.PLAY":
                mediaPlayer?.Play();
                break;
            case "com.xamarin.action.PAUSE":
                mediaPlayer?.Pause();
                break;
            case "com.xamarin.action.NEXT":
                // next track logic, e.g.:
                // mediaPlayer?.SeekToNextMediaItem();
                break;
            case "com.xamarin.action.PREVIOUS":
                // previous track logic, e.g.:
                // mediaPlayer?.SeekToPreviousMediaItem();
                break;
            case "com.xamarin.action.FAVORITE":
                // your "favorite" logic
                break;
            default:
                
                break;
        }
    }
    IDictionary<string, Action>? ICustomActionReceiver.CreateCustomActions(Context? p0, int instanceId)
    //public IDictionary<string, Action>? CreateCustomActions(Context? p0, int instanceId)
    {
        Intent intent = new Intent(Platform.AppContext, typeof(MainActivity));

        var pIntent = PendingIntent.GetService(
            Platform.AppContext,
            instanceId,
            intent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        if (Platform.AppContext == null)
        {

            return null;
        }

        var customActions = new Dictionary<string, Action>
    {
//        {
//            "com.xamarin.action.PLAY",
//            new Action.Builder(
//                global::Android.Resource.Drawable.IcMediaPlay,
//                "Play",
//                pIntent
//                //PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
//            ).Build()
//        },
//        {
//            "com.xamarin.action.PAUSE",
//            new Action.Builder(
//                global::Android.Resource.Drawable.IcMediaPause,
//                "Pause",
//pIntent).Build()
//        },
//        {
//            "com.xamarin.action.PREVIOUS",
//            new Action.Builder(
//                global::Android.Resource.Drawable.IcMediaPrevious,
//                "Previous",
//                pIntent
//            ).Build()
//        },
        {
            ActionNext,
            new Action.Builder(
                global::Android.Resource.Drawable.IcMediaNext,
                "Next",
                PendingIntent.GetService(
                    Platform.AppContext,
                    instanceId,
                    new Intent(ActionNext),
                    PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
            
            ).Build()
        },
     
    };

        return customActions;
        //throw new NotImplementedException();
    }

    void IMediaController.OnConnected(int p0, Bundle? p1)
    {
        Console.WriteLine("Well Conned!");
    }

    void IMediaController.OnCustomCommand(int p0, Bundle? p1, Bundle? p2)
    {
        Console.WriteLine("Custom Command");
    }

    class StreamMediaDataSource : MediaDataSource
    {
        System.IO.Stream data;
        public StreamMediaDataSource(System.IO.Stream data)
        {
            this.data = data;
        }
        public override long Size => data.Length;

        public override int ReadAt(long position, byte[]? buffer, int offset, int size)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (data.CanSeek)
            {
                data.Seek(position, SeekOrigin.Begin);
            }

            return data.Read(buffer, offset, size);
        }

        public override void Close()
        {
            data.Dispose();
            data = System.IO.Stream.Null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            data.Dispose();
            data = System.IO.Stream.Null;
        }
    }

    public class MediaSessionCB : Java.Lang.Object, MediaSession.ICallback
    {
        public MediaPlayerService Service { get; }

        public bool OnMediaButtonEvent(MediaSession? session, MediaSession.ControllerInfo? controller, Intent? intent)
        {
            Console.WriteLine("IntentWas "+ intent.Action);

            return true;
        }
        public MediaSessionCB(MediaPlayerService service)
        {
            Service = service;
        }
        public ConnectionResult? OnConnect(MediaSession? session, MediaSession.ControllerInfo? controller)
        {
            Console.WriteLine("Connected");
            var s = new ConnectionResult.AcceptedResultBuilder(session)
                .Build();
            return s;
        }
    }
}