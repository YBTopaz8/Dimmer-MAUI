

using OwnaudioNET;
using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;
using OwnaudioNET.Mixing;
using OwnaudioNET.Sources;

using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;

namespace Dimmer.DimmerAudio;


public partial class OwnAudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    private static readonly Lazy<OwnAudioService> lazyInstance = new(() => new OwnAudioService());
    public static IDimmerAudioService Current => lazyInstance.Value;

    // --- Rx.NET State ---
    private readonly CompositeDisposable _disposables = new();

    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);
    private readonly BehaviorSubject<DimmerPlaybackState> _playbackState = new(DimmerPlaybackState.None);
    private readonly BehaviorSubject<double> _currentPosition = new(0);
    private readonly BehaviorSubject<double> _duration = new(0);
    private readonly BehaviorSubject<double> _volume = new(1.0);
    private readonly BehaviorSubject<bool> _isMuted = new(false);

    private readonly Subject<PlaybackEventArgs> _playEndedSubject = new();
    private readonly Subject<Exception> _errorSubject = new();

    // --- OwnAudioSharp Engine Components ---

    private AudioMixer? _mixer;
    private FileSource? _mainSource;
    private FileSource? _ambienceSource;
    private readonly SemaphoreSlim _transportLock = new(1, 1); // Prevents fast-clicking crashes


    // --- The Core 5 Effects ---
    private EqualizerEffect? _eqEffect;
    private ReverbEffect? _reverbEffect;
    private CompressorEffect? _compressorEffect;
    private SmartMasterEffect? _smartMasterEffect;

    // Pitch & Tempo state (needs to be remembered across tracks)
    private float _currentPitchSemitones = 0f;
    private float _currentTempoRatio = 1f;

    // --- Smooth Playhead Interpolation ---
    private double _lastEnginePos;
    private double _lastEnginePosAt;
    private readonly Stopwatch _watch = new();


    private bool _isDisposed;
    private bool _isAmbienceEnabled;
    private string? _currentAudioDeviceId;

    public OwnAudioService()
    {
       

        // 3. Setup Position Polling Loop (Native engines usually need polling)
        Observable.Interval(TimeSpan.FromMilliseconds(17))
            .Where(_ => IsPlaying)
            .Subscribe(_ => UpdatePositionFromEngine())
            .DisposeWith(_disposables);
    }

        public async Task InitializeEngineAsync(string? outputDeviceId = null)
        {
            try
            {
                var config = OwnaudioNet.CreateDefaultConfig();
                config.EnableInput = false;
                config.OutputDeviceId = outputDeviceId; // null = system default
                config.FallbackToDefaultOnDisconnect = true;

                await OwnaudioNet.InitializeAsync(config);
                OwnaudioNet.Start();

                // The bus everything gets summed into (buffer size 1024 is standard)
                _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 1024);


                _compressorEffect = new CompressorEffect { Enabled = false };
                _eqEffect = new EqualizerEffect { Enabled = false };
                _reverbEffect = new ReverbEffect { Enabled = false };
                _smartMasterEffect = new SmartMasterEffect { Enabled = false };

                _mixer.AddMasterEffect(_compressorEffect);
                _mixer.AddMasterEffect(_eqEffect);
                _mixer.AddMasterEffect(_reverbEffect);
                _mixer.AddMasterEffect(_smartMasterEffect);


                // Listen for natural playback end
                _mixer.PlaybackEnded += Mixer_PlaybackEnded;

                _mixer.Start();
                _mixer.MasterVolume = (float)_volume.Value;

                _currentAudioDeviceId = outputDeviceId;

                Debug.WriteLine("[OwnAudioService] Engine Initialized Successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OwnAudioService] Failed to init engine: {ex.Message}");
                _errorSubject.OnNext(ex);
            }
            SetupRxEventBridging();
        }
    
    // 1. PITCH & TEMPO (High Pitch / Chopped & Screwed)
    public void SetPitchAndSpeed(float pitchSemitones, float tempoRatio)
    {
        _currentPitchSemitones = Math.Clamp(pitchSemitones, -12f, 12f);
        _currentTempoRatio = Math.Clamp(tempoRatio, 0.5f, 2.0f);

        if (_mainSource != null)
        {
            _mainSource.SetPitchSmooth(_currentPitchSemitones);
            _mainSource.SetTempoSmooth(_currentTempoRatio);
        }
    }


    // 2. EQUALIZER (10-Band - Bass Boost, Treble, etc.)
    public void EnableEqualizer(bool enable)
    {
        if (_eqEffect != null)
            _eqEffect.Enabled = enable;
    }

    public void SetEqualizerPreset(EqualizerPreset preset)
    {
        _eqEffect?.SetPreset(preset);
    }
    // Observable array for the UI to bind to (-12dB to +12dB usually)
    private readonly BehaviorSubject<float[]> _eqBands = new(new float[10]);
    public IObservable<float[]> EqBands => _eqBands.AsObservable();

    // Expose a method the UI sliders will call when dragged
    public void ChangeEqBand(int bandIndex, float gainDb)
    {
        if (bandIndex < 0 || bandIndex > 9)
            return;

        // Update the engine
        SetEqualizerBand(bandIndex, gainDb);

        // Update the state so the UI stays in sync
        var currentBands = _eqBands.Value;
        currentBands[bandIndex] = gainDb;
        _eqBands.OnNext(currentBands);
    }
    public void SetEqualizerBand(int bandIndex, float gainDb)
    {
        // Band 0 = 31.25Hz (Sub Bass), Band 9 = 16kHz (Air)
        if (_eqEffect == null || bandIndex < 0 || bandIndex > 9)
            return;

        switch (bandIndex)
        {
            case 0:
                _eqEffect.Band0Gain = gainDb;
                break;
            case 1:
                _eqEffect.Band1Gain = gainDb;
                break;
            case 2:
                _eqEffect.Band2Gain = gainDb;
                break;
            case 3:
                _eqEffect.Band3Gain = gainDb;
                break;
            case 4:
                _eqEffect.Band4Gain = gainDb;
                break;
            case 5:
                _eqEffect.Band5Gain = gainDb;
                break;
            case 6:
                _eqEffect.Band6Gain = gainDb;
                break;
            case 7:
                _eqEffect.Band7Gain = gainDb;
                break;
            case 8:
                _eqEffect.Band8Gain = gainDb;
                break;
            case 9:
                _eqEffect.Band9Gain = gainDb;
                break;
        }
    }

    // 3. REVERB (Live/Spatial feel)
    public void EnableReverb(bool enable, float roomSize = 0.8f, float mix = 0.25f)
    {
        if(!enable)
        {
            _reverbEffect?.Enabled = false;
            //Debugger.Break();
            
            return;
        }
        if (_reverbEffect != null)
        {
            _reverbEffect.RoomSize = roomSize; // 0.0 to 1.0
            _reverbEffect.Mix = mix;           // 0.0 to 1.0
            _reverbEffect.Enabled = enable;
        }
    }

    // 4. COMPRESSOR (Volume Normalization)
    public void EnableCompressor(bool enable, CompressorPreset preset = CompressorPreset.VocalGentle)
    {
        if (_compressorEffect != null)
        {
            _compressorEffect.SetPreset(preset);
            _compressorEffect.Enabled = enable;
        }
    }

    // 5. SMART MASTER (Audiophile Polish / Enhancement)
    public void EnableSmartMaster(bool enable, SpeakerType targetSpeaker = SpeakerType.HiFi)
    {
        if (_smartMasterEffect != null)
        {
            _smartMasterEffect.LoadSpeakerPreset(targetSpeaker);
            _smartMasterEffect.Enabled = enable;
        }
    }


    private void Mixer_PlaybackEnded(object? sender, EventArgs e)
    {
        // Track finished playing naturally. Triggered by Rust native callback.
        var args = new PlaybackEventArgs(_currentSong.Value) { EventType = DimmerPlaybackState.PlayCompleted };
        _playEndedSubject.OnNext(args);
    }


    private void SetupRxEventBridging()
    {
        _playbackState.DistinctUntilChanged().Subscribe(state =>
        {
            var isPlaying = state == DimmerPlaybackState.Playing;
            var args = new PlaybackEventArgs(_currentSong.Value) { EventType = state, IsPlaying = isPlaying };

            PlaybackStateChanged?.Invoke(this, args);
            IsPlayingChanged?.Invoke(this, args);
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(CurrentPlaybackState));

            if (isPlaying)
                _watch.Start();
            else
                _watch.Stop();
        }).DisposeWith(_disposables);

        _currentPosition.DistinctUntilChanged().Subscribe(pos =>
        {
            PositionChanged?.Invoke(this, pos);
            OnPropertyChanged(nameof(CurrentPosition));
        }).DisposeWith(_disposables);

        _duration.DistinctUntilChanged().Subscribe(dur =>
        {
            DurationChanged?.Invoke(this, dur);
            OnPropertyChanged(nameof(Duration));
        }).DisposeWith(_disposables);

        _volume.DistinctUntilChanged().Subscribe(vol =>
        {
            VolumeChanged?.Invoke(this, vol);
            OnPropertyChanged(nameof(Volume));
        }).DisposeWith(_disposables);

        _playEndedSubject.Subscribe(args =>
        {
            PlayEnded?.Invoke(this, args);
            _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
        }).DisposeWith(_disposables);

        _errorSubject.Subscribe(ex =>
        {
            var args = new PlaybackEventArgs(_currentSong.Value) { EventType = DimmerPlaybackState.Error };
            ErrorOccurred?.Invoke(this, args);
        }).DisposeWith(_disposables);
    }

    private void UpdatePositionFromEngine()
    {
        if (_mixer == null || _mainSource == null || _mainSource.IsEndOfStream)
            return;

        // Use the mixer's MasterClock for sample-accurate position
        double enginePos = _mixer.MasterClock.CurrentTimestamp;
        double now = _watch.Elapsed.TotalSeconds;

        if (Math.Abs(enginePos - _lastEnginePos) > 0.001) // A fresh report landed from native engine
        {
            _lastEnginePos = enginePos;
            _lastEnginePosAt = now;
        }

        // Interpolate between reports so the UI slider glides instead of ticking
        double smoothPos = _lastEnginePos + (now - _lastEnginePosAt);
        _currentPosition.OnNext(Math.Clamp(smoothPos, 0, _duration.Value));
    }

    #region IDimmerAudioService Properties

    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
    public SongModelView? CurrentTrackMetadata => _currentSong.Value;

    public DimmerPlaybackState CurrentPlaybackState => _playbackState.Value;
    public bool IsPlaying => _playbackState.Value == DimmerPlaybackState.Playing;

    public double CurrentPosition => _currentPosition.Value;
    public double Duration => _duration.Value;

    public double Volume
    {
        get => _volume.Value;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
         
            _mixer?.MasterVolume = (float)clamped;
            _volume.OnNext(clamped);
        }
    }

    private double _ambienceVolume = 0.5;
    public double AmbienceVolume
    {
        get => _ambienceVolume;
        set
        {
            _ambienceVolume = Math.Clamp(value, 0.0, 1.0);
            if (_ambienceSource != null)
            {
                _ambienceSource.Volume = (float)_ambienceVolume;
            }
        }
    }

    public IEnumerable<AudioOutputDevice>? PlaybackDevices { get; set; }
    DimmerPlaybackState IDimmerAudioService.CurrentPlaybackState { get; set; }

    #endregion

    #region IDimmerAudioService Events (Interface Compliance)

    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayEnded;
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed; // Will need SMTC hooking later
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;     // Will need SMTC hooking later

    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Core Playback

    public async Task InitializeAsync(SongModelView songModel, double pos)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(songModel);

        await _transportLock.WaitAsync();
        try
        {
            _playbackState.OnNext(DimmerPlaybackState.Opening);
            _currentSong.OnNext(songModel);

            // 1. Clean up old source
            if (_mainSource != null)
            {
                _mixer?.RemoveSource(_mainSource.Id);
                _mainSource.Dispose();
            }

            // 2. Load File 
            // Matching engine's sample rate prevents real-time conversion overhead
            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            _mainSource = new FileSource(songModel.FilePath, targetSampleRate: sr, targetChannels: ch);

            _mainSource.SetPitchSmooth(_currentPitchSemitones);
            _mainSource.SetTempoSmooth(_currentTempoRatio);
            _duration.OnNext(_mainSource.Duration);

            // 3. Add to mixer via Prepared (Prevents drift, ensures sync)
            _mixer?.Pause();
            _mixer?.Seek(pos);
            _mixer?.AddSourcePrepared(_mainSource);

            // 4. Start
            _mixer?.StartPreparedSources(startPosition: pos);
            _mixer?.Start();

            _playbackState.OnNext(DimmerPlaybackState.Playing);
            Debug.WriteLine($"[OwnAudioService] Initialized and playing: {songModel.Title}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OwnAudioService] InitializeAsync Error: {ex.Message}");
            _errorSubject.OnNext(ex);
        }
        finally
        {
            _transportLock.Release();
        }
    }

    public async Task PlayAsync(double pos)
    {
        ThrowIfDisposed();
        try
        {
            await _transportLock.WaitAsync();
            if (_mainSource != null)
            {
                if (pos > 0)
                {
                    _mixer?.Seek(pos);
                    _mainSource.Seek(pos); // Depending on the lib, if it needs TimeSpan, use TimeSpan.FromSeconds(pos)
                }

                // FIX: Wake the source back up!
                _mainSource.Play();

                // FIX: Start the master bus
                _mixer?.Start();

                _playbackState.OnNext(DimmerPlaybackState.Playing);

                if (_isAmbienceEnabled && _ambienceSource != null)
                {
                    _ambienceSource.Play();
                }
            }
        }
        catch (Exception ex)
        {
            _errorSubject.OnNext(ex);
        }
        finally { _transportLock.Release(); }
    }

    public async Task PauseAsync()
    {
        ThrowIfDisposed();
        await _transportLock.WaitAsync();
        try
        {
            _mainSource?.Pause();
            _ambienceSource?.Pause();

            // FIX: Pause the master bus so the clock stops ticking
            _mixer?.Pause();

            _playbackState.OnNext(DimmerPlaybackState.PausedDimmer);
        }
        catch (Exception ex)
        {
            _errorSubject.OnNext(ex);
        }
        finally { _transportLock.Release(); }
    }

    public async Task SeekAsync(double positionSeconds)
    {
        ThrowIfDisposed();
        await _transportLock.WaitAsync();
        try
        {
            if (_mainSource != null && _mixer != null)
            {
                var safePos = Math.Clamp(positionSeconds, 0, _duration.Value);

                // FIX: Move both the master clock AND the file decoder
                _mixer.Seek(safePos);
                _mainSource.Seek(safePos); // If you get a compile error here, use: TimeSpan.FromSeconds(safePos)

                _lastEnginePos = safePos;
                _currentPosition.OnNext(safePos);
                SeekCompleted?.Invoke(this, safePos);
            }
        }
        finally { _transportLock.Release(); }
    }
    public void Stop()
    {
        ThrowIfDisposed();
        _mainSource?.Stop();
        _ambienceSource?.Stop();
        _currentPosition.OnNext(0);
        _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
    }

    #endregion

    #region Ambience

    public async Task InitializeAmbienceAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;
        await _transportLock.WaitAsync();
        try
        {
            _ambienceSource?.Dispose();
            if (_ambienceSource is not null)
                _mixer?.RemoveSource(_ambienceSource);

            // Set Loop = true for rain/wind etc.
            _ambienceSource = new FileSource(filePath)
            {
                Loop = true,
                Volume = (float)_ambienceVolume
            };

            _mixer?.AddSource(_ambienceSource);

            if (_isAmbienceEnabled && IsPlaying)
            {
                _ambienceSource.Play();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OwnAudioService] Failed to load ambience: {ex.Message}");
        }
        finally
        {
            _transportLock.Release();
        }

        await Task.CompletedTask;
    }

    public void ToggleAmbience(bool isEnabled)
    {
        _isAmbienceEnabled = isEnabled;
        if (_ambienceSource == null)
            return;

        if (isEnabled && IsPlaying)
        {
            _ambienceSource.Play();
        }
        else
        {
            _ambienceSource.Pause();
        }
    }

    #endregion

    #region Device Management 

    public async Task<List<AudioOutputDevice>?> GetAllAudioDevicesAsync()
    {
        var outputDevs = await OwnaudioNet.GetOutputDevicesAsync();
        var devices = new List<AudioOutputDevice>();

        foreach (var d in outputDevs)
        {
            devices.Add(new AudioOutputDevice
            {
                Id = d.DeviceId,
                Name = d.Name,
                IsDefaultDevice = d.IsDefault,
                ProductName = d.EngineName
            });
        }
        PlaybackDevices = devices;
        return devices;
    }

    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        return PlaybackDevices?.FirstOrDefault(d => d.Id == _currentAudioDeviceId)
            ?? PlaybackDevices?.FirstOrDefault(d => d.IsDefaultDevice);
    }

    public bool SetPreferredOutputDevice(AudioOutputDevice dev)
    {
        if (dev.Id == _currentAudioDeviceId)
            return true;

        Task.Run(async () =>
        {
            await _transportLock.WaitAsync();
            try
            {
                // To switch devices in OwnAudio, we must re-init the whole stack
                var wasPlaying = IsPlaying;
                var currentPos = _currentPosition.Value;

                _mixer?.Stop();
                _mixer?.Dispose();
                _mixer = null;

                await OwnaudioNet.ShutdownAsync();

                // Re-initialize with new device
                await InitializeEngineAsync(dev.Id);

                // Restore state
                if (_mainSource != null)
                {
                    _mixer?.AddSourcePrepared(_mainSource);
                    _mixer?.StartPreparedSources(currentPos);
                    if (wasPlaying)
                        _mixer?.Start();
                }
            }
            finally { _transportLock.Release(); }
        });
        return true;
    }

    public async Task SetDefaultAsync(AudioOutputDevice device)
    {
        SetPreferredOutputDevice(device);
        await Task.CompletedTask;
    }

    public double GetCurrentVolume() => _volume.Value;

    public void SetVolume(double volume)
    {
        Volume = volume;
    }

    public void MuteDevice(bool mute)
    {
        _isMuted.OnNext(mute);
        if (_mixer != null)
        {
            _mixer.MasterVolume = mute ? 0.0f : (float)_volume.Value;
        }
    }

    #endregion

    #region Queue / Playlist Handling

    public void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels)
    {
        // OwnAudio gives you control of queueing. For simple implementation,
        // just init the first track. You can implement gapless by preloading the 
        // next track in a secondary FileSource and crossfading!
        _ = InitializeAsync(songModelView, 0);
    }

    public async Task SendNextSong(SongModelView nextSong)
    {
        // Here, we could pre-load the next song into a backup FileSource for 
        // zero-latency gapless transition when _mainSource hits EndOfStream.
        await Task.CompletedTask;
    }

    #endregion

    #region Utilities & INotifyPropertyChanged

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(OwnAudioService));
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        // Safely invoke on UI thread if needed, or directly if subscribers marshal
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;

        Debug.WriteLine("[OwnAudioService] Disposing Rust Audio Engine...");

        _disposables.Dispose(); // Clears all Rx Subscriptions

        if (_mixer != null)
            _mixer.PlaybackEnded -= Mixer_PlaybackEnded;

        _mainSource?.Stop();
        _mainSource?.Dispose();

        _ambienceSource?.Stop();
        _ambienceSource?.Dispose();

        // --- Dispose Effects ---
        _smartMasterEffect?.OnPlaybackStopped();
        _smartMasterEffect?.Dispose();
        _reverbEffect?.Dispose();
        _eqEffect?.Dispose();
        _compressorEffect?.Dispose();


        // Close the Native Engine
        _mixer?.Stop();
        _mixer?.Dispose();


        await OwnaudioNet.ShutdownAsync();

        // Null out C# events to prevent leaks
        IsPlayingChanged = null;
        PlayEnded = null;
        PlaybackStateChanged = null;
        ErrorOccurred = null;
        PositionChanged = null;
        DurationChanged = null;
        SeekCompleted = null;
        PropertyChanged = null;

    }

    #endregion

    public void SetPlaybackMode(PlaybackModeEnum mode, float customReverbMix = 0.25f, float customRoomSize = 0.6f)
    {
        switch (mode)
        {
            case PlaybackModeEnum.Normal:
                SetPitchAndSpeed(0f, 1f);
                EnableReverb(false); // MUST turn off!
                break;

            case PlaybackModeEnum.Nightcore:
                // Nightcore is usually faster + higher pitch (e.g., +3 semitones, 1.25x speed)
                SetPitchAndSpeed(3f, 1.25f);
                EnableReverb(false); // MUST turn off!
                break;

            case PlaybackModeEnum.SlowedAndReverb:
                // Slowed is usually -3 semitones, 0.85x speed
                SetPitchAndSpeed(-3f, 0.85f);
                // Here we use your tuneable parameters!
                EnableReverb(true, roomSize: customRoomSize, mix: customReverbMix);
                break;
        }
    }


    #region DJ/DuoPlayback
    private FileSource? _secondarySource;
    private double _crossfadeBalance = 0.5; // 0.0 = Only Track A, 1.0 = Only Track B, 0.5 = Both


    public async Task InitializeDjModeAsync(string trackA_Path, string trackB_Path)
    {
        await _transportLock.WaitAsync();
        try
        {
            // 1. Clean up old sources
            if (_mainSource != null)
            { _mixer?.RemoveSource(_mainSource.Id); _mainSource.Dispose(); }
            if (_secondarySource != null)
            { _mixer?.RemoveSource(_secondarySource.Id); _secondarySource.Dispose(); }

            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            // 2. Load both files
            _mainSource = new FileSource(trackA_Path, targetSampleRate: sr, targetChannels: ch);
            _secondarySource = new FileSource(trackB_Path, targetSampleRate: sr, targetChannels: ch);

            // 3. Set up the mixer (Use Prepared so they start perfectly in sync, down to the sample!)
            _mixer?.Pause();
            _mixer?.AddSourcePrepared(_mainSource);
            _mixer?.AddSourcePrepared(_secondarySource);

            UpdateCrossfadeVolumes(); // Apply initial volumes

            // 4. Start them simultaneously
            _mixer?.StartPreparedSources(0);
            _mixer?.Start();

            _playbackState.OnNext(DimmerPlaybackState.Playing);
        }
        finally
        {
            _transportLock.Release();
        }
    }

    /// <summary>
    /// 
    // 0.0 = Track A full volume, Track B silent
    // 1.0 = Track B full volume, Track A silent
    // 0.5 = Both at 50%
    /// 
    /// </summary>
    /// <param name="balance"></param>
    public void SetDjCrossfade(double balance=0.5)
    {
        _crossfadeBalance = Math.Clamp(balance, 0.0, 1.0);
        UpdateCrossfadeVolumes();
    }

    private void UpdateCrossfadeVolumes()
    {
        if (_mainSource != null)
            _mainSource.Volume = (float)(1.0 - _crossfadeBalance);

        if (_secondarySource != null)
            _secondarySource.Volume = (float)_crossfadeBalance;
    }
    #endregion
}


