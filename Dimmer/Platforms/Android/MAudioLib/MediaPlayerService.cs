using Android.App;
using Android.Content;
using Android.Media;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Media.Session;
using AndroidNet = Android.Net;
using Android.Graphics;
using Activity = Android.App.Activity;
using Android.Content.PM;

namespace Dimmer_MAUI.Platforms.Android.MAudioLib;

[Service(Enabled = true, Exported = true, ForegroundServiceType =ForegroundService.TypeMediaPlayback)]
[IntentFilter(new[] { ActionPlay, ActionPause, ActionStop, ActionTogglePlayback, ActionNext, ActionPrevious, ActionSeekTo })]
public class MediaPlayerService : Service,
   AudioManager.IOnAudioFocusChangeListener,
   MediaPlayer.IOnBufferingUpdateListener,
   MediaPlayer.IOnCompletionListener,
   MediaPlayer.IOnErrorListener,
   MediaPlayer.IOnPreparedListener
{
    //Actions
    public const string ActionPlay = "com.xamarin.action.PLAY";
    public const string ActionPause = "com.xamarin.action.PAUSE";
    public const string ActionStop = "com.xamarin.action.STOP";
    public const string ActionTogglePlayback = "com.xamarin.action.TOGGLEPLAYBACK";
    public const string ActionNext = "com.xamarin.action.NEXT";
    public const string ActionPrevious = "com.xamarin.action.PREVIOUS";
    public const string ActionSeekTo = "com.xamarin.action.ActionSeekTo";

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

    public MediaPlayerService()
    {
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
        audioManager = (AudioManager)GetSystemService(AudioService);
        wifiManager = (WifiManager)GetSystemService(WifiService);

        remoteComponentName = new ComponentName(PackageName, new RemoteControlBroadcastReceiver().ComponentName);
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
                mediaSession.SetMediaButtonBroadcastReceiver(remoteComponentName);

                var pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, nIntent, PendingIntentFlags.Mutable);

                mediaSession.SetSessionActivity(pendingIntent);

                mediaController = new MediaController(Platform.AppContext, mediaSession.SessionToken);
            }

            mediaSession.Active = true;
            
            mediaSession.SetCallback(new MediaSessionCallback((MediaPlayerServiceBinder)binder));

            mediaSession.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    /// <summary>
    /// Intializes the player.
    /// </summary>
    private void InitializePlayer()
    {
        mediaPlayer = new MediaPlayer();
        mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
            .SetContentType(AudioContentType.Music)
            .SetUsage(AudioUsageKind.Media).Build());

        mediaPlayer.SetWakeMode(Platform.AppContext, WakeLockFlags.Partial);

        mediaPlayer.SetOnBufferingUpdateListener(this);
        mediaPlayer.SetOnCompletionListener(this);
        mediaPlayer.SetOnErrorListener(this);
        mediaPlayer.SetOnPreparedListener(this);
    }


    public void OnBufferingUpdate(MediaPlayer mp, int percent)
    {
        int duration = 0;
        if (MediaPlayerState == PlaybackStateCode.Playing || MediaPlayerState == PlaybackStateCode.Paused)
            duration = mp.Duration;

        int newBufferedTime = duration * percent / 100;
        if (newBufferedTime != Buffered)
        {
            Buffered = newBufferedTime;
        }

        Console.WriteLine("Step 7 on buffering");
    }

    public async void OnCompletion(MediaPlayer mp)
    {
        TaskPlayEnded?.Invoke(this, EventArgs.Empty);
        await PlayNext();
        
    }
    
    public bool OnError(MediaPlayer mp, MediaError what, int extra)
    {
        UpdatePlaybackState(PlaybackStateCode.Error);
        Console.WriteLine("Step 8 error");
        Task.Run(() => Play());
        return true;
    }

    public void OnPrepared(MediaPlayer mp)
    {
        mp.Start();
        UpdatePlaybackState(PlaybackStateCode.Playing);
        Console.WriteLine("Step 9 Prepared");
    }

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

    private int buffered = 0;

    public int Buffered
    {
        get
        {
            if (mediaPlayer == null)
                return 0;
            else
                return buffered;
        }
        private set
        {
            buffered = value;
            OnBuffering(EventArgs.Empty);
        }
    }

    private Bitmap cover;

    public object Cover
    {
        get
        {
            cover ??= BitmapFactory.DecodeResource(Resources, Resource.Drawable.abc_btn_check_material); //TODO player_play
            return cover;
        }
        set
        {
            cover = value as Bitmap;
            if (cover != null)
                OnCoverReloaded(EventArgs.Empty);
        }
    }

    /// <summary>
    /// Intializes the player.
    /// </summary>
    public async Task Play()
    {
        Console.WriteLine("Step 6 Play method from mediaplayerservice");
        if (mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Paused)
        {
            //We are simply paused so just start again
            Console.WriteLine("Not null");
            mediaPlayer.Start();
            UpdatePlaybackState(PlaybackStateCode.Playing);
            StartNotification();

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

        await PrepareAndPlayMediaPlayerAsync();
    }
    
    private async Task PrepareAndPlayMediaPlayerAsync()
    {
        try
        {
            Console.WriteLine("Preparing");
            if (OperatingSystem.IsAndroidVersionAtLeast(21))
            {
                MediaMetadataRetriever metaRetriever = new MediaMetadataRetriever();

                AndroidNet.Uri uri;
                if (mediaPlay.Stream != null)
                {
                    var fileStream = File.Create(FileSystem.Current.CacheDirectory + "temp.wav");
                    mediaPlay.Stream.CopyTo(fileStream);
                    fileStream.Close();
                    uri = AndroidNet.Uri.Parse(FileSystem.Current.CacheDirectory + "temp.wav");
                }
                else
                {   
                    uri = AndroidNet.Uri.Parse(mediaPlay.URL);
                }

                mediaPlayer.Reset();
                mediaPlayer.SetAudioAttributes(new AudioAttributes.Builder()
                    .SetContentType(AudioContentType.Music)
                    .SetUsage(AudioUsageKind.Media).Build());

                mediaPlayer.SetWakeMode(Platform.AppContext, WakeLockFlags.Partial);

                mediaPlayer.SetOnBufferingUpdateListener(this);
                mediaPlayer.SetOnCompletionListener(this);
                mediaPlayer.SetOnErrorListener(this);
                mediaPlayer.SetOnPreparedListener(this);



                await mediaPlayer.SetDataSourceAsync(Platform.AppContext, uri);
                
                //If Uri Scheme is not set its a local file so there's no metadata to fetch
                if (!string.IsNullOrWhiteSpace(uri.Scheme))
                    await metaRetriever.SetDataSourceAsync(uri.ToString(), new Dictionary<string, string>());

                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                {
                    var focusResult = audioManager.RequestAudioFocus(new AudioFocusRequestClass
                    .Builder(AudioFocus.Gain)
                    .SetOnAudioFocusChangeListener(this)
                    .Build());

                    if (focusResult != AudioFocusRequest.Granted)
                    {
                        // Could not get audio focus
                        Console.WriteLine("Could not get audio focus");
                    }
                }

                UpdatePlaybackState(PlaybackStateCode.Buffering);
                triedCount++;
                mediaPlayer.PrepareAsync();
                triedAfterCount++;
                AquireWifiLock();

                if (mediaPlay.ImageBytes is not null)
                {
                    Cover = await GetImageBitmapFromBytesAsync(mediaPlay.ImageBytes);
                }
                else if (metaRetriever != null && !string.IsNullOrWhiteSpace(metaRetriever.ExtractMetadata(MetadataKey.Album)))
                {
                    byte[] imageByteArray = metaRetriever.GetEmbeddedPicture();

                    if (imageByteArray != null)
                        Cover = await BitmapFactory.DecodeByteArrayAsync(imageByteArray, 0, imageByteArray.Length);
                }
                UpdateMediaMetadataCompat(metaRetriever);
                StartNotification();
            }
            
        }
        catch (Exception ex)
        {
            UpdatePlaybackStateStopped();
            failedCount++;
            // Unable to start playback log error
            Console.WriteLine("Error !!!!!!!!!!!! when preparing. Msg: "+ ex.Message 
                +" failedCount = "+failedCount
                +" tried Before Count= " + triedCount
                +" tried after Count= " + triedAfterCount );
            Console.WriteLine($"Is Playing = {mediaPlayer.IsPlaying}");
        }
    }
    int failedCount = 0;
    int triedCount = 0;
    int triedAfterCount = 0;
    private async Task<Bitmap> GetImageBitmapFromUrl(string url)
    {
        Bitmap imageBitmap = null;

        using (var webClient = new HttpClient())
        {
            var imageBytes = await webClient.GetByteArrayAsync(url);
            if (imageBytes != null && imageBytes.Length > 0)
            {
                imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
            }
        }

        return imageBitmap;
    }
    private async Task<Bitmap> GetImageBitmapFromBytesAsync(byte[] imageBytes)
    {
        Bitmap imageBitmap = null;
        if (imageBytes != null && imageBytes.Length > 0)
        {
            imageBitmap = await BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
        }

        return imageBitmap;
    }
    public async Task Seek(int position, PlaybackStateCode playbackStateCode = PlaybackStateCode.Stopped)
    {
        await Task.Run(() =>
        {
            mediaPlayer?.SeekTo(position);
            UpdatePlaybackState(MediaPlayerState, position);
        });
    }

    public async Task PlayNext()
    {
        TaskPlayNext?.Invoke(this, EventArgs.Empty);
        //if (mediaPlayer != null)
        //{
        //    mediaPlayer.Reset();
        //    mediaPlayer.Release();
        //    mediaPlayer = null;
        //}
        UpdatePlaybackState(PlaybackStateCode.SkippingToNext);
        Console.WriteLine("Step 5: TaskPlayNext called");
        //await Play();
    }

    public void PlayPrevious()
    {
        // Start current track from beginning if it's the first track or the track has played more than 3sec and you hit "playPrevious".
        TaskPlayPrevious?.Invoke(this, EventArgs.Empty);
        //if (Position > 3000)
        //{
        //    await Seek(0);
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

        //    await Play();
        //}
    }

    public async Task PlayPause()
    {
        if (mediaPlayer == null || mediaPlayer != null && MediaPlayerState == PlaybackStateCode.Paused)
        {
            await Play();
        }
        else
        {
            await Pause();
        }
    }

    public async Task Pause()
    {
        await Task.Run(() =>
        {
            if (mediaPlayer == null)
                return;

            if (mediaPlayer.IsPlaying)
                mediaPlayer.Pause();

            UpdatePlaybackState(PlaybackStateCode.Paused);
        });
    }

    public async Task Stop()
    {
        await Task.Run(() =>
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
            StopForeground(true);
            ReleaseWifiLock();
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
            mediaPlayer.Reset();
            //mediaPlayer.Release();
            //mediaPlayer = null;
        }
    }

    private void UpdatePlaybackState(PlaybackStateCode state, int SeekedPosition = 0)
    {
        if (mediaSession == null || mediaPlayer == null)
            return;
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
                    PlaybackState.ActionSeekTo
                )
                .SetState(state, SeekedPosition, 1.0f, SystemClock.ElapsedRealtime());
            
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

        var notif = NotificationHelper.StartNotification(Platform.AppContext,mediaController.Metadata,mediaSession,
            Cover,MediaPlayerState == PlaybackStateCode.Playing);

        StartForeground(1000, notif);
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
            .PutLong(MediaMetadata.MetadataKeyDuration, mediaPlay.DurationInMs);
            // using this metaRetriever.ExtractMetadata(MetadataKey.Duration))  doesn't work
        }
        else
        {
            builder.PutString(MediaMetadata.MetadataKeyAlbum, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyAlbum))
                   .PutString(MediaMetadata.MetadataKeyArtist, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyArtist))
                   .PutString(MediaMetadata.MetadataKeyTitle, mediaSession.Controller.Metadata.GetString(MediaMetadata.MetadataKeyTitle))
                   .PutLong(MediaMetadata.MetadataKeyDuration, mediaSession.Controller.Metadata.GetLong(MediaMetadata.MetadataKeyDuration));
        }
        builder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, Cover as Bitmap);
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
            default:
                break;
        }

    }

    /// <summary>
    /// Lock the wifi so we can still stream under lock screen
    /// </summary>
    private void AquireWifiLock()
    {
        if (wifiLock == null)
        {
            wifiLock = wifiManager.CreateWifiLock(WifiMode.Full, "xamarin_wifi_lock");
        }
        wifiLock.Acquire();
    }

    /// <summary>
    /// This will release the wifi lock if it is no longer needed
    /// </summary>
    private void ReleaseWifiLock()
    {
        if (wifiLock == null)
            return;

        wifiLock.Release();
        wifiLock = null;
    }

    private void UnregisterMediaSessionCompat()
    {
        try
        {
            if (mediaSession != null)
            {
                mediaSession.Dispose();
                mediaSession = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    IBinder binder;

    public override IBinder OnBind(Intent intent)
    {
        binder = new MediaPlayerServiceBinder(this);
        return binder;
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
            mediaPlayer = null;

            NotificationHelper.StopNotification(Platform.AppContext);
            StopForeground(true);
            ReleaseWifiLock();
            UnregisterMediaSessionCompat();
        }
    }

    public async void OnAudioFocusChange(AudioFocus focusChange)
    {
        Console.WriteLine($"Audio focus: {focusChange}");
        switch (focusChange)
        {
            case AudioFocus.Gain:
                if (mediaPlayer == null)
                    InitializePlayer();

                if (!mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Start();
                }
                mediaPlayer.SetVolume(1.0f, 1.0f);
                break;
            case AudioFocus.Loss:
                //We have lost focus stop!
                //await Stop();
                break;
            case AudioFocus.LossTransient:
                //We have lost focus for a short time, but likely to resume so pause
                await Pause();
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
        private readonly MediaPlayerServiceBinder mediaPlayerService;
        public MediaSessionCallback(MediaPlayerServiceBinder service)
        {
            mediaPlayerService = service;

        }


        bool isPlaying = true;
        public override async void OnPause()
        {
            mediaPlayerService.GetMediaPlayerService().OnPlayingChanged(false);
            base.OnPause();
            isPlaying = false;
        }

        public override async void OnPlay()
        {
            Console.WriteLine("Step 2 On Play Callback Method");
            mediaPlayerService.GetMediaPlayerService().OnPlayingChanged(true);
            base.OnPlay();
            isPlaying = true;
        }

        public override async void OnSkipToNext()
        {
            Console.WriteLine("Step 1 Skip to next Callback Method");
            await mediaPlayerService.GetMediaPlayerService().PlayNext();            
            base.OnSkipToNext();
        }

        public override void OnSkipToPrevious()
        {
            mediaPlayerService.GetMediaPlayerService().PlayPrevious();
            base.OnSkipToPrevious();
        }

        public override async void OnStop()
        {
            await mediaPlayerService.GetMediaPlayerService().Stop();
            base.OnStop();
        }

        public override async void OnSeekTo(long pos)
        {
            await mediaPlayerService.GetMediaPlayerService().Seek((int)pos);
            base.OnSeekTo(pos);
        }

        public override void OnCustomAction(string action, Bundle extras)
        {
            base.OnCustomAction(action, extras);
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