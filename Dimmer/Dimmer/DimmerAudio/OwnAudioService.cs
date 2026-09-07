

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
    #region Media Key Triggers (Called by Adapter)

    public void TriggerNext()
    {
        // Fires the event so your ViewModel/App knows to play the next song
        MediaKeyNextPressed?.Invoke(this, new PlaybackEventArgs(_currentSong.Value));
    }

    public void TriggerPrevious()
    {
        MediaKeyPreviousPressed?.Invoke(this, new PlaybackEventArgs(_currentSong.Value));
    }

    #endregion

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