/// <summary>
/// Provides audio playback services using Windows.Media.Playback.MediaPlayer.
/// Implements IDimmerAudioService, INotifyPropertyChanged, and IAsyncDisposable.
/// Designed for robustness, asynchronous operations, and clear state management.
/// </summary>
//public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
//{
//    #region Singleton & Initialization


//    private static readonly Lazy<AudioService> lazyInstance = new(() => new AudioService());
//    public static IDimmerAudioService Current => lazyInstance.Value;

//    private MediaPlaybackList _playbackList;

//    private readonly MediaPlayer _mediaPlayer; 
//    private readonly MediaPlayer _ambiencePlayer;
//    private readonly DispatcherQueue _dispatcherQueue;
//    private CancellationTokenSource? _initializationCts;
//    private SongModelView? _currentTrackMetadata;
//    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);

//    private readonly CompositeDisposable _disposables = new();
//    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
//    private bool _isDisposed;
//    private string? _currentAudioDeviceId;
//    private readonly CoreAudioController _controller;
//    private readonly object _sync = new();
//    public IEnumerable<AudioOutputDevice>? PlaybackDevices
//    { get; set; }

//    public CoreAudioDevice? DefaultPlaybackDevice
//    {
//        get
//        {
//            return _controller.GetDefaultDevice(DeviceType.Playback, Role.Multimedia);
//        }
//    }

