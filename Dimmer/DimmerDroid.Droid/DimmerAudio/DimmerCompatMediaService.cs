#pragma warning disable CS0618
#pragma warning disable CA1422
namespace Dimmer.DimmerAudio;

using Android.Graphics;
using Android.Media;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using AndroidX.Media.Session;
using Avalonia.Controls.Notifications;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

[Service(Name = "com.yvanbrunel.dimmer.DimmerCompatMediaService", Exported = true, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public partial class DimmerCompatMediaService : Service, AudioManager.IOnAudioFocusChangeListener
{
    private MediaSessionCompat? _mediaSession;
    private IDimmerAudioService? _audioService;
    private PowerManager.WakeLock? _wakeLock;
    private NotificationManagerCompat? _notificationManager;
    private AudioManager? _audioManager;
    private AudioBecomingNoisyReceiver? _noisyReceiver;
    // The ONE object that prevents memory leaks
    private readonly CompositeDisposable _disposables = new();
    private DimmerAudioDeviceCallback? _deviceCallback;
    private Bitmap? _currentCoverArt;
    private bool _isStartedInForeground = false;
    private DimmerPlaybackState _lastState = DimmerPlaybackState.None;

    public override void OnCreate()
    {
        base.OnCreate();

        _audioService = IPlatformApplication.Current!.Services.GetRequiredService<IDimmerAudioService>();
        _audioManager = (AudioManager?)GetSystemService(AudioService);
        _notificationManager = NotificationManagerCompat.From(this);



        _deviceCallback = new DimmerAudioDeviceCallback(_audioService);
        _audioManager?.RegisterAudioDeviceCallback(_deviceCallback, null);


        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Dimmer::AudioServiceLock");
        _wakeLock?.SetReferenceCounted(false);

        // 1. Headset unplug receiver
        _noisyReceiver = new AudioBecomingNoisyReceiver(_audioService);
        RegisterReceiver(_noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));

        // 2. MediaSession setup
        var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(DimmerMediaButtonReceiver)));
        _mediaSession = new MediaSessionCompat(this, "DimmerRxSession", componentName, null);
        _mediaSession.SetCallback(new DimmerMediaSessionCallback(_audioService));
        _mediaSession.Active = true;

        // 3. Rx Bindings (Direct execution, no UI scheduler required)
        _audioService.CurrentSongObs
            .Where(song => song != null)
            .Subscribe(song => LoadCoverArtAndSetMetadata(song!, 0))
            .DisposeWith(_disposables);

        _audioService.DurationObs
            .Where(duration => duration > 0)
            .Subscribe(duration =>
            {
                if (_audioService.CurrentTrackMetadata != null)
                    LoadCoverArtAndSetMetadata(_audioService.CurrentTrackMetadata, duration);
            })
            .DisposeWith(_disposables);

        _audioService.PlaybackStateObs
            .CombineLatest(_audioService.PositionObs, (state, pos) => new { state, pos })
            .Sample(TimeSpan.FromMilliseconds(250))
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
        // 🔥 FIX 1: Promote to foreground IMMEDIATELY to satisfy Android's 5-second watchdog timer
        if (!_isStartedInForeground)
        {
            PromoteToForeground();
        }

        if (intent != null)
        {
            if (intent.Action == DimmerMediaSessionCallback.ActionFavorite)
            {
                if (_audioService is OwnAudioService srv) srv.TriggerFavorite();
            }
            else
            {
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

    private void PromoteToForeground()
    {
        var notification = NotificationHelper.BuildNotification(
            this,
            _mediaSession!,
            _audioService?.IsPlaying ?? false,
            _audioService?.CurrentTrackMetadata,
            _currentCoverArt);

        if (notification != null)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationHelper.NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationHelper.NotificationId, notification);

            _isStartedInForeground = true;
        }
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

            if (!_isStartedInForeground)
            {
                PromoteToForeground();
                return;
            }

            _notificationManager?.Notify(NotificationHelper.NotificationId, notification);

            if (isPlaying)
            {
                if (_wakeLock?.IsHeld == false) _wakeLock.Acquire();
            }
            else
            {
                if (_wakeLock?.IsHeld == true) _wakeLock.Release();
                // DO NOT call StopForeground(Detach) here. Keep the service alive while paused!
            }


        });
    }

    public override void OnDestroy()
    {
        // CLEANUP: Zero Memory Leaks!
        _disposables.Dispose();
        if (_noisyReceiver != null) UnregisterReceiver(_noisyReceiver);
        _audioManager?.AbandonAudioFocus(this);

        if (_deviceCallback != null)
        {
            _audioManager?.UnregisterAudioDeviceCallback(_deviceCallback);
        }

        _currentCoverArt?.Dispose();
        _mediaSession?.Release();

        if (_wakeLock?.IsHeld == true) _wakeLock.Release();
        StopForeground(StopForegroundFlags.Remove);
        _isStartedInForeground = false;
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;
}
