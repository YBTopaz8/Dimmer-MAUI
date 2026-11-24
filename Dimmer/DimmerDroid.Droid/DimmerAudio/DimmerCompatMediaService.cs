#pragma warning disable CS0618
#pragma warning disable CA1422
namespace Dimmer.DimmerAudio;

using Android.Graphics;
using Android.Media;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using AndroidX.Media.Session;
using Ownaudio.Safe;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

[Service(Name = "com.yvanbrunel.dimmer.DimmerCompatMediaService", Exported = true, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public partial class DimmerCompatMediaService : Service, AudioManager.IOnAudioFocusChangeListener
{
    public const string ActionPlayPause = "com.yvanbrunel.dimmer.ACTION_PLAY_PAUSE";
    public const string ActionNext = "com.yvanbrunel.dimmer.ACTION_NEXT";
    public const string ActionPrev = "com.yvanbrunel.dimmer.ACTION_PREV";
    public const string ActionJumpQueue = "com.yvanbrunel.dimmer.ACTION_JUMP_QUEUE";
    public const string ExtraQueueIndex = "extra_queue_index";
    private readonly Android.OS.Handler _mainHandler = new(Android.OS.Looper.MainLooper!);
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
    static string[] GetTypeKeywords(AudioDeviceType type) => type switch
    {
        AudioDeviceType.WiredHeadset or AudioDeviceType.WiredHeadphones
            => new[] { "(Headset)", "(Headphone", "Headset", "Headphone" },
        AudioDeviceType.BluetoothA2dp or AudioDeviceType.BluetoothSco or AudioDeviceType.BleHeadset
            => new[] { "(Bluetooth", "Bluetooth", "A2DP" },
        AudioDeviceType.UsbHeadset or AudioDeviceType.UsbDevice
            => new[] { "(USB", "USB" },
        AudioDeviceType.BuiltinSpeaker
            => new[] { "(Speaker)", "Speaker" },
        AudioDeviceType.BuiltinEarpiece
            => new[] { "(Earpiece)", "Earpiece" },
        _ => Array.Empty<string>()
    }; public override void OnCreate()
    {
        base.OnCreate();

        _audioService = IPlatformApplication.Current!.Services.GetRequiredService<IDimmerAudioService>();
        _audioManager = (AudioManager?)GetSystemService(AudioService);
        _notificationManager = NotificationManagerCompat.From(this);

      


        var powerManager = (PowerManager)GetSystemService(PowerService)!;
        _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "Dimmer::AudioServiceLock");
        _wakeLock?.SetReferenceCounted(false);

        NotificationHelper.CreateNotificationChannel(this);
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
            .Select(song => Observable.FromAsync(ct => LoadCoverArtAndSetMetadataAsync(song!, ct)))
            .Switch() // Cancels stale cover decodes if user skips rapidly
            .Subscribe(_ => { }, ex => System.Diagnostics.Debug.WriteLine($"Metadata error: {ex}"))
            .DisposeWith(_disposables);
        
        _audioService.DurationObs
             .Where(duration => duration > 0)
             .Subscribe(duration =>
             {
                 UpdateDurationMetadataOnly(duration);
                 UpdateAndroidPlaybackState(_lastState, _audioService.CurrentPosition);
             })
             .DisposeWith(_disposables);

        _audioService.PlaybackStateObs
             .DistinctUntilChanged()
             .Subscribe(state =>
             {
                 _lastState = state;
                 UpdateAndroidPlaybackState(state, _audioService.CurrentPosition);
                 RedrawNotification();

                 if (state == DimmerPlaybackState.Playing)
                     RequestAudioFocus();
                 else if (state is DimmerPlaybackState.PausedUser or DimmerPlaybackState.PlayCompleted)
                     AbandonFocus();
             })
             .DisposeWith(_disposables);

        _audioService.SeekCompletedObs
           .Subscribe(pos => UpdateAndroidPlaybackState(_lastState, pos))
           .DisposeWith(_disposables);

        if (_audioService is OwnAudioService ownAudioSvc)
        {
            ownAudioSvc.SpeedObs
                .Subscribe(_ => UpdateAndroidPlaybackState(_lastState, _audioService.CurrentPosition))
                .DisposeWith(_disposables);
        }

        // 1s heartbeat for MediaSession timeline (does NOT call RedrawNotification)
        _audioService.PositionObs
            .Sample(TimeSpan.FromSeconds(1))
            .Where(_ => _lastState == DimmerPlaybackState.Playing)
            .Subscribe(pos => UpdateAndroidPlaybackState(_lastState, pos))
            .DisposeWith(_disposables);

        _audioDeviceCallback = new DimmerAudioDeviceCallback(
      onAdded: added =>
      {
          var externalDevice = added.FirstOrDefault(IsExternalOutputDevice);
          if (externalDevice == null) return;

          _routeSwitchCts?.Cancel();
          _routeSwitchCts = new CancellationTokenSource();
          var token = _routeSwitchCts.Token;

          _ = Task.Run(async () =>
          {
              try
              {
                  await Task.Delay(350, token);
                  if (token.IsCancellationRequested || _audioService is not OwnAudioService ownAudio)
                      return;

                  var devices = await ownAudio.GetAllAudioDevicesAsync();
                  if (devices == null) return;

                  var keywords = GetTypeKeywords(externalDevice.Type);
                  var productName = externalDevice.ProductName?.ToString();

                  // 1. Best: matches ProductName AND device-type suffix (e.g. "25078RA3EA (Headset)")
                  var matched = devices.FirstOrDefault(d =>
                  {
                      return !string.IsNullOrEmpty(productName) &&
                                            d.Id.Contains(productName, StringComparison.OrdinalIgnoreCase) &&
                                            keywords.Any(k => d.Id.Contains(k, StringComparison.OrdinalIgnoreCase));
                  });

                  // 2. Fallback: match by type suffix (e.g. "(Headset)", "(Bluetooth)")
                  matched ??= devices.FirstOrDefault(d =>
                      keywords.Any(k =>
                      {
                          return d.Id.Contains(k, StringComparison.OrdinalIgnoreCase);
                      }));

                  // 3. Fallback: match by ProductName alone (useful for named BT/USB DACs)
                  matched ??= devices.FirstOrDefault(d =>
                  {
                      return !string.IsNullOrEmpty(productName) &&
                                            d.Id.Contains(productName, StringComparison.OrdinalIgnoreCase);
                  });

                  System.Diagnostics.Debug.WriteLine($"[AudioService] Plugged {externalDevice.Type} ({productName}) -> OwnAudio targetId: {matched?.Id ?? "<null>"}");
                  await ownAudio.SwitchOutputDeviceAsync(matched?.Id);
              }
              catch (System.OperationCanceledException) { }
              catch (Exception ex)
              {
                  System.Diagnostics.Debug.WriteLine($"[AudioService] Auto route switch failed: {ex.Message}");
              }
          });
      },
      onRemoved: removed =>
      {
          if (!removed.Any(IsExternalOutputDevice)) return;

          _routeSwitchCts?.Cancel();
          _routeSwitchCts = new CancellationTokenSource();
          var token = _routeSwitchCts.Token;

          _ = Task.Run(async () =>
          {
              try
              {
                  await Task.Delay(300, token);
                  if (token.IsCancellationRequested || _audioService is not OwnAudioService ownAudio)
                      return;

                  var devices = await ownAudio.GetAllAudioDevicesAsync();
                  if (devices == null) return;

                  // Check if another external output (e.g., BT while wired unplugged, or vice versa) is still connected
                  var remainingExternal = _audioManager?
                      .GetDevices(GetDevicesTargets.Outputs)?
                      .FirstOrDefault(IsExternalOutputDevice);

                  string? targetId = null;

                  if (remainingExternal != null)
                  {
                      var keywords = GetTypeKeywords(remainingExternal.Type);
                      var productName = remainingExternal.ProductName?.ToString();
                      targetId = devices.FirstOrDefault(d =>
                          !string.IsNullOrEmpty(productName) &&
                          d.Id.Contains(productName, StringComparison.OrdinalIgnoreCase) &&
                          keywords.Any(k => d.Id.Contains(k, StringComparison.OrdinalIgnoreCase)))?.Id
                          ?? devices.FirstOrDefault(d =>
                              keywords.Any(k => d.Id.Contains(k, StringComparison.OrdinalIgnoreCase)))?.Id;
                  }
                  else
                  {
                      // Explicitly pick "(Speaker)" so we don't fall back to Earpiece or leave a dead AAudio stream
                      targetId = devices.FirstOrDefault(d =>
                          d.Id.Contains("(Speaker)", StringComparison.OrdinalIgnoreCase))?.Id;
                  }

                  System.Diagnostics.Debug.WriteLine($"[AudioService] Unplugged -> OwnAudio fallback targetId: {targetId ?? "<null>"}");
                  await ownAudio.SwitchOutputDeviceAsync(targetId);
              }
              catch (System.OperationCanceledException) { }
              catch (Exception ex)
              {
                  System.Diagnostics.Debug.WriteLine($"[AudioService] Device removal sync failed: {ex.Message}");
              }
          });
      });
        _audioManager?.RegisterAudioDeviceCallback(_audioDeviceCallback, null);
    }

    private void AbandonFocus()
    {
        if (_audioManager == null) return;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && _audioFocusRequest != null)
            _audioManager.AbandonAudioFocusRequest(_audioFocusRequest);
        else
            _audioManager.AbandonAudioFocus(this);
    }
    private void RequestAudioFocus()
    {
        if (_audioManager == null) return;

        int focusResult=0;

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
            if (_audioFocusRequest is not null)
            {

                focusResult = (int)_audioManager.RequestAudioFocus(_audioFocusRequest);
            }
        }
        else
        {
            // Legacy Android
            focusResult = (int)_audioManager.RequestAudioFocus(this, Stream.Music, AudioFocus.Gain);
        }

        if (focusResult == (int)AudioFocusRequest.Delayed)
        {
            _wasPlayingBeforeFocusLoss = true;
            _ = _audioService?.PauseAsync();
        }
        else if (focusResult != (int)AudioFocusRequest.Granted)
        {
            _wasPlayingBeforeFocusLoss = false;
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
            PromoteToForeground();

        if (intent?.Action != null)
        {
            // Keep CPU awake long enough for OwnAudio / AAudio to reopen and start streaming
            _wakeLock?.Acquire(10_000);

            switch (intent.Action)
            {
                case DimmerMediaSessionCallback.ActionFavorite:
                    if (_audioService is OwnAudioService srv)
                    {
                        srv.TriggerFavorite();
                        UpdateAndroidPlaybackState(_lastState, _audioService.CurrentPosition);
                        RedrawNotification();
                    }
                    break;

                case ActionPlayPause:
                    _ = SafeRunAudioActionAsync(async () =>
                    {
                        if (_audioService == null) return;
                        if (_audioService.IsPlaying)
                            await _audioService.PauseAsync();
                        else
                        {
                            RequestAudioFocus();
                            await _audioService.PlayAsync();
                        }
                    });
                    break;

                case ActionNext:
                    _audioService?.TriggerNext();
                    break;

                case ActionPrev:

                    _audioService?.TriggerPrevious();
                    
                    break;

                case ActionJumpQueue:
                    int idx = intent.GetIntExtra(ExtraQueueIndex, -1);
                    if (idx >= 0)
                    {
                        // Hook into your queue service / ViewModel method to play item at `idx`
                        //_ = SafeRunAudioActionAsync(() => PlayQueueIndexAsync(idx));
                    }
                    break;

                default:
                    MediaButtonReceiver.HandleIntent(_mediaSession, intent);
                    break;
            }
        }
        return StartCommandResult.Sticky;
    }
    private static async Task SafeRunAudioActionAsync(Func<Task> action)
    {
        try { await action().ConfigureAwait(false); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MediaService] Action failed: {ex}"); }
    }
 

    private async Task LoadCoverArtAndSetMetadataAsync(SongModelView song, CancellationToken ct)
    {
        Bitmap? newCoverArt = null;
        if (!string.IsNullOrEmpty(song.CoverImagePath) && File.Exists(song.CoverImagePath))
        {
            newCoverArt = await Task.Run(() =>
            {
                if (ct.IsCancellationRequested) return null;
                var options = new BitmapFactory.Options { InJustDecodeBounds = true };
                BitmapFactory.DecodeFile(song.CoverImagePath, options);

                options.InSampleSize = CalculateInSampleSize(options, 256, 256);
                options.InJustDecodeBounds = false;

                return BitmapFactory.DecodeFile(song.CoverImagePath, options);
            }, ct);
        }

        if (ct.IsCancellationRequested)
        {
            newCoverArt?.Dispose();
            return;
        }

        double durationInSeconds = song.DurationInSeconds > 0
            ? song.DurationInSeconds
            : _audioService?.Duration ?? 0;

        long durationMs = (long)(durationInSeconds * 1000);
        if (durationMs <= 0)
        {
            var existingDuration = _mediaSession?.Controller?.Metadata?.GetLong(MediaMetadataCompat.MetadataKeyDuration) ?? 0;
            if (existingDuration > 0) durationMs = existingDuration;
        }

        _mainHandler.Post(()=>
        {
            if (ct.IsCancellationRequested || _mediaSession == null)
            {
                newCoverArt?.Dispose();
                return;
            }

            var oldArt = _currentCoverArt;
            _currentCoverArt = newCoverArt;

            var builder = new MediaMetadataCompat.Builder()?
                .PutString(MediaMetadataCompat.MetadataKeyTitle, song.Title)?
                .PutString(MediaMetadataCompat.MetadataKeyArtist, song.ArtistName)?
                .PutString(MediaMetadataCompat.MetadataKeyAlbum, song.AlbumName);

            if (durationMs > 0)
                builder?.PutLong(MediaMetadataCompat.MetadataKeyDuration, durationMs);

            if (_currentCoverArt != null)
                builder?.PutBitmap(MediaMetadataCompat.MetadataKeyAlbumArt, _currentCoverArt);

            _mediaSession.SetMetadata(builder?.Build());
            UpdateAndroidPlaybackState(_lastState, _audioService?.CurrentPosition ?? 0);
            RedrawNotification();

            // Dispose old bitmap AFTER NotificationManager has received the new one
            oldArt?.Dispose();
        });
    }

    public void UpdateSeekPositionOptimistic(double targetSec)
    {
        if (_mediaSession == null) return;

        var state = _mediaSession.Controller?.PlaybackState?.State ?? PlaybackStateCompat.StatePlaying;
        float speed = state == PlaybackStateCompat.StatePlaying
            ? (_audioService is OwnAudioService own ? own.CurrentSpeed : 1.0f)
            : 0f;

        var stateBuilder = new PlaybackStateCompat.Builder(_mediaSession.Controller?.PlaybackState)
            .SetState(state, (long)(targetSec * 1000), speed, Android.OS.SystemClock.ElapsedRealtime());

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

        float playbackSpeed = compatState == PlaybackStateCompat.StatePlaying
            ? (_audioService is OwnAudioService own ? own.CurrentSpeed : 1.0f)
            : 0f;

        var isFav = _audioService?.CurrentTrackMetadata?.IsFavorite ?? false;
        var favIcon = isFav ? Resource.Drawable.media3_icon_heart_filled : Resource.Drawable.media3_icon_heart_unfilled;
        var favCustomAction = new PlaybackStateCompat.CustomAction.Builder(
            DimmerMediaSessionCallback.ActionFavorite,
            "Favorite",
            favIcon).Build();

        var stateBuilder = new PlaybackStateCompat.Builder()?
            .SetActions(PlaybackStateCompat.ActionPlay |
                        PlaybackStateCompat.ActionPause |
                        PlaybackStateCompat.ActionPlayPause |
                        PlaybackStateCompat.ActionSkipToNext |
                        PlaybackStateCompat.ActionSkipToPrevious |
                        PlaybackStateCompat.ActionSeekTo)?
            .AddCustomAction(favCustomAction)?
            .SetState(compatState, (long)(positionSec * 1000), playbackSpeed, Android.OS.SystemClock.ElapsedRealtime());

        _mediaSession.SetPlaybackState(stateBuilder?.Build());
    }
    private void RedrawNotification()
    {
        _mainHandler.Post(() =>
        {
            if (_mediaSession == null) return;

            var isPlaying = _audioService?.IsPlaying ?? false;
            var currentSong = _audioService?.CurrentTrackMetadata;

            var notification = NotificationHelper.BuildNotification(this, _mediaSession, isPlaying, currentSong, _currentCoverArt);
            if (notification is null) return;

            if (_isStartedInForeground)
                _notificationManager?.Notify(NotificationHelper.NotificationId, notification);
            else
                PromoteToForeground();

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
        _disposables.Dispose();

        _routeSwitchCts?.Cancel();
        _routeSwitchCts?.Dispose();
        if (_audioDeviceCallback != null)
            _audioManager?.UnregisterAudioDeviceCallback(_audioDeviceCallback);

        if (_noisyReceiver != null) UnregisterReceiver(_noisyReceiver);
        _audioManager?.AbandonAudioFocus(this);

        _currentCoverArt?.Dispose();
        _currentCoverArt = null;
        _mediaSession?.Release();

        if (_wakeLock?.IsHeld == true) _wakeLock.Release();
        ServiceCompat.StopForeground(this, ServiceCompat.StopForegroundRemove);
        _isStartedInForeground = false;

        base.OnDestroy();
    }
    private DimmerAudioDeviceCallback? _audioDeviceCallback;
    private CancellationTokenSource? _routeSwitchCts;

    private sealed class DimmerAudioDeviceCallback : AudioDeviceCallback
    {
        private readonly Action<AudioDeviceInfo[]> _onAdded;
        private readonly Action<AudioDeviceInfo[]> _onRemoved;
        private bool _initialSnapshotSkipped;

        public DimmerAudioDeviceCallback(
            Action<AudioDeviceInfo[]> onAdded,
            Action<AudioDeviceInfo[]> onRemoved)
        {
            _onAdded = onAdded;
            _onRemoved = onRemoved;
        }

        public override void OnAudioDevicesAdded(AudioDeviceInfo[]? addedDevices)
        {
            if (addedDevices == null || addedDevices.Length == 0) return;

            // Android fires OnAudioDevicesAdded once immediately upon registration
            // with already-connected devices (e.g. built-in speaker). Ignore that first call.
            if (!_initialSnapshotSkipped)
            {
                _initialSnapshotSkipped = true;
                return;
            }

            _onAdded(addedDevices);
        }

        public override void OnAudioDevicesRemoved(AudioDeviceInfo[]? removedDevices)
        {
            if (removedDevices == null || removedDevices.Length == 0) return;
            _onRemoved(removedDevices);
        }
    }

    private static bool IsExternalOutputDevice(AudioDeviceInfo info)
    {
        if (!info.IsSink) return false;

        return info.Type is AudioDeviceType.WiredHeadphones
            or AudioDeviceType.WiredHeadset
            or AudioDeviceType.BluetoothA2dp
            or AudioDeviceType.BluetoothSco
            or AudioDeviceType.BleHeadset
            or AudioDeviceType.UsbHeadset
            or AudioDeviceType.UsbDevice
            or AudioDeviceType.AuxLine
            or AudioDeviceType.HearingAid;
    }
    public override IBinder? OnBind(Intent? intent) => null;
}
