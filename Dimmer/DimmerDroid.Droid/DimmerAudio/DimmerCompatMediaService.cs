#pragma warning disable CS0618
#pragma warning disable CA1422
namespace Dimmer.DimmerAudio;

using Android.Graphics;
using Android.Media;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Media.Session;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

[Service(Name = "com.yvanbrunel.dimmer.DimmerCompatMediaService", Exported = true, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public partial class DimmerCompatMediaService : Service, AudioManager.IOnAudioFocusChangeListener
{
    private MediaSessionCompat? _mediaSession;
    private IDimmerAudioService? _audioService;
    private PowerManager.WakeLock? _wakeLock;

    private AudioManager? _audioManager;
    private AudioBecomingNoisyReceiver? _noisyReceiver;
    // The ONE object that prevents memory leaks
    private readonly CompositeDisposable _disposables = new();

    private Bitmap? _currentCoverArt;

    private DimmerPlaybackState _lastState = DimmerPlaybackState.None;
    public override void OnCreate()
    {
        base.OnCreate();

        // 1. Fetch exactly the same instance your ViewModels use via DI
        _audioService = IPlatformApplication.Current!.Services.GetRequiredService<IDimmerAudioService>();
        _audioManager = (AudioManager?)GetSystemService(AudioService);

        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Dimmer::AudioServiceLock");
        _wakeLock?.SetReferenceCounted(false);


        // 1. Setup Headset Unplug Receiver
        _noisyReceiver = new AudioBecomingNoisyReceiver(_audioService);
        RegisterReceiver(_noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));


        // 2. Initialize MediaSessionCompat
        var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(DimmerMediaButtonReceiver)));


        _mediaSession = new MediaSessionCompat(this, "DimmerRxSession", componentName, null);
        
      
        _mediaSession.SetCallback(new DimmerMediaSessionCallback(_audioService));
        _mediaSession.Active = true;

        // ==========================================
        // 3. PURE RX.NET BINDINGS
        // ==========================================

        // A) Observe Song Changes (Updates Metadata & Cover Art)
        _audioService.CurrentSongObs
            .Where(song => song != null)
            .Subscribe(song =>
            {
                LoadCoverArtAndSetMetadata(song!,0);
            })
            .DisposeWith(_disposables);

        _audioService.DurationObs
    .Where(duration => duration > 0) // Only fire when the Rust engine finds the actual length
    .Subscribe(duration =>
    {
        if (_audioService.CurrentTrackMetadata != null)
        {
            // Re-push the metadata, but this time with the real duration!
            LoadCoverArtAndSetMetadata(_audioService.CurrentTrackMetadata, duration);
        }
    })
    .DisposeWith(_disposables);

        // B) Observe Playback State (Combine State + Position)
        // We only push updates to Android when State changes or when user seeks, 
        // to avoid spamming the UI thread every 16ms. Android extrapolates the seekbar automatically!
        _audioService.PlaybackStateObs
            .CombineLatest(_audioService.PositionObs, (state, pos) => new { state, pos })
            .Sample(TimeSpan.FromMilliseconds(200)) // Throttle slightly
            .Subscribe(x =>
            {
                _lastState = x.state;
                UpdateAndroidPlaybackState(x.state, x.pos);
                if (x.state == DimmerPlaybackState.Playing)
                    RequestAudioFocus();
            })
            .DisposeWith(_disposables);
    }
    private void RequestAudioFocus()
    {
        if (_audioManager == null) return;
        var focusResult = _audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
        if (focusResult != AudioFocusRequest.Granted)
        {
            _ = _audioService?.PauseAsync();
        }
    }

    public void OnAudioFocusChange(AudioFocus focusChange)
    {
        switch (focusChange)
        {
            case AudioFocus.Loss:
            case AudioFocus.LossTransient:
                _ = _audioService?.PauseAsync(); // Pause for phone calls/youtube
                break;
        }
    }


    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent != null)
        {
            // Route Custom Intents (Like our Favorite button)
            if (intent.Action == DimmerMediaSessionCallback.ActionFavorite)
            {
                if (_audioService is OwnAudioService srv) srv.TriggerFavorite();
            }
            else
            {
                // Route Standard Media Intents
                MediaButtonReceiver.HandleIntent(_mediaSession, intent);
            }
        }
        return StartCommandResult.Sticky;
    }

    private void LoadCoverArtAndSetMetadata(SongModelView song, double durationInSeconds)
    {
        // If you have local file paths for images:
        if (!string.IsNullOrEmpty(song.CoverImagePath) && File.Exists(song.CoverImagePath))
        {
            var options = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(song.CoverImagePath, options);

            // Calculate downsample ratio
            options.InSampleSize = CalculateInSampleSize(options, 256, 256);
            options.InJustDecodeBounds = false;

            _currentCoverArt = BitmapFactory.DecodeFile(song.CoverImagePath, options);
        }
        else
        {
            _currentCoverArt = null;
        }

        var builder = new MediaMetadataCompat.Builder()?
            .PutString(MediaMetadataCompat.MetadataKeyTitle, song.Title)?
            .PutString(MediaMetadataCompat.MetadataKeyArtist, song.ArtistName)?
            .PutString(MediaMetadataCompat.MetadataKeyAlbum, song.AlbumName)?
        .PutLong(MediaMetadataCompat.MetadataKeyDuration, (long)(durationInSeconds * 1000));

        if (_currentCoverArt != null)
            builder?.PutBitmap(MediaMetadataCompat.MetadataKeyAlbumArt, _currentCoverArt);

        _mediaSession!.SetMetadata(builder?.Build());
        RedrawNotification();
    }
    public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
    {
        // Raw height and width of the image
        int height = options.OutHeight;
        int width = options.OutWidth;
        int inSampleSize = 1;

        if (height > reqHeight || width > reqWidth)
        {
            int halfHeight = height / 2;
            int halfWidth = width / 2;

            // Keep halving until we drop below the requested size
            while ((halfHeight / inSampleSize) >= reqHeight
                && (halfWidth / inSampleSize) >= reqWidth)
            {
                inSampleSize *= 2;
            }
        }

        return inSampleSize;
    }
    private void UpdateAndroidPlaybackState(DimmerPlaybackState state, double positionSec)
    {
        if (_mediaSession == null) return;

        int compatState = state switch
        {
            DimmerPlaybackState.Playing => PlaybackStateCompat.StatePlaying,
            DimmerPlaybackState.Opening => PlaybackStateCompat.StateBuffering,
            _ => PlaybackStateCompat.StatePaused
        };

        // 1.0f means Android will animate the seekbar automatically while playing!
        float playbackSpeed = compatState == PlaybackStateCompat.StatePlaying ? 1.0f : 0f;

        var stateBuilder = new PlaybackStateCompat.Builder()?
            .SetActions(PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionSkipToNext |
                        PlaybackStateCompat.ActionSkipToPrevious |
                        PlaybackStateCompat.ActionSeekTo)?
            .SetState(compatState, (long)(positionSec * 1000), playbackSpeed);

        _mediaSession.SetPlaybackState(stateBuilder?.Build());
        RedrawNotification();
    }

    private void RedrawNotification()
    {
        RxSchedulers.UI.ScheduleTo(() =>
        {
           
            var isPlaying = _audioService?.IsPlaying ?? false;
            var currentSong = _audioService?.CurrentTrackMetadata;

            var notification = NotificationHelper.BuildNotification(this, _mediaSession!, isPlaying, currentSong, _currentCoverArt);
            if (notification is null) return;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationHelper.NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationHelper.NotificationId, notification);

            bool isTransitioning = _lastState == DimmerPlaybackState.PlayCompleted ||
                                  _lastState == DimmerPlaybackState.Opening;

            if (isPlaying || isTransitioning)
            {
                // Lock the CPU so it doesn't sleep while we fetch the next track
                if (_wakeLock?.IsHeld == false) _wakeLock.Acquire();
            }
            else
            {
                // The user EXPLICITLY paused. It is safe to drop foreground and sleep.
                if (_wakeLock?.IsHeld == true) _wakeLock.Release();
                StopForeground(StopForegroundFlags.Detach);
            }

        });
    }

    public override void OnDestroy()
    {
        // CLEANUP: Zero Memory Leaks!
        _disposables.Dispose();

        _currentCoverArt?.Dispose();
        _mediaSession?.Release();

        if (_wakeLock?.IsHeld == true) _wakeLock.Release();
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;
}