//    public AudioService()
//    {
//        _controller = new CoreAudioController();
//        AudioSwitcher.AudioApi.CoreAudio.CoreAudioDevice defaultPlaybackDevice = _controller.DefaultPlaybackDevice;
//        //defaultPlaybackDevice.StateChanged += DefaultPlaybackDevice_StateChanged;
//        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
//            ?? throw new InvalidOperationException("AudioService must be initialized on a thread with a DispatcherQueue (typically the UI thread).");



//        _playbackList = new MediaPlaybackList();
//        _playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;


//        _mediaPlayer = new MediaPlayer
//        {
//            AudioCategory = MediaPlayerAudioCategory.Media,
//            CommandManager = { IsEnabled = true },


//        };

//        _ambiencePlayer = new MediaPlayer
//        {
//            AudioCategory = MediaPlayerAudioCategory.GameMedia, // 'GameMedia' often mixes better as background fx
//            IsLoopingEnabled = true, // Crucial: Rain must loop forever
//            Volume = 0.5 // Default starting volume
//        };
//        _ambiencePlayer.CommandManager.IsEnabled = false;

//        SubscribeToPlayerEvents();
//        SubscribeToSystemEvents();



//        _volume = _mediaPlayer.Volume;
//        _isMuted = _mediaPlayer.IsMuted;
//        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);

