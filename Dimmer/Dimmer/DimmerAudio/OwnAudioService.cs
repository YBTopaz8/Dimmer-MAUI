using OwnaudioNET;
using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;
using OwnaudioNET.Mixing;
using OwnaudioNET.Monitoring;
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
    
    private readonly BehaviorSubject<float[]> _eqBands = new(new float[30]);

    private readonly BehaviorSubject<(double Left, double Right)> _peakLevels = new((-60.0, -60.0));


    private readonly BehaviorSubject<float> _pitch = new(0f);
    private readonly BehaviorSubject<float> _speed = new(1f);

    public IObservable<float> PitchObs => _pitch.AsObservable();
    public IObservable<float> SpeedObs => _speed.AsObservable();

    // ==========================================================
    // REACTIVE EVENTS (Subjects are for one-time triggers)
    // ==========================================================
    private readonly Subject<SongModelView> _playEnded = new();
    private readonly Subject<double> _seekCompleted = new();
    private readonly Subject<Exception> _errors = new();
    private readonly Subject<SongModelView> _nextRequested = new();
    private readonly Subject<SongModelView> _prevRequested = new();
    private readonly Subject<SongModelView> _favRequested = new();

    private readonly BehaviorSubject<(double? A, double? B)> _abLoopState = new((null, null));
   
    private bool _isLoopSeeking = false;
    // ==========================================================
    // EXPOSED OBSERVABLES
    // ==========================================================
    public IObservable<(double? A, double? B)> AbLoopStateObs => _abLoopState.AsObservable();

    public IObservable<(double Left, double Right)> PeakLevelsObs => _peakLevels.AsObservable();
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
    public IObservable<float[]> SpectrumDataObs => _spectrumData.AsObservable();

    // ==========================================================
    // INTERNAL ENGINE STATE
    // ==========================================================
    private AudioMixer? _mixer;
    private FileSource? _mainSource;
    private FileSource? _secondarySource; // For DJ crossfade
    private FileSource? _ambienceSource;
    private readonly SemaphoreSlim _transportLock = new(1, 1);
    private readonly Stopwatch _watch = new();

    private EffectSpectrumAnalyzer? _analyzer;
    private IDisposable? _analyzerTimer;
    private readonly Subject<float[]> _spectrumData = new();
    private double _lastEnginePos;
    private double _lastEnginePosAt;
    private bool _isDisposed;
    private bool _isAmbienceEnabled;
    private double _ambienceVolume = 0.5;

    private double? _loopPointA;
    private double? _loopPointB;
    public bool IsAbLooping { get; private set; }
    // Effects
    private Equalizer30BandEffect? _eqEffect;
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


    public float CurrentPitch
    {
        get => _pitch.Value;
        set
        {
            var clamped = Math.Clamp(value, -12f, 12f);
            _pitch.OnNext(clamped);
            _mainSource?.SetPitchSmooth(clamped);
        }
    }

    public float CurrentSpeed
    {
        get => _speed.Value;
        set
        {
            var clamped = Math.Clamp(value, 0.5f, 2.0f);
            _speed.OnNext(clamped);
            _mainSource?.SetTempoSmooth(clamped);
        }
    }
    private double _duckingMultiplier = 1.0;

    // Add this to your interface (IDimmerAudioService) or cast to OwnAudioService
    public void SetDucking(bool isDucked)
    {
        _duckingMultiplier = isDucked ? 0.2 : 1.0;
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (_mixer != null)
            _mixer.MasterVolume = (float)(_volume.Value * _duckingMultiplier);
    }


    // ==========================================================
    // INITIALIZATION & DEVICE ROUTING
    // ==========================================================
    public async Task InitializeEngineAsync(string? outputDeviceId = null)
    {
        if (OwnaudioNet.Engine?.UnderlyingEngine is not null)
        {
            return ;
        }
        try
        {
            var config = OwnaudioNet.CreateDefaultConfig();
            config.EnableInput = false;
            config.OutputDeviceId = outputDeviceId;

            config.FallbackToDefaultOnDisconnect = true;

            await OwnaudioNet.InitializeAsync(config);
            OwnaudioNet.Start();

            _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 1024);

            _compressorEffect = new CompressorEffect { Enabled = false };
            _eqEffect = new Equalizer30BandEffect { Enabled = false };
            _reverbEffect = new ReverbEffect { Enabled = false };
            _smartMasterEffect = new SmartMasterEffect { Enabled = false };

            _mixer.AddMasterEffect(_compressorEffect);
            _mixer.AddMasterEffect(_eqEffect);
            _mixer.AddMasterEffect(_reverbEffect);
            _mixer.AddMasterEffect(_smartMasterEffect);

            _mixer.PlaybackEnded += (s, e) =>
            {
                Task.Run(() =>
                {
                    if (_currentSong.Value != null)
                    {
                        _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
                        _playEnded.OnNext(_currentSong.Value);
                    }
                });
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

            if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(3)))
            {
                _errors.OnNext(new TimeoutException("Audio engine locked during device switch."));
                return ;
            }
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
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }
        try
        {
            
            _playbackState.OnNext(DimmerPlaybackState.Opening);
            _currentSong.OnNext(songModel);

            if (_mainSource != null)
            {
                _mixer?.RemoveSource(_mainSource.Id);
                _mainSource.Dispose();
            }
            if(OwnaudioNet.Engine is null)
            {
                await InitializeEngineAsync();
            }

            
            if(OwnaudioNet.Engine is null)
            {
                throw new InvalidOperationException("OwnaudioNet Engine is not initialized.");
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
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }
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
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }

        try
        {
            
            _mainSource?.Pause();
            _ambienceSource?.Pause();
            _mixer?.Pause();
            _playbackState.OnNext(DimmerPlaybackState.PausedUser);
            _peakLevels.OnNext((-60.0, -60.0));
        }
        catch (Exception ex) { _errors.OnNext(ex); }
        finally { _transportLock.Release(); }
    }

    public async Task SeekAsync(double positionSeconds)
    {
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }
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
        _peakLevels.OnNext((-60.0, -60.0));
        _playbackState.OnNext(DimmerPlaybackState.PlayCompleted);
    }
    private static double ToDbFs(float linear)
    => linear > 0f ? Math.Max(20.0 * Math.Log10(linear), -60.0) : -60.0;
    private void UpdatePositionFromEngine()
    {
        if (_mixer == null || _mainSource == null || _mainSource.IsEndOfStream) return;

        // 1. Update Position
        double enginePos = _mixer.MasterClock.CurrentTimestamp;
        double now = _watch.Elapsed.TotalSeconds;

        if (Math.Abs(enginePos - _lastEnginePos) > 0.001)
        {
            _lastEnginePos = enginePos;
            _lastEnginePosAt = now;
        }
        double smoothPos = _lastEnginePos + (now - _lastEnginePosAt);
        _currentPosition.OnNext(Math.Clamp(smoothPos, 0, _duration.Value));

        if (IsAbLooping && _loopPointA.HasValue && _loopPointB.HasValue)
        {
            if (smoothPos >= _loopPointB.Value && !_isLoopSeeking)
            {
                _isLoopSeeking = true;
                Task.Run(async () =>
                {
                    await SeekAsync(_loopPointA.Value);
                    _isLoopSeeking = false;
                });
            }
        }


        // 2. Update VU Meters (Only push if changed by 0.5dB to save UI layout passes)
        double leftDb = ToDbFs(_mixer.LeftPeak);
        double rightDb = ToDbFs(_mixer.RightPeak);
        var currentPeaks = _peakLevels.Value;

        if (Math.Abs(leftDb - currentPeaks.Left) >= 0.5 || Math.Abs(rightDb - currentPeaks.Right) >= 0.5)
        {
            _peakLevels.OnNext((leftDb, rightDb));
        }
    }

    public double Volume
    {
        get => _volume.Value;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            _volume.OnNext(clamped);
            ApplyVolume(); 
        }
    }
    public bool IsMuted => _volume.Value == 0;
    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        return _currentDevice.Value;
    }

    public async Task SendNextSong(SongModelView nextSong)
    {
        // Pre-load the upcoming track while the current one is still playing
        int sr = OwnaudioNet.Engine!.Config.SampleRate;
        int ch = OwnaudioNet.Engine!.Config.Channels;

        _secondarySource = new FileSource(nextSong.FilePath, targetSampleRate: sr, targetChannels: ch);

        // Add it to the mixer PREPARED, but DO NOT start it yet.
        // The engine holds it in memory, fully decoded and ready to fire.
        _mixer?.AddSourcePrepared(_secondarySource);
    }
    public void StartVisualizer(int fftSize = 2048)
    {
        if (_mixer == null) return;

        _analyzer?.Dispose();
        _analyzerTimer?.Dispose();

        // Tap the master chain so it includes Reverb, Nightcore, and EQ
        _analyzer = new EffectSpectrumAnalyzer(_mixer.CreateMasterEffectTap(), fftSize);

        // Poll at ~30fps (33ms)
        _analyzerTimer = Observable.Interval(TimeSpan.FromMilliseconds(33))
            .Where(_ => IsPlaying)
            .Subscribe(_ =>
            {
                if (_analyzer != null && _analyzer.Update())
                {
                    // Wet signal (PostMagnitudesDb) is what the user actually hears
                    _spectrumData.OnNext(_analyzer.PostMagnitudesDb.ToArray());
                }
            });
    }

    public void StopVisualizer()
    {
        _analyzerTimer?.Dispose();
        _analyzerTimer = null;
        _analyzer?.Dispose();
        _analyzer = null;
    }
    public void ExportRemixToDisk(string outputFilePath)
    {
        
        _mixer?.StartRecording(outputFilePath);
    }

    public void StopExport()
    {
        _mixer?.StopRecording();
    }
    private CancellationTokenSource? _crossfadeCts;
    public async Task CrossfadeToNextAsync(SongModelView nextSong, double overlapSeconds = 3.0)
    {
        _crossfadeCts?.Cancel();
        _crossfadeCts = new CancellationTokenSource();
        var token = _crossfadeCts.Token;

        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }

        FileSource? oldSource = _mainSource;
        try
        {
            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            // 1. Prepare new source
            var newSource = new FileSource(nextSong.FilePath, targetSampleRate: sr, targetChannels: ch);
            newSource.SetPitchSmooth(_currentPitchSemitones);
            newSource.SetTempoSmooth(_currentTempoRatio);
            newSource.Volume = 0f; // Start silent for fade-in

            _mixer?.AddSourcePrepared(newSource);
            _mixer?.StartPreparedSources(0);

            // 2. Correctly update engine pointers
            _mainSource = newSource;
            _secondarySource = null;

            _currentSong.OnNext(nextSong);
            _duration.OnNext(_mainSource.Duration);

            // 3. Fire-and-forget the volume crossfade
            _ = Task.Run(async () =>
            {
                try
                {
                    int steps = 50;
                    int delayMs = (int)((overlapSeconds * 1000) / steps);

                    for (int i = 0; i <= steps; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        float ratio = (float)i / steps;
                        if (oldSource != null) oldSource.Volume = 1f - ratio;
                        if (newSource != null) newSource.Volume = ratio;

                        await Task.Delay(delayMs);
                    }
                }
                finally
                {
                    if (oldSource != null)
                    {
                        _mixer?.RemoveSource(oldSource.Id);
                        oldSource.Stop();
                        oldSource.Dispose();
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _errors.OnNext(ex);
        }
        finally
        {
            _transportLock.Release(); // MUST RELEASE THE LOCK
        }
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
    public void SetLoopPointA()
    {
        _loopPointA = _currentPosition.Value;

        // If B is already set and is behind A, reset B
        if (_loopPointB.HasValue && _loopPointB.Value <= _loopPointA.Value)
        {
            _loopPointB = null;
            IsAbLooping = false;
        }
        _abLoopState.OnNext((_loopPointA, _loopPointB));
    }
    public void SetLoopPointB()
    {
        if (_loopPointA.HasValue && _currentPosition.Value > _loopPointA.Value)
        {
            _loopPointB = _currentPosition.Value;
            IsAbLooping = true;
            _abLoopState.OnNext((_loopPointA, _loopPointB));
        }
    }
    public void ClearAbLoop()
    {
        _loopPointA = null;
        _loopPointB = null;
        IsAbLooping = false;
        _abLoopState.OnNext((null, null));
    }
    
    public void SetPitchAndSpeed(float pitchSemitones, float tempoRatio)
    {
        CurrentPitch = pitchSemitones;
        CurrentSpeed = tempoRatio;
    }

    public void EnableEqualizer(bool enable) { if (_eqEffect != null) _eqEffect.Enabled = enable; }
    public void SetEqualizerPreset(Equalizer30Preset preset) => _eqEffect?.SetPreset(preset);

    public void ChangeEqBand(int bandIndex, float gainDb)
    {
        if (_eqEffect == null || bandIndex < 0 || bandIndex >= 30) return;

       
        _eqEffect.SetBandGain(bandIndex, 0f, 1f, gainDb); // Freq and Q are ignored by the wrapper for standard index calls, but gain is applied.

        var currentBands = _eqBands.Value;
        currentBands[bandIndex] = gainDb;
        _eqBands.OnNext(currentBands);
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
    public async Task TransitionToNextGaplessAsync(SongModelView nextSong)
    {
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }
        try
        {
           
            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            // 1. If we haven't pre-loaded the next song yet, decode it now
            if (_secondarySource == null)
            {
                _secondarySource = new FileSource(nextSong.FilePath, targetSampleRate: sr, targetChannels: ch);
                _mixer?.AddSourcePrepared(_secondarySource);
            }

            _secondarySource.SetPitchSmooth(_currentPitchSemitones);
            _secondarySource.SetTempoSmooth(_currentTempoRatio);
            _secondarySource.Volume = (float)_volume.Value; // Full volume immediately

            // 2. Start it instantly on the existing master clock
            _mixer?.StartPreparedSources(0);

            var oldSource = _mainSource;
            _mainSource = _secondarySource;
            _secondarySource = null; // Reset for the next-next song

            _currentSong.OnNext(nextSong);
            _duration.OnNext(_mainSource.Duration);

            // 3. Detach and dispose the old song in the background
            if (oldSource != null)
            {
                RxSchedulers.Background.ScheduleTo(() =>
                {
                    _mixer?.RemoveSource(oldSource.Id);
                    oldSource.Stop();
                    oldSource.Dispose();
                });
            }
        }
        catch (Exception ex) { _errors.OnNext(ex); }
        finally { _transportLock.Release(); }
    }
    public void SetVolume(double volume)
    {

        Volume = volume;
    }
    public void MuteDevice(bool mute)
    {
        
        _mixer!.MasterVolume = mute ? 0.0f : (float)_volume.Value;
    }
    public async Task InitializeDjModeAsync(SongModelView trackA, SongModelView trackB)
    {
        if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            Debug.WriteLine("Engine is hung!");
            return;
        }
        try
        {
            
            // 1. Clean up old sources
            if (_mainSource != null) { _mixer?.RemoveSource(_mainSource.Id); _mainSource.Dispose(); }
            if (_secondarySource != null) { _mixer?.RemoveSource(_secondarySource.Id); _secondarySource.Dispose(); }

            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            _mainSource = new FileSource(trackA.FilePath, targetSampleRate: sr, targetChannels: ch);
            _secondarySource = new FileSource(trackB.FilePath, targetSampleRate: sr, targetChannels: ch);

            // 2. Add both via Prepared (Guarantees they start on the exact same sample!)
            _mixer?.Pause();
            _mixer?.AddSourcePrepared(_mainSource);
            _mixer?.AddSourcePrepared(_secondarySource);

            SetDjCrossFade(0.5); // Start at 50/50 mix

            _mixer?.StartPreparedSources(0);
            _mixer?.Start();

            _playbackState.OnNext(DimmerPlaybackState.Playing);
        }
        finally { _transportLock.Release(); }
    }
    public void SetDjCrossFade(double balance = 0.5)
    {
        // Balance: 0.0 = 100% Track A, 1.0 = 100% Track B, 0.5 = 50% Both
        var clamped = Math.Clamp(balance, 0.0, 1.0);

        if (_mainSource != null) _mainSource.Volume = (float)(1.0 - clamped);
        if (_secondarySource != null) _secondarySource.Volume = (float)clamped;
    }

    // ==========================================================
    // HARDWARE & NOTIFICATION TRIGGERS
    // ==========================================================
    public void TriggerNext() { if (_currentSong.Value != null) _nextRequested.OnNext(_currentSong.Value); }
    public void TriggerPrevious() { if (_currentSong.Value != null) _prevRequested.OnNext(_currentSong.Value); }
    public void TriggerFavorite() { if (_currentSong.Value != null) _favRequested.OnNext(_currentSong.Value); }

    // ==========================================================
    // AMBIENCE
    // ==========================================================
    public async Task InitializeAmbienceAsync(string filePath)
    {

        try
        {
            if (!await _transportLock.WaitAsync(TimeSpan.FromSeconds(2)))
            {
                Debug.WriteLine("Engine is hung!");
                return; // Prevent app freeze
            }
            _ambienceSource?.Dispose();

            _ambienceSource = new FileSource(filePath)
            {
                Volume = (float)_ambienceVolume,
                Loop = true
            };

            _mixer?.AddSource(_ambienceSource);

            if (_isAmbienceEnabled && IsPlaying)
                _ambienceSource.Play();
        }
        finally { _transportLock.Release(); }
    }

    public void ToggleAmbience(bool isEnabled)
    {
        _isAmbienceEnabled = isEnabled;
        if (_ambienceSource == null) return;

        if (isEnabled && IsPlaying) _ambienceSource.Play();
        else _ambienceSource.Pause();
    }


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
