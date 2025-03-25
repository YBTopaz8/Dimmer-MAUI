using Android.App;
using Android.Content;
using Android.Media;
using Android.Net.Wifi;
using Android.OS;
using Android.Media.Session;
using Android.Graphics;
using Activity = Android.App.Activity;
using Application = Android.App.Application;
using Android.Content.PM;
using Uri = Android.Net.Uri;
using Exception = System.Exception;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

[Service(Enabled = true, Exported = true, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter(new[] { ActionPlay, ActionPause, ActionStop, ActionTogglePlayback, ActionNext, ActionPrevious, ActionSeekTo, ActionSetRating })]
public class MediaPlayerService : Service,
   AudioManager.IOnAudioFocusChangeListener, MediaPlayer.IOnCompletionListener,
   MediaPlayer.IOnPreparedListener, MediaPlayer.IOnErrorListener
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

    public MediaPlayer mediaPlayer;
    private AudioManager audioManager;
    private MediaSession mediaSession;
    public MediaController mediaController;
    private WifiManager wifiManager;
    private WifiManager.WifiLock wifiLock;

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

    public Activity MainAct;
    public MediaPlay mediaPlay;
    public bool isCurrentEpisode = true;

    private readonly Handler PlayingHandler;
    private readonly Java.Lang.Runnable PlayingHandlerRunnable;

    private ComponentName remoteComponentName;

    public PlaybackStateCode MediaPlayerState
    {
        get
        {
            return mediaController.PlaybackState != null
                ? mediaController.PlaybackState.State
                : PlaybackStateCode.None;
        }
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
    public MediaPlayerService()
    {
        IsFavorite = Rating.NewHeartRating(false);
        PlayingHandler = new Handler(Looper.MainLooper);

        // Create a runnable, restarting itself if the status still is "playing"
        PlayingHandlerRunnable = new Java.Lang.Runnable(() =>
        {
            OnPlaying(EventArgs.Empty);

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
    }
    protected virtual void OnStatusChanged(EventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }

    protected virtual void OnPlayingChanged(bool e)
    {
        PlayingChanged?.Invoke(this, e);
        IsPlayingChanged?.Invoke(this, e);
    }

    protected virtual void OnCoverReloaded(EventArgs e)
    {
        if (CoverReloaded != null)
        {
            CoverReloaded(this, e);
            StartNotification();
            UpdateMediaMetadataCompat();
        }
    }

    protected virtual void OnPlaying(EventArgs e)
    {
        Playing?.Invoke(this, e);
    }

    protected virtual void OnBuffering(EventArgs e)
    {
        Buffering?.Invoke(this, e);
    }


    /// <summary>
    /// On create simply detect some of our managers
    /// </summary>
    public override void OnCreate()
    {
        base.OnCreate();
        //Find our audio and notificaton managers
        audioManager = (AudioManager)GetSystemService(AudioService)!;
        wifiManager = (WifiManager)GetSystemService(WifiService)!;

        remoteComponentName = new ComponentName(PackageName!, new RemoteControlBroadcastReceiver().ComponentName);
    }

    /// <summary>
    /// Will register for the remote control client commands in audio manager
    /// </summary>
    private void InitMediaSession()
    {
        try
        {
            if (mediaSession == null)
            {
                
                Intent nIntent = new Intent(Platform.AppContext, typeof(MainActivity));
                remoteComponentName = new ComponentName(PackageName, new RemoteControlBroadcastReceiver().ComponentName);
                mediaSession = new MediaSession(Platform.AppContext, "MauiStreamingAudio"/*, remoteComponentName*/); //TODO
                //mediaSession.SetMediaButtonBroadcastReceiver(remoteComponentName);

                var pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, PendingIntentFlags.Mutable);

                mediaSession.SetSessionActivity(pendingIntent);
                mediaSession.SetRatingType(RatingStyle.Heart);
                
                mediaController = new MediaController(Platform.AppContext, mediaSession.SessionToken);
            }

            mediaSession.Active = true;
            
            mediaSession.SetCallback(new MediaSessionCallback((MediaPlayerServiceBinder)binder));

            mediaSession.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
            //useless ^
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    /// <summary>
    /// Intializes the player.
    /// </summary>
    public void DoInitialInitialization()
    {
        InitializePlayer();
        InitMediaSession();

    }
    private void InitializePlayer()
    {
        mediaPlayer = new MediaPlayer();

        mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
            .SetContentType(AudioContentType.Music)
            .SetUsage(AudioUsageKind.Media).Build());

        mediaPlayer.SetWakeMode(Platform.AppContext, WakeLockFlags.Partial);

        mediaPlayer.SetOnPreparedListener(this);
        mediaPlayer.SetOnErrorListener(this);
        mediaPlayer.SetOnCompletionListener(this);

    }
    
    public bool OnError(MediaPlayer? mp, MediaError what, int extra)
    {
        UpdatePlaybackState(PlaybackStateCode.Error);
        Console.WriteLine(DateTime.Now.ToString() + "Step 8 ERROR on " + mediaPlay.Name + " What is " + what);
        Task.Run(() => Play());
        return true;
    }

    public void OnCompletion(MediaPlayer? mp)
    {
        IsPlayingChanged?.Invoke(this, false);
        TaskPlayEnded?.Invoke(this, EventArgs.Empty);
        UpdatePlaybackState(PlaybackStateCode.Stopped);
    }

    public void OnPrepared(MediaPlayer? mp)
    {
        try
        {
            mp.SeekTo(positionInMs);
            mp.Start();
        }
        catch (Exception ex)
        {
            Task.Run(() => Play());
            Console.WriteLine(ex.Message);
        }
        UpdatePlaybackState(PlaybackStateCode.Playing);
        IsPlayingChanged?.Invoke(this, true);
        Console.WriteLine(DateTime.Now.ToString() + "Step 9 Prepared "+mediaPlay.Name);
    }

    public Rating? IsFavorite { get; set; } = Rating.NewHeartRating(false);
        
    public int Position
    {
        get
        {
            if (mediaPlayer == null
                || MediaPlayerState != PlaybackStateCode.Playing
                    && MediaPlayerState != PlaybackStateCode.Paused)
                return -1;
            else
                return mediaPlayer.CurrentPosition;
        }
    }

    public int Duration
    {
        get
        {
            if (mediaPlayer == null || MediaPlayerState != PlaybackStateCode.Playing && MediaPlayerState != PlaybackStateCode.Paused)
                return 0;
            else
                Console.WriteLine("Step 10 getDuration");
            return mediaPlayer.Duration;
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
    public static Bitmap? GetBitmapFromFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found: " + filePath);
            return null;
        }
        Bitmap? f = BitmapFactory.DecodeFile(filePath);
        return f;
    }


    /// <summary>
    /// Intializes the player.
    /// </summary>
    int positionInMs = 0;
    public void Play(int position = 0)
    {
        Console.WriteLine("Step 6 Play method from mediaplayerservice");
        if (mediaPlay.ImagePath is not null)
        {
            //Cover = GetImageBitmapFromBytes(mediaPlay.ImageBytes);
            Cover = GetBitmapFromFilePath(mediaPlay.ImagePath);
        }
        positionInMs = position;
        if (mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Paused)
        {
            //We are simply paused so just start again

            Console.WriteLine("From Play Seeking to " + position);
            mediaPlayer.SeekTo(position);

            mediaPlayer.Start();
            UpdatePlaybackState(PlaybackStateCode.Playing);
            

            //Update the metadata now that we are playing
            UpdateMediaMetadataCompat();
            return;
        }


        if (mediaPlayer == null)
        {
            Console.WriteLine("Media Player is null");
            InitializePlayer();
        }

        if (mediaSession == null)
        {
            Console.WriteLine("mediaSession is null");
            InitMediaSession();
        }

        if (mediaPlayer.IsPlaying && isCurrentEpisode)
        {
            UpdatePlaybackState(PlaybackStateCode.Playing);
            return;
        }

        isCurrentEpisode = true;
        mediaPlayer?.Release();
        PrepareAndPlayMediaPlayer();
        IsPlayingChanged?.Invoke(this, true);
    }
    
    private void PrepareAndPlayMediaPlayer()
    {
        try
        {
            Console.WriteLine(DateTime.Now.ToString() + "Preparing " +mediaPlay.Name );
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
            {                
                mediaPlayer = new MediaPlayer();
                MediaMetadataRetriever metaRetriever = new MediaMetadataRetriever();

                
                mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()!
                    .SetContentType(AudioContentType.Music)!
                    .SetUsage(AudioUsageKind.Media)!.Build()!);

                mediaPlayer.SetWakeMode(Platform.AppContext, WakeLockFlags.Partial);

                var file = mediaPlay.URL;

                if (File.Exists(file))
                {
                    try
                    {
                        mediaPlayer.SetDataSource(file);
                    }
                    catch
                    {
                        var context = Platform.AppContext; 
                        var encodedPath =  Uri.Encode(file)
                            ?? throw new Exception("Unable to generate encoded path.");
                        var uri = Uri.Parse(encodedPath)
                            ?? throw new Exception("Unable to parse encoded path.");

                        mediaPlayer.SetDataSource(context, uri);
                    }
                }

                mediaPlayer.SetOnPreparedListener(this);
                mediaPlayer.SetOnErrorListener(this);
                mediaPlayer.SetOnCompletionListener(this);

                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    var focusResult = audioManager.RequestAudioFocus(new AudioFocusRequestClass
                    .Builder(AudioFocus.Gain)!
                    .SetOnAudioFocusChangeListener(this)!
                    .Build()!)!;

                    if (focusResult != AudioFocusRequest.Granted)
                    {
                        // Could not get audio focus
                        Console.WriteLine("Could not get audio focus");
                    }
                }

                //UpdatePlaybackState(PlaybackStateCode.Buffering);
                triedCount++;
                mediaPlayer.Prepare();
             
                UpdateMediaMetadataCompat(metaRetriever);
                StartNotification();
            }

        }
        catch (Exception ex)
        {
            UpdatePlaybackStateStopped();
            failedCount++;
            // Unable to start playback log error
            Console.WriteLine("Error !!!!!!!!!!!! when preparing. Msg: " + ex.Message
                + " failedCount = " + failedCount
                + " tried Before Count= " + triedCount
                + " tried after Count= " + triedAfterCount);
            Console.WriteLine($"Is Playing = {mediaPlayer.IsPlaying}");
        }
    }
    int failedCount = 0;
    int triedCount = 0;
    int triedAfterCount = 0;
   
    public void Seek(int position = 0, PlaybackStateCode playbackStateCode = PlaybackStateCode.Stopped)
    {
            positionInMs = position;
        mediaPlayer?.SeekTo(position, MediaPlayerSeekMode.ClosestSync);
        IsSeekedFromNotificationBar?.Invoke(this, position);
        UpdatePlaybackState(MediaPlayerState, position);
        
        IsPlayingChanged?.Invoke(this, true);

    }

    public Task PlayNext()
    {
        Console.WriteLine("Step 5: TaskPlayNext called");
        TaskPlayNext?.Invoke(this, EventArgs.Empty);
        //if (mediaPlayer != null)
        //{
        //    mediaPlayer.Reset();
        //    mediaPlayer.Release();
        //    mediaPlayer = null;
        //}
        UpdatePlaybackState(PlaybackStateCode.SkippingToNext);

        return Task.CompletedTask;
    }

    public void PlayPrevious()
    {
        // Start current track from beginning if it's the first track or the track has played more than 3sec and you hit "playPrevious".
        TaskPlayPrevious?.Invoke(this, EventArgs.Empty);
        //if (Position > 3000)
        //{
        //    Seek(0);
        //}
        //else
        //{
        //    if (mediaPlayer != null)
        //    {
        //        mediaPlayer.Reset();
        //        mediaPlayer.Release();
        //        mediaPlayer = null;
        //    }

        UpdatePlaybackState(PlaybackStateCode.SkippingToPrevious);

        //    Play();
        //}
    }

    public void PlayPause()
    {
        if (mediaPlayer == null || mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Paused)
        {
            Play();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.IsPlaying)
                mediaPlayer.Pause();
            UpdatePlaybackState(PlaybackStateCode.Paused);
        
    }

    public void Stop()
    {
        Task.Run(() =>
        {
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Stop();
            }

            UpdatePlaybackState(PlaybackStateCode.Stopped);
            mediaPlayer.Reset();
            NotificationHelper.StopNotification(Platform.AppContext!);
            StopForeground(StopForegroundFlags.Detach);
            UnregisterMediaSessionCompat();
        });
    }


    public AudioManager ExposeAudioManager()
    {
        return audioManager;
    }

    public void UpdatePlaybackStateStopped()
    {
        UpdatePlaybackState(PlaybackStateCode.Stopped);
        if (mediaPlayer != null)
        {
            mediaPlayer.Release();
            mediaPlayer = null;
        }
    }

    private void UpdatePlaybackState(PlaybackStateCode state, int SeekedPosition = 0)
    {
        if (mediaSession == null || mediaPlayer == null)
            return;

        mediaController ??= new MediaController(Platform.AppContext, mediaSession.SessionToken);
        if (SeekedPosition == 0)
        {
            SeekedPosition = Position;
        }
        try
        {
            PlaybackState.Builder stateBuilder = new PlaybackState.Builder()
                .SetActions(
                    PlaybackState.ActionPause |
                    PlaybackState.ActionPlay |
                    PlaybackState.ActionPlayPause |
                    PlaybackState.ActionSkipToNext |
                    PlaybackState.ActionSkipToPrevious |
                    PlaybackState.ActionStop |
                    PlaybackState.ActionSeekTo |
                    PlaybackState.ActionSetRating
                )!
                .SetState(state, SeekedPosition, 1.0f, SystemClock.ElapsedRealtime())!;

            mediaSession.SetPlaybackState(stateBuilder.Build());

            OnStatusChanged(EventArgs.Empty);

            if (state == PlaybackStateCode.Playing || state == PlaybackStateCode.Paused)
            {
                StartNotification();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void StartNotification()
    {
        if (mediaSession == null)
            return;
        var s = Application.Context;
        var notif = NotificationHelper
            .StartNotification(s, mediaController.Metadata!, 
            mediaSession, Cover, 
            MediaPlayerState == PlaybackStateCode.Playing);
        notif.TickerText = new Java.Lang.String($"Now Playing {mediaPlay.Name}");

        StartForeground(id: 1000, notif); //or this?
    }

    /// <summary>
    /// Updates the metadata on the lock screen and on notification bar
    /// </summary>
    private void UpdateMediaMetadataCompat(MediaMetadataRetriever metaRetriever = null)
    {
        if (mediaSession == null)
            return;

        MediaMetadata.Builder builder = new MediaMetadata.Builder();

        if (metaRetriever != null)
        {
            builder
            .PutString(MediaMetadata.MetadataKeyAlbum, metaRetriever.ExtractMetadata(MetadataKey.Album))
            .PutString(MediaMetadata.MetadataKeyArtist, mediaPlay.Author ?? metaRetriever.ExtractMetadata(MetadataKey.Artist))
            .PutString(MediaMetadata.MetadataKeyTitle, mediaPlay.Name ?? metaRetriever.ExtractMetadata(MetadataKey.Title))
            .PutRating( MediaMetadata.MetadataKeyRating, Rating.NewHeartRating(true))
            .PutLong(MediaMetadata.MetadataKeyDuration, mediaPlay.DurationInMs);
            // using this metaRetriever.ExtractMetadata(MetadataKey.Duration))  doesn't work
        }
        else
        {
            builder.PutString(MediaMetadata.MetadataKeyAlbum, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyAlbum))
                   .PutString(MediaMetadata.MetadataKeyArtist, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyArtist))
                   .PutString(MediaMetadata.MetadataKeyTitle, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyTitle))
                   .PutRating(MediaMetadata.MetadataKeyRating, Rating.NewHeartRating(true))
                   .PutLong(MediaMetadata.MetadataKeyDuration, mediaSession.Controller.Metadata.GetLong(MediaMetadata.MetadataKeyDuration));
        }
        builder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, Cover);
        mediaSession.SetMetadata(builder.Build());
    }


    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        HandleIntent(intent);
        base.OnStartCommand(intent, flags, startId);
        return StartCommandResult.Sticky;
    }

    private void HandleIntent(Intent intent)
    {
        if (intent == null || intent.Action == null)
            return;

        string action = intent.Action;
        
        switch (intent.Action)
        {
            case ActionPlay:
                mediaController.GetTransportControls().Play();
                Console.WriteLine("Step 3 Play from Intent");
                break;
            case ActionPause:
                mediaController.GetTransportControls().Pause();
                break;
            case ActionPrevious:
                mediaController.GetTransportControls().SkipToPrevious();
                break;
            case ActionNext:
                mediaController.GetTransportControls().SkipToNext();
                Console.WriteLine("Step 4 Skip To Next From Intent");
                break;
            case ActionSeekTo:
                mediaController.GetTransportControls().SeekTo(Position);
                break;
            case ActionSetRating:
                mediaController.GetTransportControls().SetRating(IsFavorite);
                break;
            default:
                break;
        }

    }

    private void UnregisterMediaSessionCompat()
    {
        try
        {
            if (mediaSession != null)
            {
                mediaSession.Dispose();                
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    IBinder binder;

    public override IBinder OnBind(Intent? intent)
    {
        binder = new MediaPlayerServiceBinder(this);
        return binder;
    }

    public override bool OnUnbind(Intent? intent)
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
            
            
            UnregisterMediaSessionCompat();
        }
    }

    public void OnAudioFocusChange(AudioFocus focusChange)
    {
        Console.WriteLine($"Audio focus: {focusChange}");
        switch (focusChange)
        {
            case AudioFocus.Gain:
                //if (mediaPlayer == null)
                //    InitializePlayer();

                //if (!mediaPlayer.IsPlaying)
                //{
                //    mediaPlayer.Start();
                //}
                //mediaPlayer.SetVolume(1.0f, 1.0f);
                break;
            case AudioFocus.Loss:
                //We have lost focus stop!
                Pause();
                IsPlayingChanged?.Invoke(this, false);
                //Stop();
                break;
            case AudioFocus.LossTransient:
                //We have lost focus for a short time, but likely to resume so pause
                Pause();
                IsPlayingChanged?.Invoke(this, false);
                break;
            case AudioFocus.LossTransientCanDuck:
                //We have lost focus but should till play at a muted 10% volume
                if (mediaPlayer.IsPlaying)
                    mediaPlayer.SetVolume(.1f, .1f);
                break;
        }
    }

    public class MediaSessionCallback : MediaSession.Callback
    {
        HomePageVM MyViewModel { get; set; }
        private readonly MediaPlayerServiceBinder mediaPlayerService;
        public MediaSessionCallback(MediaPlayerServiceBinder service)
        {
            mediaPlayerService = service;
            MyViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        }

        bool isPlaying = true;
        public override void OnPause()
        {
            mediaPlayerService.GetMediaPlayerService().OnPlayingChanged(false);
            base.OnPause();
            MyViewModel.PauseSong();
            isPlaying = false;
        }

        public override void OnPlay()
        {
            Console.WriteLine("Step 2 On Play Callback Method");
            mediaPlayerService.GetMediaPlayerService().OnPlayingChanged(true);
            //base.OnPlay();
            MyViewModel.ResumeSong();
            isPlaying = true;
        }

        public override void OnSkipToNext()
        {
            Console.WriteLine("Step 1 Skip to next Callback Method");
            mediaPlayerService.GetMediaPlayerService().PlayNext();
            base.OnSkipToNext();
        }

        public override void OnSkipToPrevious()
        {
            
            mediaPlayerService.GetMediaPlayerService().PlayPrevious();
            base.OnSkipToPrevious();
        }

        public override void OnStop()
        {
            mediaPlayerService.GetMediaPlayerService().Stop();
            base.OnStop();
        }

        public override void OnSeekTo(long pos)
        {
            mediaPlayerService.GetMediaPlayerService().Seek((int)pos);
            Console.WriteLine("From OnSeek Seeking to " + pos);
            //base.OnSeekTo(pos);
        }

        public override void OnSetRating(Rating rating)
        {
            base.OnSetRating(rating);
        }
        
    }

}

public class MediaPlayerServiceBinder : Binder
{
    private readonly MediaPlayerService service;

    public MediaPlayerServiceBinder(MediaPlayerService service)
    {
        this.service = service;
    }

    public MediaPlayerService GetMediaPlayerService()
    {
        return service;
    }
}