//        _ = Task.Run(async () => await GetSetUpOutPutDevices());
//    }


//    private void PlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
//    {

//        if (args.Reason == MediaPlaybackItemChangedReason.EndOfStream)
//        {

//            MediaPlayer_MediaEnded(null, args);
//        }
//        if (args.NewItem == null ||_nextSongInList is null) return;
//        var newProps = args.NewItem.GetDisplayProperties();
//        //_currentTrackMetadata = _nextSongInList;

//    }

//    private async Task GetSetUpOutPutDevices()
//    {
//        var outputDevices = new List<AudioOutputDevice>();
//        try
//        {

//            string selector = MediaDevice.GetAudioRenderSelector();
//            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);

//            foreach (var device in devices)
//            {
//                outputDevices.Add(new AudioOutputDevice { Id = device.Id, Name = device.Name });
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] Error getting audio output devices: {ex}");
//            OnErrorOccurred("Failed to enumerate audio output devices.", ex);
//        }
//        PlaybackDevices = outputDevices;
//    }

//    private void SubscribeToSystemEvents()
//    {
//        MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
//        DefaultAudioDevice = _controller.GetDefaultDevice(DeviceType.Playback, Role.Multimedia);
//        _controller.AudioDeviceChanged.Subscribe(e =>
//        {
//            switch (e)
//            {

//                case DefaultDeviceChangedArgs def: 
//                    Debug.WriteLine($"Default changed: {def.Device.Name}");
//                    DefaultAudioDevice = (CoreAudioDevice)def.Device;
//                    break;
//                case DeviceAddedArgs add: 
//                    Debug.WriteLine($"Device added: {add.Device.Name}"); 
//                    break;
//                case DeviceRemovedArgs rem: 
//                    Debug.WriteLine($"Device removed: {rem.Device.Name}"); 
//                    break;
//                case DeviceChangedArgs chg: 
//                    Debug.WriteLine($"Device property changed: {chg.Device.Name}"); 
//                    break;
//            }
//        }).DisposeWith(_disposables);

//        if (DefaultAudioDevice is not null)
//            DefaultAudioDevice.VolumeChanged.Subscribe(newVol =>
//            {

//                DeviceVolumeChanged?.Invoke(DefaultAudioDevice, (newVol.Device.Volume, newVol.Device.IsMuted, 100));

//            }).DisposeWith(_disposables);

//    }
//    public AudioOutputDevice? GetCurrentAudioOutputDevice()
//    {
//        var currentDev = _controller.DefaultPlaybackDevice;
//        if (currentDev is null) return null;
//        return new AudioOutputDevice
//        {
//            Id = currentDev.Id.ToString(),
//            Name = currentDev.Name,
//            IsDefaultDevice = currentDev.IsDefaultDevice,
//            IsMuted = currentDev.IsMuted,
//            Volume = currentDev.Volume
//            ,ProductName= currentDev.FullName
//            ,IconString = currentDev.IconPath
//        };
//    }
//    public double GetCurrentVolume()
//    {
//        return _controller.DefaultPlaybackDevice.Volume;
//    }

//    public async Task SetVolume(double volume)
//    {
//        var dev = _controller.DefaultPlaybackDevice;
//        if (dev != null)
//            await dev.SetVolumeAsync(volume);
//    }

//    public async Task SetDefaultAsync(AudioOutputDevice device)
//    {

//        CoreAudioDevice newDev = _controller.GetDevice(Guid.Parse(device.Id!)) as CoreAudioDevice;
//        if (device == null) return;
//        await newDev.SetAsDefaultAsync();
//    }

//    public void WatchVolume()
//    {

//        var dev = _controller.GetDefaultDevice(DeviceType.Playback, Role.Multimedia);
//        dev.VolumeChanged.Subscribe(x =>
//        {
//            Debug.WriteLine($"Volume: {x.Volume}");
//        });
//        dev.MuteChanged.Subscribe(x =>
//        {
//            Debug.WriteLine($"Muted: {x.IsMuted}");
//        });
//    }

//    public async Task MuteDevice(bool mute)
//    {
//        var dev = _controller.DefaultPlaybackDevice;
//        if (dev != null)
//           await dev.SetMuteAsync(mute);
//    }
//    private void SubscribeToPlayerEvents()
//    {



//        _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
//        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
//        _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
//        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
//        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
//        _mediaPlayer.PlaybackSession.NaturalDurationChanged += PlaybackSession_NaturalDurationChanged;
//        _mediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
//        _mediaPlayer.PlaybackSession.MediaPlayer.VolumeChanged +=MediaPlayer_VolumeChanged;

//        _mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
//        _mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
//        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
//        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

//        _mediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;

//        _mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
//        _mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
//    }

//    private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
//    {

//        VolumeChanged?.Invoke(sender, sender.Volume);
//    //DeviceVolumeChanged?.Invoke(sender, (sender.Volume,sender.IsMuted,100));
//    }

//    private void UnsubscribeFromSystemEvents()
//    {
//        _disposables.Clear();
//    }
//    private void UnsubscribeFromPlayerEvents()
//    {

//        MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;
//        if (_mediaPlayer == null)
//            return;
//        _mediaPlayer.VolumeChanged -= MediaPlayer_VolumeChanged;
//        _mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
//        _mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
//        _mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;

//        var session = _mediaPlayer.PlaybackSession;
//        if (session != null)
//        {
//            session.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
//            session.PositionChanged -= PlaybackSession_PositionChanged;
//            session.NaturalDurationChanged -= PlaybackSession_NaturalDurationChanged;
//            session.SeekCompleted -= PlaybackSession_SeekCompleted;
//        }

