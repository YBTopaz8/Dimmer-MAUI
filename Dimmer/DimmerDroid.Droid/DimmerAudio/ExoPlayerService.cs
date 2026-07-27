#region Using Directives
// Android Core
using AndroidX.Media3.UI;

using Uri = Android.Net.Uri;

// AndroidX Core & Media

using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;

// Java Interop
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
using OwnaudioNET.Mixing;
using OwnaudioNET.Effects;
using OwnaudioNET.Sources;
using OwnaudioNET.Core;
using OwnaudioNET.Effects.SmartMaster;
using System.Collections.Concurrent;
using OwnaudioNET;
using Java.Lang;
using Exception = System.Exception;
using Math = System.Math;
using Ownaudio.Core;

namespace Dimmer.DimmerAudio; // Make sure this namespace is correct

[Service(Name = "com.yvanbrunel.dimmer.ExoPlayerService", // Ensure this matches AndroidManifest.xml if needed
         Enabled = true, Exported = true,
         ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter(new[] { "androidx.media3.session.MediaSessionService" })] // REQUIRED FOR MEDIA3 NOTIFICATION
[IntentFilter(new[] { "android.media.browse.MediaBrowserService" })]
public partial class ExoPlayerService : MediaSessionService
{


    public ExoPlayerService()
    {

    }
    // --- OwnaudioNET Components ---
    private AudioMixer? _mixer;
    private FileSource? _mainSource;
    private readonly ConcurrentDictionary<string, FileSource> _activeStems = new();

    private ReverbEffect? _reverbEffect;
    private Equalizer30BandEffect? _lofiEq;
    private SmartMasterEffect? _smartMaster;

    // --- Android System Media3 Session ---
    private MediaSession? _mediaSession;
    private ExoPlayerServiceBinder? _binder;
    private Handler? _positionHandler;

    public static SongModelView? CurrentSongContext { get; private set; }
    public static SongModelView? CurrentSongExposed => CurrentSongContext;

    public bool IsPlaying => _mainSource?.State == AudioState.Playing;
    public double CurrentPosition => _mainSource?.Position ?? 0;
    public double Duration => _mainSource?.Duration ?? 0;

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0, 1.0);
            if (_mixer != null) _mixer.MasterVolume = (float)_volume;
        }
    }

    public bool IsMuted { get; set; }

    // --- DSP Properties ---
    private double _playbackSpeed = 1.0;
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set { _playbackSpeed = value; ApplyPitchAndSpeed(); }
    }

    private double _pitchShift = 0.0;
    public double PitchShift
    {
        get => _pitchShift;
        set { _pitchShift = value; ApplyPitchAndSpeed(); }
    }

    private bool _isReversed;
    public bool IsReversed
    {
        get => _isReversed;
        set { _isReversed = value; ApplyPitchAndSpeed(); }
    }

    private void ApplyPitchAndSpeed()
    {
        float pitchVal = (float)(_pitchShift + (_playbackSpeed - 1.0));
        if (_isReversed) pitchVal = -Math.Abs(pitchVal == 0 ? 1.0f : pitchVal);

        if (_mainSource != null) _mainSource.PitchShift = pitchVal;
        foreach (var stem in _activeStems.Values) stem.PitchShift = pitchVal;
    }

    public bool EnableReverb { get => _reverbEffect?.Mix > 0; set { if (_reverbEffect != null) _reverbEffect.Mix = value ? (float)ReverbMix : 0f; } }
    public double ReverbMix { get; set; } = 0.4;
    public float ReverbRoomSize { get => _reverbEffect?.RoomSize ?? 0.8f; set { if (_reverbEffect != null) _reverbEffect.RoomSize = (float)value; } }
    public float ReverbDamping { get => _reverbEffect?.Damping ?? 0.4f; set { if (_reverbEffect != null) _reverbEffect.Damping = (float)value; } }

    public bool EnableLoFi { get => _lofiEq?.Enabled ?? false; set { if (_lofiEq != null) _lofiEq.Enabled = value; } }
    public double LoFiCutoffFrequency { get; set; } = 2000.0;

    public bool EnableSmartMaster { get => _smartMaster?.Enabled ?? false; set { if (_smartMaster != null) _smartMaster.Enabled = value; } }

    // --- Service Lifecycle ---

    private IDisposable? _positionRxSub;
    public override void OnCreate()
    {
        base.OnCreate();

        Task.Run(() =>
        {
            try
            {
                var config = new AudioConfig
                {
                    SampleRate = 48000,
                    Channels = 2,
                    BufferSize = 1024,
                    HostType = EngineHostType.AAUDIO,
                    FallbackToDefaultOnDisconnect = true
                };

                OwnaudioNet.Initialize(config);
                OwnaudioNet.Start();

                _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 1024);

                // Setup DSP Effects
                _reverbEffect = new ReverbEffect(size: 0.8f, damp: 0.4f, wet: 0.4f, dry: 0.8f, stereoWidth: 1.0f, mix: 0.0f);
                _lofiEq = new Equalizer30BandEffect { Enabled = false };
                _smartMaster = new SmartMasterEffect();
                _smartMaster.Initialize(config);
                _smartMaster.Enabled = false;

                _mixer.AddMasterEffect(_reverbEffect);
                _mixer.AddMasterEffect(_lofiEq);
                _mixer.AddMasterEffect(_smartMaster);
                _mixer.Start();

                // 2. Build Android System MediaSession & Foreground Notification
                Intent intent = new Intent(Platform.AppContext, typeof(MainActivity));
                PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
                PendingIntent pendingIntent = PendingIntent.GetActivity(Platform.AppContext, 0, intent, flags)!;

                // Simple dummy player stub for MediaSession so Android draws notifications natively
                var dummyPlayer = new AndroidX.Media3.ExoPlayer.ExoPlayerBuilder(this).Build();

                _mediaSession = new MediaSession.Builder(this, dummyPlayer)
                    .SetSessionActivity(pendingIntent)
                    .SetId("Dimmer_Ownaudio_Session")
                    .Build();

                this.SetMediaNotificationProvider(new DefaultMediaNotificationProvider.Builder(this).Build());

                _binder = new ExoPlayerServiceBinder(this);

                StartPositionPollingRx();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExoPlayerService] Android Init Error: {ex.Message}");
            }
        });
    }

    // --- Playback Commands ---

    public void PrepareTrack(SongModelView songModel, double startPos)
    {
        CurrentSongContext = songModel;

        try
        {
            ClearAllStems();
            if (_mainSource != null)
            {
                _mixer?.RemoveSource(_mainSource);
                _mainSource.Dispose();
            }

            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            _mainSource = new FileSource(songModel.FilePath, 1024, targetSampleRate: sr, targetChannels: ch);


            _mixer?.Stop();
            _mainSource.Seek(startPos);

            _mainSource.AttachToClock(_mixer!.MasterClock);
            _mixer.AddSource(_mainSource);

            if (startPos > 0) _mainSource.Seek(startPos);

            // Update Android OS Notification Card & Lockscreen Metadata
            UpdateSystemMediaNotification(songModel);

            Play(startPos);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ExoPlayerService] Prepare error: {ex.Message}");
        }
    }

    public void Play(double pos)
    {
        if (_mainSource == null || _mixer == null) return;

        _mainSource.Seek(pos);
        if (pos > 0 && Math.Abs(pos - CurrentPosition) > 0.5)
        {
            _mainSource.Seek(pos);
            foreach (var stem in _activeStems.Values) stem.Seek(pos);
        }

        _mixer.Start();
        _mainSource.Play();
        foreach (var stem in _activeStems.Values) stem.Play();

        IsPlayingChanged?.Invoke(this, true);
    }

    public void Pause()
    {
        _mainSource?.Pause();
        foreach (var stem in _activeStems.Values) stem.Pause();
        IsPlayingChanged?.Invoke(this, false);
    }

    public void Stop()
    {
        ClearAllStems();
        if (_mainSource != null)
        {
            _mainSource.Stop();
            _mixer?.RemoveSource(_mainSource);
            _mainSource.Dispose();
            _mainSource = null;
        }
        IsPlayingChanged?.Invoke(this, false);
    }

    public void Seek(double positionSeconds)
    {
        if (_mainSource == null) return;
        _mainSource.Seek(positionSeconds);
        foreach (var stem in _activeStems.Values) stem.Seek(positionSeconds)    ;
    }

    // --- Dynamic Stems ---

    public void AddStem(string stemId, string filePath, double initialVolume = 1.0)
    {
        if (!File.Exists(filePath) || _mixer == null) return;

        int sr = OwnaudioNet.Engine!.Config.SampleRate;
        int ch = OwnaudioNet.Engine!.Config.Channels;

        var stemSource = new OwnaudioNET.Sources.FileSource(filePath, 8192, targetSampleRate: sr, targetChannels: ch);
        stemSource.Volume = (float)initialVolume;
        stemSource.AttachToClock(_mixer.MasterClock);

        if (_mainSource != null) stemSource.Seek(_mainSource.Position);
        _mixer.AddSource(stemSource);

        if (IsPlaying) stemSource.Play();

        _activeStems.AddOrUpdate(stemId, stemSource, (k, old) => { _mixer.RemoveSource(old); old.Dispose(); return stemSource; });
    }

    public void RemoveStem(string stemId)
    {
        if (_activeStems.TryRemove(stemId, out var stem))
        {
            _mixer?.RemoveSource(stem);
            stem.Dispose();
        }
    }

    public void SetStemVolume(string stemId, double volume)
    {
        if (_activeStems.TryGetValue(stemId, out var stem)) stem.Volume = (float)volume;
    }

    public void ClearAllStems()
    {
        foreach (var kvp in _activeStems)
        {
            _mixer?.RemoveSource(kvp.Value);
            kvp.Value.Dispose();
        }
        _activeStems.Clear();
    }

    // --- Android System Metadata Integration ---

    private void UpdateSystemMediaNotification(SongModelView song)
    {
        if (_mediaSession == null) return;

        var metadata = new MediaMetadata.Builder()
            .SetTitle(song.Title)
            .SetArtist(song.ArtistName)
            .SetAlbumTitle(song.AlbumName)
            .SetArtworkUri(string.IsNullOrEmpty(song.CoverImagePath) ? null : Uri.FromFile(new Java.IO.File(song.CoverImagePath)))
            .Build();

        var mediaItem = new MediaItem.Builder()
            .SetMediaId(song.Id.ToString())
            .SetMediaMetadata(metadata)
            .Build();

        _mediaSession.Player.SetMediaItem(mediaItem);
        _mediaSession.Player.Prepare();
    }

    // --- Polling for UI Updates ---
    private void StartPositionPollingRx()
    {
        // Clean up any existing subscription
        _positionRxSub?.Dispose();

        // Create a 100ms interval ticker on a background thread
        _positionRxSub = Observable.Interval(TimeSpan.FromMilliseconds(100))
            .Where(_ => IsPlaying && _mainSource != null && _mixer != null) // Only execute when playing
            .Subscribe(_ =>
            {
                // Read exact clock safely
                double clockPos = _mixer!.MasterClock.CurrentTimestamp;
                PositionChanged?.Invoke(this, clockPos);

                // Auto-stop at end of track
                if (clockPos >= Duration - 0.1)
                {
                    PlayingEnded?.Invoke(this, EventArgs.Empty);
                    Stop();
                }
            });
    }
    // --- Device Management ---

    public List<AudioOutputDevice> GetAvailableDevices()
    {
        var audioManager = Platform.AppContext.GetSystemService(AudioService) as AudioManager;
        var devices = audioManager?.GetDevices(GetDevicesTargets.Outputs) ?? Array.Empty<Android.Media.AudioDeviceInfo>();

        return devices.Select(d => new AudioOutputDevice
        {
            Id = d.Id.ToString(),
            Name = d.ProductNameFormatted?.ToString() ?? d.Type.ToString(),
            Type = d.Type.ToString(),
            IsPlaybackDevice = true
        }).ToList();
    }

    public bool SetPreferredDevice(AudioOutputDevice dev) => true;

    // --- Events & Binder ---

    public event EventHandler<double>? PositionChanged;
    public event EventHandler<bool>? IsPlayingChanged;
    public event EventHandler? PlayingEnded;

    public override IBinder OnBind(Intent? intent)
    {
        if (intent?.Action == "androidx.media3.session.MediaSessionService")
            return base.OnBind(intent);

        return _binder!;
    }

    public override MediaSession? OnGetSession(MediaSession.ControllerInfo? p0) => _mediaSession;

    public override void OnDestroy()
    {
        Stop();
        _mixer?.Dispose();
        OwnaudioNet.Stop();
        OwnaudioNet.Shutdown();

        _mediaSession?.Release();
        base.OnDestroy();
    }
}

public partial class ExoPlayerServiceBinder : Binder
{
    public ExoPlayerService Service { get; }
    internal ExoPlayerServiceBinder(ExoPlayerService service) { Service = service; }
}