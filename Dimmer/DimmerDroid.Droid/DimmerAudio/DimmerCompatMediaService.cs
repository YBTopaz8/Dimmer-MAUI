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
    private AudioFocusRequestClass? _audioFocusRequest;
    private double _volumeBeforeDuck = 1.0; // To remember volume before a
    private bool _isDucked = false; 
    private bool _wasPlayingBeforeFocusLoss = false; 
    private MediaSessionCompat? _mediaSession;
    private IDimmerAudioService? _audioService;
    private PowerManager.WakeLock? _wakeLock;
    private NotificationManagerCompat? _notificationManager;
    private AudioManager? _audioManager;
    private AudioBecomingNoisyReceiver? _noisyReceiver;
    // The ONE object that prevents memory leaks
    private readonly CompositeDisposable _disposables = new();
    private Bitmap? _currentCoverArt;
    private bool _isStartedInForeground = false;
    private DimmerPlaybackState _lastState = DimmerPlaybackState.None;

    private void UpdateDurationMetadataOnly(double durationInSeconds)
    {
        var song = _audioService?.CurrentTrackMetadata;
        if (song == null || _mediaSession == null) return;

        var currentMetadata = _mediaSession.Controller?.Metadata;
        var builder = currentMetadata is null
            ? new MediaMetadataCompat.Builder()
            : new MediaMetadataCompat.Builder(currentMetadata);

        builder.PutLong(MediaMetadataCompat.MetadataKeyDuration, (long)(durationInSeconds * 1000));

        _mediaSession.SetMetadata(builder.Build()); 
        
        RedrawNotification();
    }
    
    public override void OnCreate()
    {
        base.OnCreate();

        _audioService = IPlatformApplication.Current!.Services.GetRequiredService<IDimmerAudioService>();
        _audioManager = (AudioManager?)GetSystemService(AudioService);
        _notificationManager = NotificationManagerCompat.From(this);




        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Dimmer::AudioServiceLock");
        _wakeLock?.SetReferenceCounted(false);

        // 1. Headset unplug receiver
        _noisyReceiver = new AudioBecomingNoisyReceiver(_audioService);
        RegisterReceiver(_noisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy));

        // 2. MediaSession setup
        var componentName = new ComponentName(this, Java.Lang.Class.FromType(typeof(DimmerMediaButtonReceiver)));
        _mediaSession = new MediaSessionCompat(this, "DimmerRxSession", componentName, null);
     
        var sessionCallback = new DimmerMediaSessionCallback(_audioService)
        {
            OnOptimisticSeek = targetSec => UpdateSeekPositionOptimistic(targetSec)
        };
        _mediaSession.SetCallback(sessionCallback);


        _mediaSession.Active = true;


        _audioService.FavoriteRequestedObs
            .Subscribe(_ =>
            {
                // Force the notification & media session to refresh the heart icon
                UpdateAndroidPlaybackState(_lastState, _audioService.CurrentPosition);
                RedrawNotification();
            })
            .DisposeWith(_disposables);

        // 3. Rx Bindings (Direct execution, no UI scheduler required)
        _audioService.CurrentSongObs
            .Where(song => song != null)
            .Subscribe(async song =>
            {
                double duration = song!.DurationInSeconds > 0
            ? song.DurationInSeconds
            : (_audioService.CurrentTrackMetadata?.DurationInSeconds ?? 0);
                await LoadCoverArtAndSetMetadataAsync(song!, duration);
            })
            .DisposeWith(_disposables);

        _audioService.DurationObs
            .Where(duration => duration > 0)
                .Subscribe(duration => UpdateDurationMetadataOnly(duration))
            .DisposeWith(_disposables);

        _audioService.PlaybackStateObs
    .DistinctUntilChanged()
    .Subscribe(state =>
    {
        _lastState = state;
        UpdateAndroidPlaybackState(state, _audioService.CurrentPosition);
        RedrawNotification(); // Only redraw icons on Play/Pause/Stop!

        if (state == DimmerPlaybackState.Playing)
            RequestAudioFocus();
        else if (state == DimmerPlaybackState.PausedUser || state == DimmerPlaybackState.PlayCompleted)
            _audioManager?.AbandonAudioFocus(this);
    })
    .DisposeWith(_disposables);
    }

    
    private void RequestAudioFocus()
    {
        if (_audioManager == null) return;

        int focusResult;

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            // Modern Android 8.0+ Focus Request
            if (_audioFocusRequest == null)
            {
                _audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                    .SetAudioAttributes(new AudioAttributes.Builder()?
                        .SetUsage(AudioUsageKind.Media)?
                        .SetContentType(AudioContentType.Music)?
                        .Build()!)
                    .SetAcceptsDelayedFocusGain(true)
                    .SetOnAudioFocusChangeListener(this)
                    .Build();
            }
            focusResult = (int)_audioManager.RequestAudioFocus(_audioFocusRequest);
        }
        else
        {
            // Legacy Android
            focusResult = (int)_audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
        }

        if (focusResult != (int)AudioFocusRequest.Granted)
        {
            _ = _audioService?.PauseAsync();
        }
    }

    public void OnAudioFocusChange(AudioFocus focusChange)
    {
        switch (focusChange)
        {
            case AudioFocus.Loss:
                // Another app (like YouTube) started playing. Stop completely.
                _wasPlayingBeforeFocusLoss = false;

                _ = _audioService?.PauseAsync();
                break;

            case AudioFocus.LossTransient:
                // A phone call or WhatsApp audio is playing. Pause temporarily.
                _wasPlayingBeforeFocusLoss = _audioService?.IsPlaying ?? false;
                _ = _audioService?.PauseAsync();
                break;

            case AudioFocus.LossTransientCanDuck:
                if (_audioService is OwnAudioService srv && !_isDucked)
                {
                    srv.SetDucking(true);
                    _isDucked = true;
                }
                break;

            case AudioFocus.Gain:
                
                if (_audioService != null)
                {
                    if (_isDucked && _audioService is OwnAudioService srvc)
                    {
                        srvc.SetDucking(false);
                        _isDucked = false;
                    }

                    // Only resume if WE were the ones playing before the interruption
                    if (_wasPlayingBeforeFocusLoss && !_audioService.IsPlaying)
                    {
                        _ = _audioService.PlayAsync();
                    }
                }
                break;
        }
    }
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
      
        if (!_isStartedInForeground)
        {
            PromoteToForeground();
        }

        if (intent != null)
        {
            if (intent.Action == DimmerMediaSessionCallback.ActionFavorite)
            {
                if (_audioService is OwnAudioService srv)
                { 
                    srv.TriggerFavorite();
                    UpdateAndroidPlaybackState(_lastState, _audioService.CurrentPosition);
                    RedrawNotification();
                }
            }
            else
            {
                MediaButtonReceiver.HandleIntent(_mediaSession, intent);
            }
        }
        return StartCommandResult.Sticky;
    }
    private async Task LoadCoverArtAndSetMetadataAsync(SongModelView song, double durationInSeconds)
    {
       
        Bitmap? newCoverArt = null;
        // If you have local file paths for images:
        if (!string.IsNullOrEmpty(song.CoverImagePath) && File.Exists(song.CoverImagePath))
        {
            newCoverArt = await Task.Run(() =>
            {
                var options = new BitmapFactory.Options { InJustDecodeBounds = true };
                BitmapFactory.DecodeFile(song.CoverImagePath, options);

                // Calculate downsample ratio
                options.InSampleSize = CalculateInSampleSize(options, 256, 256);
                options.InJustDecodeBounds = false;

                return BitmapFactory.DecodeFile(song.CoverImagePath, options);
            });
        }
        if (_currentCoverArt != null)
        {
            _currentCoverArt.Dispose();
        }
        _currentCoverArt = newCoverArt;

        long durationMs = (long)(durationInSeconds * 1000);
        if (durationMs <= 0)
        {
            var existingDuration = _mediaSession?.Controller?.Metadata?.GetLong(MediaMetadataCompat.MetadataKeyDuration) ?? 0;
            if (existingDuration > 0)
            {
                durationMs = existingDuration;
            }
        }


        var builder = new MediaMetadataCompat.Builder()?
            .PutString(MediaMetadataCompat.MetadataKeyTitle, song.Title)?
            .PutString(MediaMetadataCompat.MetadataKeyArtist, song.ArtistName)?
            .PutString(MediaMetadataCompat.MetadataKeyAlbum, song.AlbumName);
        if (durationMs > 0)
        {
            builder?.PutLong(MediaMetadataCompat.MetadataKeyDuration, durationMs);
        }

        if (_currentCoverArt != null)
            builder?.PutBitmap(MediaMetadataCompat.MetadataKeyAlbumArt, _currentCoverArt);

        _mediaSession!.SetMetadata(builder?.Build());
        RedrawNotification();
    }

    public void UpdateSeekPositionOptimistic(double targetSec)
    {
        if (_mediaSession == null) return;

        // Send the user's tapped position with speed = 0 while scrubbing/seeking
        var state = _mediaSession.Controller?.PlaybackState?.State ?? PlaybackStateCompat.StatePlaying;

        var stateBuilder = new PlaybackStateCompat.Builder(_mediaSession.Controller?.PlaybackState)
            .SetState(state, (long)(targetSec * 1000), 1.0f);

        _mediaSession.SetPlaybackState(stateBuilder?.Build());
    }

    private void PromoteToForeground()
    {
        if (_isStartedInForeground) return;
        var notification = NotificationHelper.BuildNotification(
            this,
            _mediaSession!,
            _audioService?.IsPlaying ?? false,
            _audioService?.CurrentTrackMetadata,
            _currentCoverArt);
        if (notification == null) return;


        try
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                StartForeground(NotificationHelper.NotificationId, notification, global::Android.Content.PM.ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationHelper.NotificationId, notification);

            _isStartedInForeground = true;
        }
        catch (Android.App.ForegroundServiceStartNotAllowedException ex)
        {
            // Android 12+ prevents background apps from starting foreground services.
            //  gracefully catch this. The MediaSession will still exist to wake the app.
            System.Diagnostics.Debug.WriteLine($"[AudioService] Cannot promote to foreground from background: {ex.Message}");
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
        var isFav = _audioService?.CurrentTrackMetadata?.IsFavorite ?? false;
        var favIcon = isFav ? Resource.Drawable.media3_icon_heart_filled : Resource.Drawable.media3_icon_heart_unfilled;
        var favCustomAction = new PlaybackStateCompat.CustomAction.Builder(
      DimmerMediaSessionCallback.ActionFavorite,
      "Favorite",
      favIcon).Build();
        var stateBuilder = new PlaybackStateCompat.Builder()?
            .SetActions(PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionSkipToNext |
                        PlaybackStateCompat.ActionSkipToPrevious |
                        PlaybackStateCompat.ActionSeekTo)?
        .AddCustomAction(favCustomAction)?
            .SetState(compatState, (long)(positionSec * 1000), playbackSpeed);

        _mediaSession.SetPlaybackState(stateBuilder?.Build());
    }

    private void RedrawNotification()
    {
        
        RxSchedulers.UI.ScheduleTo(() =>
        {
           
            var isPlaying = _audioService?.IsPlaying ?? false;
            var currentSong = _audioService?.CurrentTrackMetadata;

            var notification = NotificationHelper.BuildNotification(this, _mediaSession!, isPlaying, currentSong, _currentCoverArt);
            if (notification is null) return;

            if (_isStartedInForeground)
            {
                _notificationManager?.Notify(NotificationHelper.NotificationId, notification);
            }
            else
            {
                // Only promote if we haven't started foreground yet
                PromoteToForeground();
            }
            _notificationManager?.Notify(NotificationHelper.NotificationId, notification);

            if (isPlaying)
            {
                if (_wakeLock?.IsHeld == false) _wakeLock.Acquire();
            }
            else
            {
                if (_wakeLock?.IsHeld == true) _wakeLock.Release();
              
               
            }


        });
    }

    public override void OnDestroy()
    {
        // CLEANUP: Zero Memory Leaks!
        _disposables.Dispose();
        if (_noisyReceiver != null) UnregisterReceiver(_noisyReceiver);
        _audioManager?.AbandonAudioFocus(this);


        _currentCoverArt?.Dispose();
        _mediaSession?.Release();

        if (_wakeLock?.IsHeld == true) _wakeLock.Release();
        StopForeground(StopForegroundFlags.Remove);
        _isStartedInForeground = false;
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;
}