//        var commandManager = _mediaPlayer.CommandManager;
//        if (commandManager != null)
//        {
//            commandManager.PlayReceived -= CommandManager_PlayReceived;
//            commandManager.PauseReceived -= CommandManager_PauseReceived;
//            commandManager.NextReceived -= CommandManager_NextReceived;
//            commandManager.PreviousReceived -= CommandManager_PreviousReceived;
//            commandManager.IsEnabled = false;
//        }
//    }

//    #endregion

//    private double _requestedSeekPosition = -1;
//    #region Events (Interface + Additional)


//    private EventHandler<PlaybackEventArgs>? _isPlayingChanged;
//    public event EventHandler<PlaybackEventArgs> IsPlayingChanged
//    {
//        add => _isPlayingChanged += value;
//        remove => _isPlayingChanged -= value;
//    }

//    private EventHandler<PlaybackEventArgs>? _playEnded;
//    public event EventHandler<PlaybackEventArgs> PlayEnded
//    {
//        add => _playEnded += value;
//        remove => _playEnded -= value;
//    }

//    private EventHandler<PlaybackEventArgs>? _playStarted;
//    public event EventHandler<PlaybackEventArgs> PlayStarted
//    {
//        add => _playStarted += value;
//        remove => _playStarted -= value;
//    }


//    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
//    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
//    public event EventHandler<double>? DurationChanged;
//    public event EventHandler<double>? PositionChanged;
//    public event EventHandler<double>? SeekCompleted;
//    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;
//    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed;
//    public event PropertyChangedEventHandler? PropertyChanged;
//    public event EventHandler<double>? VolumeChanged;
//    public event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;



//    #endregion

//    #region Properties

//    private DimmerPlaybackState _playbackState = DimmerPlaybackState.PlayCompleted;
//    public DimmerPlaybackState CurrentPlaybackState
//    {
//        get => _playbackState;
//        private set => SetProperty(ref _playbackState, value);
//    }

//    public bool IsPlaying => CurrentPlaybackState == DimmerPlaybackState.Playing;

//    private double _duration;
//    public double Duration
//    {
//        get => _duration;
//        private set
//        {
//            if (SetProperty(ref _duration, value))
//            {
//                DurationChanged?.Invoke(this, value);

//                if (IsPlaying || CurrentPlaybackState == DimmerPlaybackState.PausedDimmer)
//                {
//                    RaiseIsPlayingChanged();
//                }
//            }
//        }
//    }
//    private double _currentPositionValue;
//    private readonly BehaviorSubject<double> _currPositionBS = new(0);

//    public IObservable<double> CurrPositionObs => _currPositionBS.AsObservable();
//    public double CurrentPosition
//    {
//        get => _currentPositionValue;
//        private set
//        {
//            if ((Math.Abs(_currentPositionValue - value) > 0.1 || Math.Abs(value) < 0.0001 || Math.Abs(value - Duration) < 0.0001))
//            {
//                _currPositionBS.OnNext(value);
//                if (SetProperty(ref _currentPositionValue, value))
//                {
//                    PositionChanged?.Invoke(this, value);
//                }
//            }
//        }
//    }

//    private double _volume = 1.0;
//    public double Volume
//    {
//        get
//        {
//            if (_mediaPlayer is null)
//            {
//                return _volume;
//            }
//            else
//            {
//                return _mediaPlayer.Volume;
//            }

//        }

//        set
//        {
//            var clampedValue = Math.Clamp(value, 0.0, 1.0);
//            if (Math.Abs(_mediaPlayer.Volume - clampedValue) > 0.001)
//            {
//                _mediaPlayer.Volume = clampedValue;

//                SetProperty(ref _volume, clampedValue, nameof(Volume));
//            }
//        }
//    }
//    private readonly BehaviorSubject<bool?> _isMutedObs = new(false);

//    public IObservable<bool?> IsMutedObs => _isMutedObs.AsObservable();

//    private bool _isMuted;
//    public bool Muted
//    {
//        get
//        {
//            return _mediaPlayer.IsMuted;
//        }

//        set
//        {
//            if (_mediaPlayer.IsMuted != value)
//            {
//                _mediaPlayer.IsMuted = value;
//                _isMutedObs.OnNext(value);
//                SetProperty(ref _isMuted, value, nameof(Muted));
//            }
//        }
//    }


//    private double _balance;
//    public double Balance
//    {
//        get => _balance;
//        set => SetProperty(ref _balance, Math.Clamp(value, -1.0, 1.0)); // Store value, but no effect yet
//    }

//    public SongModelView? CurrentTrackMetadata => _currentTrackMetadata;

//    private bool _isAmbienceEnabled = false;

//    private double _ambienceVolume = 0.5;
//    private SongModelView _nextSongInList;

//    public double AmbienceVolume
//    {
//        get => _ambienceVolume;
//        set
//        {
//            // Clamp and set
//            double clamped = Math.Clamp(value, 0.0, 1.0);
//            if (SetProperty(ref _ambienceVolume, clamped))
//            {
//                if (_ambiencePlayer != null)
//                {
//                    _ambiencePlayer.Volume = clamped;
//                }
//            }
//        }
//    }

//    public CoreAudioDevice DefaultAudioDevice { get; private set; }

//    public async Task InitializeAmbienceAsync(string filePath)
//    {
//        if (string.IsNullOrWhiteSpace(filePath) || !TaggingUtils.FileExists(filePath))
//            return;

//        try
//        {
//            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
//            var source = MediaSource.CreateFromStorageFile(file);

//            // Create item and set to player
//            _ambiencePlayer.Source = new MediaPlaybackItem(source);

//            Debug.WriteLine($"[AudioService] Ambience loaded: {filePath}");

//            // If music is already playing and ambience is enabled, start it immediately
//            if (IsPlaying && _isAmbienceEnabled)
//            {
//                _ambiencePlayer.PlayAsync();
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] Failed to load ambience: {ex.Message}");
//        }
//    }

//    public void ToggleAmbience(bool isEnabled)
//    {
//        _isAmbienceEnabled = isEnabled;

//        if (_ambiencePlayer.Source == null) return;

//        if (isEnabled && IsPlaying)
//        {
//            _ambiencePlayer.PlayAsync();
//        }
//        else
//        {
//            _ambiencePlayer.PauseAsync();
//        }
//    }

//    #endregion


//    public async Task SendNextSong(SongModelView nextSong)
//    {
//        _nextSongInList = nextSong;
//        //var mediaPBItem = await CreateMediaPlaybackItemAsync(nextSong);
//        //_playbackList.Items.Add(mediaPBItem);

//    }

//    #region Core Playback Methods (Async)

//    /// <summary>
//    /// Initializes the player with the specified track metadata and plays at the speficified position. Stops any current playback.
//    /// </summary>
//    /// <param name="metadata">The metadata of the track to load.</param>
//    /// <returns>Task indicating completion.</returns>
//    public async Task InitializeAsync(SongModelView songModel,double pos)
//    {

//        {
//            ThrowIfDisposed();
//            ArgumentNullException.ThrowIfNull(songModel);

//            _currentTrackMetadata = songModel;
//            _currentSong.OnNext(songModel);
//            OnPropertyChanged(nameof(CurrentTrackMetadata));


//            _mediaPlayer.PauseAsync();
//            _playbackList.Items.Clear();
//            Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer paused and source nulled.");


//            MediaPlaybackItem? mediaPlaybackItem = null;
//            bool success = false;

//            try
//            {
//                mediaPlaybackItem = await CreateMediaPlaybackItemAsync(songModel).ConfigureAwait(false);

//                if (mediaPlaybackItem != null)
//                {
//                    _playbackList.Items.Clear(); // Clear previous queue
//                    _playbackList.Items.Add(mediaPlaybackItem);
//                    success = true;

//                    if (pos > 0)
//                    {
//                        _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(pos);
//                    }
//                    _mediaPlayer.Source = _playbackList;
//                    _mediaPlayer.PlayAsync();
//                    Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer source SET for {SongTitle}. Waiting for MediaOpened", songModel.Title);
//                }
//                else
//                {
//                    Debug.WriteLine("[AudioService] InitializeAsync: CreateMediaPlaybackItemAsync returned null for {SongTitle}. Cannot set source.", songModel.Title);


