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

public partial class OwnAudioService : IDimmerAudioService
{
    private readonly CompositeDisposable _disposables = new();

    // ==========================================================
    // REACTIVE STATE (BehaviorSubjects hold current value)
    // ==========================================================
    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);
    private readonly BehaviorSubject<DimmerPlaybackState> _playbackState = new(DimmerPlaybackState.None);
    private readonly BehaviorSubject<double> _currentPosition = new(0);
    private readonly BehaviorSubject<double> _duration = new(0);
    private readonly BehaviorSubject<double> _volume = new(1.0);
    private readonly BehaviorSubject<AudioOutputDevice?> _currentDevice = new(null);
    private readonly BehaviorSubject<float[]> _eqBands = new(new float[10]);

    // ==========================================================
    // REACTIVE EVENTS (Subjects are for one-time triggers)
    // ==========================================================
    private readonly Subject<SongModelView> _playEnded = new();
    private readonly Subject<double> _seekCompleted = new();
    private readonly Subject<Exception> _errors = new();
    private readonly Subject<SongModelView> _nextRequested = new();
    private readonly Subject<SongModelView> _prevRequested = new();
    private readonly Subject<SongModelView> _favRequested = new();

    // ==========================================================
    // EXPOSED OBSERVABLES
    // ==========================================================
    public IObservable<SongModelView?> CurrentSongObs => _currentSong.AsObservable();
    public IObservable<DimmerPlaybackState> PlaybackStateObs => _playbackState.AsObservable();
    public IObservable<double> PositionObs => _currentPosition.AsObservable();
    public IObservable<double> DurationObs => _duration.AsObservable();
    public IObservable<double> VolumeObs => _volume.AsObservable();
    public IObservable<AudioOutputDevice?> CurrentDeviceObs => _currentDevice.AsObservable();
    public IObservable<float[]> EqBandsObs => _eqBands.AsObservable();

    public IObservable<SongModelView> PlayEndedObs => _playEnded.AsObservable();
    public IObservable<double> SeekCompletedObs => _seekCompleted.AsObservable();
    public IObservable<Exception> ErrorObs => _errors.AsObservable();

    public IObservable<SongModelView> NextRequestedObs => _nextRequested.AsObservable();
    public IObservable<SongModelView> PreviousRequestedObs => _prevRequested.AsObservable();
    public IObservable<SongModelView> FavoriteRequestedObs => _favRequested.AsObservable();

    // ==========================================================
    // INTERNAL ENGINE STATE
    // ==========================================================
    private AudioMixer? _mixer;
    private FileSource? _mainSource;
    private FileSource? _secondarySource; // For DJ crossfade
    private FileSource? _ambienceSource;
    private readonly SemaphoreSlim _transportLock = new(1, 1);
    private readonly Stopwatch _watch = new();

    private double _lastEnginePos;
    private double _lastEnginePosAt;
    private bool _isDisposed;
    private bool _isAmbienceEnabled;
    private double _ambienceVolume = 0.5;

    // Effects
    private EqualizerEffect? _eqEffect;
    private ReverbEffect? _reverbEffect;
    private CompressorEffect? _compressorEffect;
    private SmartMasterEffect? _smartMasterEffect;
    private float _currentPitchSemitones = 0f;
    private float _currentTempoRatio = 1f;

    // Properties
    public SongModelView? CurrentTrackMetadata => _currentSong.Value;
    public bool IsPlaying => _playbackState.Value == DimmerPlaybackState.Playing;
    public double CurrentPosition => _currentPosition.Value;
    public double Duration => _duration.Value;
    public IEnumerable<AudioOutputDevice>? PlaybackDevices { get; private set; }

    public double AmbienceVolume
    {
        get => _ambienceVolume;
        set
        {
            _ambienceVolume = Math.Clamp(value, 0.0, 1.0);
            if (_ambienceSource != null) _ambienceSource.Volume = (float)_ambienceVolume;
        }
    }

    public OwnAudioService()
    {
        // Smooth UI Polling (60fps equivalent) - only ticks when playing
        Observable.Interval(TimeSpan.FromMilliseconds(16))
            .Where(_ => IsPlaying)
            .Subscribe(_ => UpdatePositionFromEngine())
            .DisposeWith(_disposables);

        // Keep watch timer in sync with Playback State
        _playbackState
            .Subscribe(state =>
            {
                if (state == DimmerPlaybackState.Playing) _watch.Start();
                else _watch.Stop();
            })
            .DisposeWith(_disposables);
    }

    // ==========================================================
    // INITIALIZATION & DEVICE ROUTING
    // ==========================================================
    public async Task InitializeEngineAsync(string? outputDeviceId = null)
    {
        try
        {
            var config = OwnaudioNet.CreateDefaultConfig();
            config.EnableInput = false;
            config.OutputDeviceId = outputDeviceId;

            // MAGIC: This forces the Rust engine to automatically switch to the phone speaker 
            // if Bluetooth headphones disconnect, preventing a crash!
            config.FallbackToDefaultOnDisconnect = true;

            await OwnaudioNet.InitializeAsync(config);
            OwnaudioNet.Start();

            _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 1024);

            _compressorEffect = new CompressorEffect { Enabled = false };
            _eqEffect = new EqualizerEffect { Enabled = false };
            _reverbEffect = new ReverbEffect { Enabled = false };
            _smartMasterEffect = new SmartMasterEffect { Enabled = false };

            _mixer.AddMasterEffect(_compressorEffect);
            _mixer.AddMasterEffect(_eqEffect);
            _mixer.AddMasterEffect(_reverbEffect);
            _mixer.AddMasterEffect(_smartMasterEffect);

            _mixer.PlaybackEnded += (s, e) =>
            {
                if (_currentSong.Value != null)
                {
                    _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
                    _playEnded.OnNext(_currentSong.Value);
                }
            };

            _mixer.Start();
            _mixer.MasterVolume = (float)_volume.Value;

            // Fetch devices to update UI state
            await GetAllAudioDevicesAsync();

            Debug.WriteLine("[OwnAudioService] Engine Initialized Successfully.");
        }
        catch (Exception ex)
        {
            _errors.OnNext(ex);
        }
    }

    public async Task<List<AudioOutputDevice>?> GetAllAudioDevicesAsync()
    {
        var outputDevs = await OwnaudioNet.GetOutputDevicesAsync();
        var devices = outputDevs.Select(d => new AudioOutputDevice
        {
            Id = d.DeviceId,
            Name = d.Name,
            IsDefaultDevice = d.IsDefault,
            ProductName = d.EngineName
        }).ToList();

        PlaybackDevices = devices;

        // Update current device subject
        var current = devices.FirstOrDefault(d => d.Id == OwnaudioNet.Engine?.Config.OutputDeviceId)
                      ?? devices.FirstOrDefault(d => d.IsDefaultDevice);

        _currentDevice.OnNext(current);
        return devices;
    }

    public bool SetPreferredOutputDevice(AudioOutputDevice dev)
    {
        if (dev.Id == _currentDevice.Value?.Id) return true;

        Task.Run(async () =>
        {
            await _transportLock.WaitAsync();
            try
            {
                var wasPlaying = IsPlaying;
                var currentPos = _currentPosition.Value;

                _mixer?.Stop();
                _mixer?.Dispose();
                _mixer = null;

                await OwnaudioNet.ShutdownAsync();
                await InitializeEngineAsync(dev.Id);

                if (_mainSource != null)
                {
                    _mixer?.AddSourcePrepared(_mainSource);
                    _mixer?.StartPreparedSources(currentPos);
                    if (wasPlaying) _mixer?.Start();
                }
            }
            finally { _transportLock.Release(); }
        });
        return true;
    }

    // ==========================================================
    // CORE PLAYBACK
    // ==========================================================
    public async Task InitializeAsync(SongModelView songModel, double pos)
    {
        ArgumentNullException.ThrowIfNull(songModel);
        await _transportLock.WaitAsync();
        try
        {
            _playbackState.OnNext(DimmerPlaybackState.Opening);
            _currentSong.OnNext(songModel);

            if (_mainSource != null)
            {
                _mixer?.RemoveSource(_mainSource.Id);
                _mainSource.Dispose();
            }

            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            _mainSource = new FileSource(songModel.FilePath, targetSampleRate: sr, targetChannels: ch);
            _mainSource.SetPitchSmooth(_currentPitchSemitones);
            _mainSource.SetTempoSmooth(_currentTempoRatio);

            _duration.OnNext(_mainSource.Duration);

            _mixer?.Pause();
            _mixer?.Seek(pos);
            _mixer?.AddSourcePrepared(_mainSource);
            _mixer?.StartPreparedSources(startPosition: pos);
            _mixer?.Start();

            _playbackState.OnNext(DimmerPlaybackState.Playing);
        }
        catch (Exception ex) { _errors.OnNext(ex); }
        finally { _transportLock.Release(); }
    }

    public async Task PlayAsync(double pos = -1)
    {
        await _transportLock.WaitAsync();
        try
        {
            if (_mainSource != null)
            {
                if (pos >= 0)
                {
                    _mixer?.Seek(pos);
                    _mainSource.Seek(pos);
                }
                _mainSource.Play();
                _mixer?.Start();
                _playbackState.OnNext(DimmerPlaybackState.Playing);

                if (_isAmbienceEnabled) _ambienceSource?.Play();
            }
        }
        catch (Exception ex) { _errors.OnNext(ex); }
        finally { _transportLock.Release(); }
    }

    public async Task PauseAsync()
    {
        await _transportLock.WaitAsync();
        try
        {
            _mainSource?.Pause();
            _ambienceSource?.Pause();
            _mixer?.Pause();
            _playbackState.OnNext(DimmerPlaybackState.PausedUser);
        }
        catch (Exception ex) { _errors.OnNext(ex); }
        finally { _transportLock.Release(); }
    }

    public async Task SeekAsync(double positionSeconds)
    {
        await _transportLock.WaitAsync();
        try
        {
            if (_mainSource != null && _mixer != null)
            {
                var safePos = Math.Clamp(positionSeconds, 0, _duration.Value);
                _mixer.Seek(safePos);
                _mainSource.Seek(safePos);

                _lastEnginePos = safePos;
                _currentPosition.OnNext(safePos);
                _seekCompleted.OnNext(safePos);
            }
        }
        finally { _transportLock.Release(); }
    }

    public void Stop()
    {
        _mainSource?.Stop();
        _ambienceSource?.Stop();
        _currentPosition.OnNext(0);
        _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
    }

    private void UpdatePositionFromEngine()
    {
        if (_mixer == null || _mainSource == null || _mainSource.IsEndOfStream) return;

        double enginePos = _mixer.MasterClock.CurrentTimestamp;
        double now = _watch.Elapsed.TotalSeconds;

        if (Math.Abs(enginePos - _lastEnginePos) > 0.001)
        {
            _lastEnginePos = enginePos;
            _lastEnginePosAt = now;
        }

        double smoothPos = _lastEnginePos + (now - _lastEnginePosAt);
        _currentPosition.OnNext(Math.Clamp(smoothPos, 0, _duration.Value));
    }


    public double Volume
    {
        get => _volume.Value;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (_mixer != null) _mixer.MasterVolume = (float)clamped;
            _volume.OnNext(clamped);
        }
    }

    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        return _currentDevice.Value;
    }

    public async Task SendNextSong(SongModelView nextSong)
    {
        // This is a stub for old Gapless/Preload logic.
        
        await Task.CompletedTask;
    }

    // ==========================================================
    // AUDIO EFFECTS & TWEAKS
    // ==========================================================
    public void SetPlaybackMode(PlaybackModeEnum mode, float customReverbMix = 0.15f, float customRoomSize = 0.35f)
    {
        switch (mode)
        {
            case PlaybackModeEnum.Normal:
                SetPitchAndSpeed(0f, 1f);
                EnableReverb(false);
                break;

            case PlaybackModeEnum.Nightcore:
                SetPitchAndSpeed(3f, 1.25f);
                EnableReverb(false);
                break;

            case PlaybackModeEnum.Slowed: // Clean slowed
                SetPitchAndSpeed(-3f, 0.85f);
                EnableReverb(false);
                break;

            case PlaybackModeEnum.SlowedAndReverb: // Tasteful slowed + reverb
                SetPitchAndSpeed(-3f, 0.85f);
                EnableReverb(true, roomSize: customRoomSize, mix: customReverbMix);
                break;
        }
    }

    public void SetPitchAndSpeed(float pitchSemitones, float tempoRatio)
    {
        _currentPitchSemitones = Math.Clamp(pitchSemitones, -12f, 12f);
        _currentTempoRatio = Math.Clamp(tempoRatio, 0.5f, 2.0f);
        _mainSource?.SetPitchSmooth(_currentPitchSemitones);
        _mainSource?.SetTempoSmooth(_currentTempoRatio);
    }

    public void EnableEqualizer(bool enable) { if (_eqEffect != null) _eqEffect.Enabled = enable; }
    public void SetEqualizerPreset(EqualizerPreset preset) => _eqEffect?.SetPreset(preset);

    public void ChangeEqBand(int bandIndex, float gainDb)
    {
        if (_eqEffect == null || bandIndex < 0 || bandIndex > 9) return;

        // Reflection/Switch wrapper based on your underlying engine logic
        SetEqBandInternal(bandIndex, gainDb);

        var currentBands = _eqBands.Value;
        currentBands[bandIndex] = gainDb;
        _eqBands.OnNext(currentBands);
    }

    private void SetEqBandInternal(int band, float gain)
    {
        if (_eqEffect == null) return;
        switch (band)
        {
            case 0: _eqEffect.Band0Gain = gain; break;
            case 1: _eqEffect.Band1Gain = gain; break;
            case 2: _eqEffect.Band2Gain = gain; break;
            case 3: _eqEffect.Band3Gain = gain; break;
            case 4: _eqEffect.Band4Gain = gain; break;
            case 5: _eqEffect.Band5Gain = gain; break;
            case 6: _eqEffect.Band6Gain = gain; break;
            case 7: _eqEffect.Band7Gain = gain; break;
            case 8: _eqEffect.Band8Gain = gain; break;
            case 9: _eqEffect.Band9Gain = gain; break;
        }
    }

    public void EnableReverb(bool enable, float roomSize = 0.35f, float mix = 0.15f)
    {
        if (_reverbEffect != null)
        {
            _reverbEffect.RoomSize = roomSize;
            _reverbEffect.Mix = mix;
            _reverbEffect.Enabled = enable;
        }
    }

    public void EnableCompressor(bool enable, CompressorPreset preset = CompressorPreset.VocalGentle)
    {
        if (_compressorEffect != null)
        {
            _compressorEffect.SetPreset(preset);
            _compressorEffect.Enabled = enable;
        }
    }

    public void EnableSmartMaster(bool enable, SpeakerType targetSpeaker = SpeakerType.HiFi)
    {
        if (_smartMasterEffect != null)
        {
            _smartMasterEffect.LoadSpeakerPreset(targetSpeaker);
            _smartMasterEffect.Enabled = enable;
        }
    }

    public void SetVolume(double volume)
    {
        var clamped = Math.Clamp(volume, 0.0, 1.0);
        if (_mixer != null) _mixer.MasterVolume = (float)clamped;
        _volume.OnNext(clamped);
    }
    public void MuteDevice(bool mute) => _mixer!.MasterVolume = mute ? 0.0f : (float)_volume.Value;
    public void SetDjCrossfade(double balance = 0.5) { /* Implement your DJ crossfade volume math here */ }

    // ==========================================================
    // HARDWARE & NOTIFICATION TRIGGERS
    // ==========================================================
    public void TriggerNext() { if (_currentSong.Value != null) _nextRequested.OnNext(_currentSong.Value); }
    public void TriggerPrevious() { if (_currentSong.Value != null) _prevRequested.OnNext(_currentSong.Value); }
    public void TriggerFavorite() { if (_currentSong.Value != null) _favRequested.OnNext(_currentSong.Value); }

    // ==========================================================
    // AMBIENCE
    // ==========================================================
    public async Task InitializeAmbienceAsync(string filePath) { /* Implement existing Ambience init */ }
    public void ToggleAmbience(bool isEnabled) { /* Implement existing Ambience toggle */ }

    // ==========================================================
    // CLEANUP
    // ==========================================================
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _disposables.Dispose(); // Cleans all Rx Subscriptions
        _mainSource?.Dispose();
        _ambienceSource?.Dispose();

        _smartMasterEffect?.Dispose();
        _reverbEffect?.Dispose();
        _eqEffect?.Dispose();
        _compressorEffect?.Dispose();

        _mixer?.Stop();
        _mixer?.Dispose();
        await OwnaudioNet.ShutdownAsync();

        // Complete all subjects
        _currentSong.OnCompleted();
        _playbackState.OnCompleted();
        _currentPosition.OnCompleted();
        _duration.OnCompleted();
        _volume.OnCompleted();
        _currentDevice.OnCompleted();
        _eqBands.OnCompleted();
        _playEnded.OnCompleted();
        _seekCompleted.OnCompleted();
        _errors.OnCompleted();
        _nextRequested.OnCompleted();
        _prevRequested.OnCompleted();
        _favRequested.OnCompleted();
    }
}