//                }
//            }
//            catch (OperationCanceledException ex)
//            {
//                Debug.WriteLine($"[AudioService] InitializeAsync: Operation CANCELED while creating/setting source for {songModel.Title}. {ex.Message}");

//            }
//            catch(Exception ee)
//            {
//                Debug.WriteLine(ee.Message);
//            }
//            finally
//            {



//                if (!success)
//                {
//                    Debug.WriteLine("[AudioService] InitializeAsync: Finalizing with FAILED status for {SongTitle}.", songModel.Title);

//                    if (ReferenceEquals(_currentTrackMetadata, songModel))
//                    {
//                        _currentTrackMetadata = null;
//                        OnPropertyChanged(nameof(CurrentTrackMetadata));
//                    }
//                    UpdatePlaybackState(DimmerPlaybackState.Error);
//                    OnErrorOccurred($"Failed to initialize track: {songModel?.Title}", null);
//                }


//            }
//        }
//    }

//    /// <summary>
//    /// Starts or resumes playback.
//    /// </summary>
//    /// <returns>Task indicating completion.</returns>
//    public void PlayAsync(double pos)
//    {
//        ThrowIfDisposed();
//        if (_mediaPlayer.Source == null)
//        {
//            Debug.WriteLine("[AudioService] PlayAsync called but no source is set.");
//        }


//        try
//        {
//            Debug.WriteLine("[AudioService] PlayAsync executing.");
//            _mediaPlayer.PlayAsync();
//            _mediaPlayer.Position = TimeSpan.FromSeconds(pos);
//            if (_isAmbienceEnabled && _ambiencePlayer.Source != null)
//            {
//                _ambiencePlayer.PlayAsync();
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] Error calling PlayAsync(): {ex}");
//            OnErrorOccurred("Failed to start playback.", ex);
//            UpdatePlaybackState(DimmerPlaybackState.Error);
//        }
//    }

//    /// <summary>
//    /// Pauses playback.
//    /// </summary>
//    /// <returns>Task indicating completion.</returns>
//    public void PauseAsync()
//    {
//        ThrowIfDisposed();
//        if (_mediaPlayer.PlaybackSession.CanPause)
//        {

//            try
//            {
//                Debug.WriteLine("[AudioService] PauseAsync executing.");
//                _mediaPlayer.PauseAsync();
//                if (_ambiencePlayer.PlaybackSession.CanPause)
//                {
//                    _ambiencePlayer.PauseAsync();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[AudioService] Error calling PauseAsync(): {ex}");

//                OnErrorOccurred("Failed to pause playback.", ex);
//            }
//        }
//        else
//        {
//            Debug.WriteLine("[AudioService] PauseAsync called but cannot pause in current state.");
//        }
//    }

//    /// <summary>
//    /// Stops playback, resets position, and clears the current source.
//    /// </summary>
//    /// <returns>Task indicating completion.</returns>
//    public void Stop()
//    {
//        try
//        {

//        ThrowIfDisposed();
//        Debug.WriteLine("[AudioService] StopAsync executing.");
//        _mediaPlayer.PauseAsync();
//        _mediaPlayer.Source = null;
//        _currentTrackMetadata = null;
//        OnPropertyChanged(nameof(CurrentTrackMetadata));
//        CurrentPosition = 0;
//        Duration = 0;
//        UpdatePlaybackState(DimmerPlaybackState.PausedDimmer);

//            _ambiencePlayer.PauseAsync();
//            _initializationCts?.Cancel();
//        _initializationCts?.Dispose();
//        _initializationCts = null;

//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine(ex.Message);
//        }
//    }

//    /// <summary>
//    /// Seeks to the specified position in seconds.
//    /// </summary>
//    /// <param name="positionSeconds">The target position in seconds.</param>
//    /// <returns>Task indicating completion of the seek request (not necessarily the completion of the seek operation itself).</returns>
//    public void SeekAsync(double positionSeconds)
//    {
//        try
//        {

//        ThrowIfDisposed(); 

//        if (_mediaPlayer.PlaybackSession.CanSeek)
//        {

//            var targetPositionSeconds = Math.Clamp(positionSeconds, 0, _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds);
//            var targetPosition = TimeSpan.FromSeconds(targetPositionSeconds);


//            if (Math.Abs(_mediaPlayer.PlaybackSession.Position.TotalSeconds - targetPosition.TotalSeconds) > 0.2)
//            {

//                _requestedSeekPosition = targetPositionSeconds;

//                Debug.WriteLine($"[AudioService] Storing requested position ({_requestedSeekPosition}) and seeking to: {targetPosition}");


//                _mediaPlayer.PlaybackSession.Position = targetPosition;
//            }
//        }
//        else
//        {

//            CurrentPosition = positionSeconds;
//            Debug.WriteLine("[AudioService] SeekAsync requested but session cannot seek.");
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine(ex.Message);
//        }
//    }

//    #endregion

//    #region Media Item Creation

//    private static async Task<MediaPlaybackItem?> CreateMediaPlaybackItemAsync(SongModelView media, CancellationToken token = default)
//    {


//        if (string.IsNullOrWhiteSpace(media.FilePath))
//        {
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: No FilePath for '{media.Title ?? "Unknown"}', cannot create item.");
//            return null;
//        }

//        Uri? uri = null;
//        StorageFile? storageFile = null;

//        try
//        {

//            if (Uri.TryCreate(media.FilePath, UriKind.Absolute, out var parsedUri) && !parsedUri.IsFile)
//            {
//                uri = parsedUri;
//                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Using direct URI: {uri} for '{media.Title}'");
//            }
//            else
//            {
//                string fullPath = Path.GetFullPath(media.FilePath);


//                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Attempting StorageFile for path: {fullPath} for '{media.Title}'");
//                storageFile = await StorageFile.GetFileFromPathAsync(fullPath).AsTask(token);
//            }

//            if(token.IsCancellationRequested)
//            {
//                return null;
//            }
//            MediaSource? mediaSource;
//            if (storageFile != null)
//            {

//                mediaSource = MediaSource.CreateFromStorageFile(storageFile);
//                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from StorageFile for '{media.Title}'. ContentType: {storageFile.ContentType}");
//            }
//            else if (uri != null)
//            {
//                mediaSource = MediaSource.CreateFromUri(uri);
//                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from URI for '{media.Title}'.");
//            }
//            else
//            {

//                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Could not determine how to create MediaSource for '{media.Title}'.");
//                return null;
//            }


//            var mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
//            var props = mediaPlaybackItem.GetDisplayProperties();
//            props.Type = MediaPlaybackType.Music;

//            props.MusicProperties.Title = media.Title ?? Path.GetFileNameWithoutExtension(media.FilePath) ?? "Unknown Title";
//            props.MusicProperties.Artist = media.OtherArtistsName.ToString();
//            props.MusicProperties.AlbumTitle = media.AlbumName ?? string.Empty;
//            props.MusicProperties.AlbumArtist = media.OtherArtistsName.ToString();

//            if (!string.IsNullOrEmpty(media.CoverImagePath) && File.Exists(media.CoverImagePath))
//            {
//                try
//                {

//                    var coverFile = await StorageFile.GetFileFromPathAsync(media.CoverImagePath);

//                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(coverFile);
//                    Debug.WriteLine($"[AudioService] Successfully created thumbnail reference for '{media.Title}'.");
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[AudioService] Error creating thumbnail for '{media.Title}' from path '{media.CoverImagePath}': {ex.Message}");
//                    // Optionally, set a default placeholder image here
//                }
//            }
//            else
//            {
//                Debug.WriteLine($"[AudioService] Cover image path is missing or file does not exist for '{media.Title}'. Path: '{media.CoverImagePath}'");
//                // Optionally, set a default placeholder image here
//            }
//            mediaPlaybackItem.ApplyDisplayProperties(props);
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Successfully created MediaPlaybackItem for '{media.Title}'.");
//            return mediaPlaybackItem;

//        }
//        catch (OperationCanceledException)
//        {
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Operation CANCELED for '{media.Title ?? media.FilePath}'.");
//            throw;
//        }
//        catch (FileNotFoundException fnfEx)
//        {
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: File not found for '{media.FilePath}': {fnfEx.Message}");
//            return null;
//        }
//        catch (UnauthorizedAccessException uaEx)
//        {
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Access denied for '{media.FilePath}': {uaEx.Message}. Check capabilities (e.g., broadFileSystemAccess) or file permissions.");
//            return null;
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Generic error creating MediaSource for '{media.FilePath}': {ex.Message}");
//            return null;
//        }
//    }

//    #endregion

//    #region Player Event Handlers

//    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
//    {
//        MediaPlaybackState winuiState = sender.PlaybackState;
//        var newState = ConvertPlaybackState(winuiState);
//        Debug.WriteLine($"[AudioService] PlaybackStateChanged: {winuiState} -> {newState}");
//        if (newState.Item2)
//        {
//            UpdatePlaybackState(newState.Item1);
//        }
//    }

//    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
//    {
//        CurrentPosition = sender.Position.TotalSeconds;
//    }

//    private void PlaybackSession_NaturalDurationChanged(MediaPlaybackSession sender, object args)
//    {
//        var newDuration = sender.NaturalDuration.TotalSeconds;

//        if (newDuration > 0)
//        {
//            Debug.WriteLine($"[AudioService] NaturalDurationChanged: {newDuration}");
//            Duration = newDuration;
//        }
//    }

//    private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
//    {

//        if (_requestedSeekPosition >= 0)
//        {
//            var confirmedPosition = _requestedSeekPosition;
//            _requestedSeekPosition = -1; // Reset for the next operation

//            // This debug line will now show the CORRECT value
//            Debug.WriteLine($"[AudioService] PlaybackSession_SeekCompleted fired. Using confirmed position: {confirmedPosition}");

//            // Update your service's internal state
//            CurrentPosition = confirmedPosition;

//            // Invoke your custom event with the RELIABLE data
//            SeekCompleted?.Invoke(this, confirmedPosition);
//        }
//        else
//        {
//            // This might happen if the player seeks for its own reasons (e.g., buffering).
//            // You can decide if you want to handle this or just log it.
//            Debug.WriteLine($"[AudioService] PlaybackSession_SeekCompleted fired unexpectedly. Sender position: {sender.Position.TotalSeconds}");
//        }
//    }

//    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
//    {

//        Debug.WriteLine($"[AudioService] MediaOpened: {_currentTrackMetadata?.Title ?? "Unknown"}");
//        Duration = sender.PlaybackSession.NaturalDuration.TotalSeconds;
//        CurrentPosition = sender.PlaybackSession.Position.TotalSeconds;
//        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.Playing };
//        _playStarted?.Invoke(this, eventArgs);

//    }

//    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
//    {

//        Debug.WriteLine($"[AudioService] MediaEnded: {_currentTrackMetadata?.Title ?? "Unknown"}");
//        _ambiencePlayer.PauseAsync();
//        CurrentPosition = Duration;
//        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);


//        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayCompleted };
//        _playEnded?.Invoke(this, eventArgs);

//    }
//        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
//    {
//        Debug.WriteLine($"[AudioService] MediaFailed: Error={args.Error}, Code={args.ExtendedErrorCode}, Msg={args.ErrorMessage}");
//        OnErrorOccurred($"Playback failed: {args.ErrorMessage}", args.ExtendedErrorCode, args.Error);


//        _currentTrackMetadata = null;
//        OnPropertyChanged(nameof(CurrentTrackMetadata));
//        UpdatePlaybackState(DimmerPlaybackState.Error);
//        CurrentPosition = 0;
//        Duration = 0;
//    }

//    #endregion

//    #region SMTC Command Handlers

//    private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
//    {
//        Debug.WriteLine("[AudioService] SMTC PlayAsync Received");

//        var deferral = args.GetDeferral();
//        try
//        {
//            if (_mediaPlayer.Source != null)
//            {
//                PlayAsync(CurrentPosition);
//                args.Handled = true;
//            }
//            else
//            {
//                args.Handled = false;
//            }
//        }
//        finally
//        {
//            deferral.Complete();
//        }
//    }

//    private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
//    {
//        Debug.WriteLine("[AudioService] SMTC PauseAsync Received");
//        var deferral = args.GetDeferral();
//        try
//        {
//            if (_mediaPlayer.PlaybackSession.CanPause)
//            {
//                PauseAsync();
//                args.Handled = true;
//            }
//            else
//            {
//                args.Handled = false;
//            }
//        }
//        finally
//        {
//            deferral.Complete();
//        }
//    }

//    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
//    {
//        Debug.WriteLine("[AudioService] SMTC Next Received");

//        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType= DimmerPlaybackState.PlayNextUser };

//        MediaKeyNextPressed?.Invoke(this, eventArgs);
//        args.Handled = true;
//    }

//    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
//    {
//        Debug.WriteLine("[AudioService] SMTC Previous Received");

//        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayPreviousUser };
//        MediaKeyPreviousPressed?.Invoke(this, eventArgs);
//        args.Handled = true;
//    }

//    #endregion

//    #region Audio Output Management

//    /// <summary>
//    /// Gets a list of available audio output devices.
//    /// </summary>
//    public async Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
//    {
//        ThrowIfDisposed();

//        var outputDevices = new List<AudioOutputDevice>();
//        try
//        {

//            string selector = MediaDevice.GetAudioRenderSelector();
//            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);

//            foreach (var device in devices)
//            {
//                outputDevices.Add(new AudioOutputDevice { Id = device.Id, Name = device.Name });
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] Error getting audio output devices: {ex}");
//            OnErrorOccurred("Failed to enumerate audio output devices.", ex);
//        }
//        return outputDevices;
//    }

//    /// <summary>
//    /// Sets the audio output device for the MediaPlayer.
//    /// </summary>
//    /// <param name="deviceId">The ID of the device to use, or null to use the system default.</param>
//    public async Task SetAudioOutputDeviceAsync(string? deviceId)
//    {
//        ThrowIfDisposed();
//        try
//        {
//            DeviceInformation? deviceInfo = null;
//            if (!string.IsNullOrEmpty(deviceId))
//            {
//                deviceInfo = await DeviceInformation.CreateFromIdAsync(deviceId);
//            }


//            _mediaPlayer.AudioDevice = deviceInfo;
//            _ambiencePlayer.AudioDevice = deviceInfo;
//            _currentAudioDeviceId = deviceInfo?.Id;
//            Debug.WriteLine($"[AudioService] Audio output device set to: {deviceInfo?.Name ?? "System Default"} (ID: {_currentAudioDeviceId})");
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"[AudioService] Error setting audio output device (ID: {deviceId}): {ex}");
//            OnErrorOccurred($"Failed to set audio output device to {deviceId}.", ex);
//        }
//    }


//    private async void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
//    {
//        Debug.WriteLine($"[AudioService] System default audio render device changed. Role: {args.Role}, New ID: {args.Id}");









//        if (!string.IsNullOrEmpty(_currentAudioDeviceId) && _currentAudioDeviceId != args.Id)
//        {

//            try
//            {
//                var currentDevice = await DeviceInformation.CreateFromIdAsync(_currentAudioDeviceId);

//                Debug.WriteLine($"[AudioService] Still using explicitly selected device: {currentDevice.Name}");
//            }
//            catch
//            {

//                Debug.WriteLine($"[AudioService] Previously selected device ID {_currentAudioDeviceId} is no longer valid. Resetting to default.");
//                await SetAudioOutputDeviceAsync(null);
//            }
//        }
//        else if (string.IsNullOrEmpty(_currentAudioDeviceId))
//        {

//            Debug.WriteLine("[AudioService] Using system default, MediaPlayer should adapt.");
//        }
//    }


//    #endregion

//    #region State Management & Helpers

//    private void UpdatePlaybackState(DimmerPlaybackState newState)
//    {

//        if (SetProperty(ref _playbackState, newState, nameof(CurrentPlaybackState)))
//        {

//            OnPropertyChanged(nameof(IsPlaying));



//            var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  newState };
//            PlaybackStateChanged?.Invoke(this, args);


//            RaiseIsPlayingChanged();

//        }
//    }


//    private static (DimmerPlaybackState, bool) ConvertPlaybackState(MediaPlaybackState state)
//    {
//        switch (state)
//        {
//            case MediaPlaybackState.None:
//                return (DimmerPlaybackState.None, false);

//            case MediaPlaybackState.Opening:
//                return (DimmerPlaybackState.Opening, false);
//            case MediaPlaybackState.Buffering:
//                return (DimmerPlaybackState.Buffering, false);
//            case MediaPlaybackState.Playing:
//                return (DimmerPlaybackState.Playing, true);
//            case MediaPlaybackState.Paused:
//                return (DimmerPlaybackState.PausedDimmer, true);
//            default:
//                return (DimmerPlaybackState.PlayCompleted, true);
//        }

//    }

//    private void RaiseIsPlayingChanged()
//    {
//        // Use current state to construct the event args
//        DimmerPlaybackState eventType = IsPlaying ? DimmerPlaybackState.Playing : DimmerPlaybackState.PausedDimmer;

//        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  eventType };
//        _isPlayingChanged?.Invoke(this, args);
//    }

//    private void OnErrorOccurred(string message, Exception? exception = null, MediaPlayerError? playerError = null)
//    {

//        Debug.WriteLine($"[AudioService ERROR] {message} | Exception: {exception?.Message} | PlayerError: {playerError}");

//        DimmerPlaybackState dimmerPBError =DimmerPlaybackState.Error;
//        if(playerError is not null)
//        {
//            dimmerPBError = playerError.Value == MediaPlayerError.SourceNotSupported ? DimmerPlaybackState.ErrorAudioSourceNotSupported : DimmerPlaybackState.Error;
//        }
//        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType= dimmerPBError };
//        ErrorOccurred?.Invoke(this, args);
//    }


//    #endregion


//    #region INotifyPropertyChanged

//    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
//    {
//        if (EqualityComparer<T>.Default.Equals(backingStore, value))
//            return false;

//        backingStore = value;


//        _dispatcherQueue.TryEnqueue(() =>
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        });
//        return true;
//    }


//    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
//    {
//        _dispatcherQueue.TryEnqueue(() =>
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        });
//    }

//    #endregion

//    #region IAsyncDisposable

//    private void ThrowIfDisposed()
//    {
//        if (_isDisposed)
//        {
//            throw new ObjectDisposedException(nameof(AudioService));
//        }
//    }

//    public async ValueTask DisposeAsync()
//    {
//        if (_isDisposed)
//        {
//            return;
//        }
//        _isDisposed = true;

//        Debug.WriteLine("[AudioService] Starting asynchronous disposal...");


//        MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;


//        if (_initializationCts is not null)
//        {
//            await _initializationCts.CancelAsync();
//        }
//        _initializationCts?.Dispose();
//        _initializationCts = null;


//        _mediaPlayer?.PauseAsync();
//        _mediaPlayer?.Source = null;

//        _controller?.Dispose();

//        UnsubscribeFromPlayerEvents();
//        UnsubscribeFromSystemEvents();
//        try
//        {
//            _ambiencePlayer?.PauseAsync();
//            _ambiencePlayer?.Source = null;
//            _ambiencePlayer?.Dispose();
//        }
//        catch { /* Ignore ambience dispose errors */ }


//        _mediaPlayer?.Dispose();
//        Debug.WriteLine("[AudioService] MediaPlayer disposed.");


//        _isPlayingChanged = null;
//        _playEnded = null;
//        _playStarted = null;
//        PlaybackStateChanged = null;
//        ErrorOccurred = null;
//        DurationChanged = null;
//        PositionChanged = null;
//        SeekCompleted = null;
//        MediaKeyNextPressed = null;
//        MediaKeyPreviousPressed = null;
//        PropertyChanged = null;

//        Debug.WriteLine("[AudioService] Asynchronous disposal complete.");


//        await Task.CompletedTask;
//    }



//    #endregion

//    /// <summary>
//    /// Copies data from a regular Stream to an IRandomAccessStream.
//    /// </summary>
//    /// <param name="fileStream">The source stream.</param>
//    /// <param name="randomAccessStream">The target random access stream.</param>
//    /// <param name="token">A cancellation token.</param>
//    /// <param name="progressHandler">A progress handler reporting the number of bytes copied.</param>
//    public static async Task CopyFileStreamToRandomAccessStreamAsync(Stream fileStream, IRandomAccessStream randomAccessStream, CancellationToken token, IProgress<long> progressHandler)
//    {

//        using (Stream outputStream = randomAccessStream.GetOutputStreamAt(0).AsStreamForWrite())
//        {

//            const int bufferSize = 81920;
//            byte[] buffer = new byte[bufferSize];
//            long totalBytesCopied = 0;
//            int bytesRead;


//            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
//            {
//                await outputStream.WriteAsync(buffer, 0, bytesRead, token);
//                totalBytesCopied += bytesRead;
//                progressHandler?.Report(totalBytesCopied);
//            }


//            await outputStream.FlushAsync(token);
//            token.ThrowIfCancellationRequested();
//        }
//    }

//    private readonly object _lockObject = new object();





//    public bool SetPreferredOutputDevice(AudioOutputDevice dev)
//    {
//        if (dev?.Id == null)
//            return false;

//        try
//        {
//            if (dev?.Id == null)
//                return false;

//            try
//            {
//                // The library works with its own device objects. Get it by its ID.
//                // The ID from NAudio is compatible.
//                var deviceToSet = _controller.GetDevice(new Guid(dev.Id));

//                if (deviceToSet == null)
//                {
//                    Debug.WriteLine($"Device with ID {dev.Id} not found by AudioSwitcher.");
//                    return false;
//                }

//                // This one line does it all. It's clean, safe, and readable.
//                deviceToSet.SetAsDefault();
//                // You can also set the communications default separately if needed
//                deviceToSet.SetAsDefaultCommunications();

//                Debug.WriteLine($"Successfully set default audio output device to: {dev.Name}");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error setting default audio device: {ex.Message}");
//                return false;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine($"Error in SetPreferredOutputDevice: {ex.Message}");
//            return false;
//        }
//    }

//    /// <summary>
//    /// Gets a list of all active audio output devices using AudioSwitcher for consistency.
//    /// </summary>
//    public List<AudioOutputDevice> GetAllAudioDevices()
//    {
//        // Get all active playback devices from the controller.
//        //var devices = _audioController.GetPla ybackDevices(AudioSwitcher.AudioApi.DeviceState.Active);
//        IEnumerable<CoreAudioDevice>? devices = _controller.GetPlaybackDevices(DeviceState.Active)
//            . Where(x=>x.DeviceType == DeviceType.Playback);

//        // Map them to your own simple model.
//        return devices.Select(d => new AudioOutputDevice
//        {
//            // Note: The library provides the ID as a Guid. Convert to string.
//            Id = d.Id.ToString(),
//            Name = d.FullName,
//            Type = d.DeviceType.ToString(),
//            ProductName = d.InterfaceName,
//            IsPlaybackDevice=d.IsPlaybackDevice,
//            IconString=d.Icon.ToString(),
//            State=d.State.ToString(),
//            Volume= d.Volume,
//            IsMuted=d.IsMuted,
//            IsDefaultCommunicationsDevice=d.IsDefaultCommunicationsDevice,
//            IsDefaultDevice=d.IsDefaultDevice,

//        }).ToList();
//    }


//    public void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels)
//    {
//        try
//        {

//            Task.Run(async () => await InitializeAsync(songModelView,0));
//        }
//        catch (Exception ex)
//        {
//            Debug.WriteLine(ex.Message);
//        }
//    }

//}

