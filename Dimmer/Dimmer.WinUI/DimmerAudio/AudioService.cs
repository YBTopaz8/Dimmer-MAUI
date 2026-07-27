using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Subjects;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage;
using Microsoft.UI.Dispatching;

using Ownaudio.Core;
using OwnaudioNET;
using OwnaudioNET.Core;
using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;
using OwnaudioNET.Features.OwnChordDetect;
using OwnaudioNET.Mixing;
using OwnaudioNET.Sources;

namespace Dimmer.WinUI.DimmerAudio;

public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    private static readonly Lazy<AudioService> lazyInstance = new(() => new AudioService());
    public static IDimmerAudioService Current => lazyInstance.Value;

    private double _currentPositionValue;
    private readonly BehaviorSubject<double> _currPositionBS = new(0);
    public IObservable<double> CurrPositionObs => _currPositionBS.AsObservable();

    public double CurrentPosition
    {
        get => _currentPositionValue;
        private set
        {
            if (Math.Abs(_currentPositionValue - value) > 0.1)
            {
                _currPositionBS.OnNext(value);
                if (SetProperty(ref _currentPositionValue, value))
                    PositionChanged?.Invoke(this, value);
            }
        }
    }


    // Lazy Init Lock
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isEngineInitialized;


    // --- OwnaudioNET Components ---
    private AudioMixer? _mixer;
    private FileSource? _mainSource;
    private readonly ConcurrentDictionary<string, FileSource> _activeStems = new();

    // --- Built-in Effects ---
    private ReverbEffect? _reverbEffect;
    private Equalizer30BandEffect? _lofiEq;
    private SmartMasterEffect? _smartMaster;

    // --- Playback State ---
    private Task? _uiUpdateTask;
    private CancellationTokenSource? _uiUpdateCts;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly MediaPlayer _smtcPlayer; // Dummy player for Windows Media Keys
    private readonly CompositeDisposable _disposables = new();

    private SongModelView? _currentTrackMetadata;
    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);
    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();

    // --- Events & State Management ---
    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged { add => _isPlayingChanged += value; remove => _isPlayingChanged -= value; }
    private EventHandler<PlaybackEventArgs>? _isPlayingChanged;

    public event EventHandler<PlaybackEventArgs>? PlayEnded { add => _playEnded += value; remove => _playEnded -= value; }
    private EventHandler<PlaybackEventArgs>? _playEnded;

    public event EventHandler<PlaybackEventArgs>? PlayStarted { add => _playStarted += value; remove => _playStarted -= value; }
    private EventHandler<PlaybackEventArgs>? _playStarted;

    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;

    private DimmerPlaybackState _playbackState = DimmerPlaybackState.PlayCompleted;
    public DimmerPlaybackState CurrentPlaybackState
    {
        get => _playbackState;
        private set => SetProperty(ref _playbackState, value);
    }

    public bool IsPlaying => CurrentPlaybackState == DimmerPlaybackState.Playing;

    private void UpdatePlaybackState(DimmerPlaybackState newState)
    {
        if (SetProperty(ref _playbackState, newState, nameof(CurrentPlaybackState)))
        {
            OnPropertyChanged(nameof(IsPlaying));
            var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying = IsPlaying, EventType = newState };
            PlaybackStateChanged?.Invoke(this, args);
            RaiseIsPlayingChanged();
        }
    }

    public void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels)
    {
        Task.Run(async () => await InitializeAsync(songModelView, 0));
    }

    public List<AudioOutputDevice>? GetAllAudioDevices() => PlaybackDevices?.ToList();

    public async Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
    {
        await GetSetUpOutPutDevices();
        return PlaybackDevices?.ToList() ?? new List<AudioOutputDevice>();
    }

    public async Task<bool> SetPreferredOutputDeviceAsync(AudioOutputDevice dev)
    {
        if (dev?.Name == null || !OwnaudioNet.IsInitialized) return false;
        try
        {
            // Use Async extension off the UI thread to prevent 500ms UI freeze
            await Task.Run(async () =>
            {
                await OwnaudioNet.Engine!.UnderlyingEngine.SetOutputDeviceByNameAsync(dev.Name);
            });
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Device switch failed: {ex.Message}");
            return false;
        }
    }


    public async Task MuteDevice(bool mute)
    {
        IsMuted = mute;
        await Task.CompletedTask;
    }

    public async Task SetVolume(double volume)
    {
        Volume = volume;
        await Task.CompletedTask;
    }

    public double GetCurrentVolume() => Volume;
    public AudioOutputDevice? GetCurrentAudioOutputDevice() => PlaybackDevices?.FirstOrDefault(d => d.IsDefaultDevice);

    private SongModelView? _nextSongInList;
    public Task SendNextSong(SongModelView nextSong)
    {
        _nextSongInList = nextSong;
        return Task.CompletedTask;
    }

    public AudioService()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Must be on UI thread.");

        _smtcPlayer = new MediaPlayer { AudioCategory = MediaPlayerAudioCategory.Media, Volume = 0, IsMuted = true };
        _smtcPlayer.CommandManager.IsEnabled = true;
        SubscribeToSMTCEvents();

        MediaDevice.DefaultAudioRenderDeviceChanged += OnWindowsDefaultAudioDeviceChanged;
    }



    /// <summary>
    /// Thread-safe, deferred initialization. Guarantees the engine starts before any operation,
    /// but avoids async-in-constructor anti-patterns.
    /// </summary>
    private async Task EnsureEngineInitializedAsync()
    {
        if (_isEngineInitialized) return;


        await _initLock.WaitAsync();
        try
        {
            if (_isEngineInitialized) return;

            // FIX 1: 4096 Buffer Size (~85ms). 
            // Gives the GC and UI thread plenty of room to breathe. Guarantees ZERO CRACKLING.
            var config = new AudioConfig
            {
                SampleRate = 48000,
                Channels = 2,
                BufferSize = 4096,
                HostType = EngineHostType.None,
                FallbackToDefaultOnDisconnect = true
            };

            // Start engine on background thread to avoid blocking UI during WASAPI COM setup
            await Task.Run(() =>
            {
                OwnaudioNet.Initialize(config);
                OwnaudioNet.Start();
            });

            _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 4096);
            _mixer.MasterVolume = 1.0f;

            _reverbEffect = new ReverbEffect(size: 0.8f, damp: 0.4f, wet: 0.4f, dry: 0.8f, stereoWidth: 1.0f, mix: 0.0f);
            _lofiEq = new Equalizer30BandEffect { Enabled = false };

            _smartMaster = new SmartMasterEffect();
            _smartMaster.Initialize(config);
            _smartMaster.LoadSpeakerPreset(SpeakerType.HiFi);
            _smartMaster.Enabled = false;

            _mixer.AddMasterEffect(_reverbEffect);
            _mixer.AddMasterEffect(_lofiEq);
            _mixer.AddMasterEffect(_smartMaster);

            _mixer.Start();

            _isEngineInitialized = true;
            Debug.WriteLine("[AudioService] Ownaudio Engine Initialized Successfully.");
        }
        catch (Exception ex)
        {
            OnErrorOccurred("Audio engine init failed", ex);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async void OnWindowsDefaultAudioDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] Windows OS reported audio device change. New ID: {args.Id}");

        // Push to UI thread so we can safely update collections
        _dispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                await EnsureEngineInitializedAsync();

                // Get the newly updated list of devices from the engine
                var devices = await GetAvailableAudioOutputsAsync();
                var newDefault = devices.FirstOrDefault(d => d.IsDefaultDevice);

                if (newDefault != null && OwnaudioNet.IsInitialized)
                {
                    // Force the engine to route to the new Windows default
                    await Task.Run(() => OwnaudioNet.Engine!.UnderlyingEngine.SetOutputDeviceByName(newDefault.Name));
                    Debug.WriteLine($"[AudioService] Successfully re-routed audio to: {newDefault.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Device re-routing failed: {ex.Message}");
            }
        });
    }


    public async Task SetDefaultAsync(AudioOutputDevice device)
    {
        if (device?.Name == null || !OwnaudioNet.IsInitialized) return;
        await Task.Run(async () =>
        {
            await OwnaudioNet.Engine!.UnderlyingEngine.SetOutputDeviceByNameAsync(device.Name);
        });
    }
    private void SubscribeToSMTCEvents()
    {
        var cmd = _smtcPlayer.CommandManager;
        cmd.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        cmd.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;

        cmd.PlayReceived += (s, e) => { e.Handled = true; _dispatcherQueue.TryEnqueue(() => Play(CurrentPosition)); };
        cmd.PauseReceived += (s, e) => { e.Handled = true; _dispatcherQueue.TryEnqueue(Pause); };
        cmd.NextReceived += (s, e) => { e.Handled = true; MediaKeyNextPressed?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata)); };
        cmd.PreviousReceived += (s, e) => { e.Handled = true; MediaKeyPreviousPressed?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata)); };
    }

    private bool _isDisposed;
    public IEnumerable<AudioOutputDevice>? PlaybackDevices { get; set; }

    private async Task InitializeAudioEngineAsync()
    {
        try
        {
            var config = new AudioConfig { SampleRate = 48000, Channels = 2, BufferSize = 1024, HostType = EngineHostType.None,
                FallbackToDefaultOnDisconnect = true
            };
           await OwnaudioNet.InitializeAsync(config);
            OwnaudioNet.Start();

            _mixer = new AudioMixer(OwnaudioNet.Engine!.UnderlyingEngine, bufferSizeInFrames: 1024);
            _mixer.MasterVolume = 1.0f;

            _reverbEffect = new ReverbEffect(size: 0.8f, damp: 0.4f, wet: 0.4f, dry: 0.8f, stereoWidth: 1.0f, mix: 0.0f);

            _lofiEq = new Equalizer30BandEffect();
            _lofiEq.Enabled = false;

            _smartMaster = new SmartMasterEffect();
            _smartMaster.Initialize(config);
            _smartMaster.LoadSpeakerPreset(SpeakerType.HiFi);
            _smartMaster.Enabled = false;

            _mixer.AddMasterEffect(_reverbEffect);
            _mixer.AddMasterEffect(_lofiEq);
            _mixer.AddMasterEffect(_smartMaster);

            _mixer.Start();

            OwnaudioNet.Engine.UnderlyingEngine.OutputDeviceChanged += (s, e) => GetSetUpOutPutDevices().ConfigureAwait(false);
            OwnaudioNet.Engine.UnderlyingEngine.DeviceStateChanged += (s, e) => GetSetUpOutPutDevices().ConfigureAwait(false);
            OwnaudioNet.Engine.UnderlyingEngine.DeviceReconnected += (s, e) => GetSetUpOutPutDevices().ConfigureAwait(false);
            await GetSetUpOutPutDevices();
        }
        catch (Exception ex) { OnErrorOccurred("Audio engine init failed", ex); }
    }

    // --- Properties (DSP mapped directly to OwnaudioNET Sources/Mixer) ---
    private double _playbackSpeed = 1.0;
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            if (SetProperty(ref _playbackSpeed, Math.Clamp(value, 0.25, 2.0)))
            {
                ApplyPitchAndSpeed();
            }
        }
    }
    private double _pitchShift = 0.0;
    public double PitchShift
    {
        get => _pitchShift;
        set
        {
            if (SetProperty(ref _pitchShift, Math.Clamp(value, -12.0, 12.0)))
            {
                ApplyPitchAndSpeed();
            }
        }
    }
    private bool _enableReverb;
    public bool EnableReverb
    {
        get => _enableReverb;
        set { if (SetProperty(ref _enableReverb, value) && _reverbEffect != null) _reverbEffect.Mix = value ? (float)_reverbMix : 0f; }
    }

    private double _reverbMix = 0.4;
    public double ReverbMix
    {
        get => _reverbMix;
        set { if (SetProperty(ref _reverbMix, Math.Clamp(value, 0.0, 1.0)) && _reverbEffect != null && _enableReverb) _reverbEffect.Mix = (float)value; }
    }

    private bool _enableLoFi;
    public bool EnableLoFi
    {
        get => _enableLoFi;
        set
        {
            if (SetProperty(ref _enableLoFi, value) && _lofiEq != null)
            {
                _lofiEq.Enabled = value;
                if (value) ApplyLoFiEQProfile();
            }
        }
    }
    private double _lofiCutoffFrequency = 2000.0; // 2 kHz default lowpass cutoff
    public double LoFiCutoffFrequency
    {
        get => _lofiCutoffFrequency;
        set
        {
            if (SetProperty(ref _lofiCutoffFrequency, Math.Clamp(value, 200.0, 10000.0)))
            {
                if (_enableLoFi) ApplyLoFiEQProfile();
            }
        }
    }


    private void ApplyLoFiEQProfile()
    {
        if (_lofiEq == null) return;

        // Dynamic 30-Band Lowpass Filter according to LoFiCutoffFrequency
        for (int i = 0; i < 30; i++)
        {
            float freq = _lofiEq.GetBandFrequency(i);
            if (freq > _lofiCutoffFrequency)
            {
                // Muffle highs above cutoff frequency
                _lofiEq.SetBandGain(i, freq, 1.0f, -18f);
            }
            else if (freq < 120)
            {
                // Cut sub-bass for telephone/radio effect
                _lofiEq.SetBandGain(i, freq, 1.0f, -12f);
            }
            else
            {
                _lofiEq.SetBandGain(i, freq, 1.0f, 0f);
            }
        }
    }

    private bool _enableSmartMaster;
    public bool EnableSmartMaster
    {
        get => _enableSmartMaster;
        set { if (SetProperty(ref _enableSmartMaster, value) && _smartMaster != null) _smartMaster.Enabled = value; }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetProperty(ref _volume, clamped))
            {
                if (_mixer != null && !IsMuted) _mixer.MasterVolume = (float)clamped;
                VolumeChanged?.Invoke(this, clamped);
            }
        }
    }

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (SetProperty(ref _isMuted, value))
            {
                if (_mixer != null) _mixer.MasterVolume = value ? 0f : (float)_volume;
            }
        }
    }

    private double _duration;
    public double Duration
    {
        get => _duration;
        private set
        {
            if (SetProperty(ref _duration, value))
            {
                DurationChanged?.Invoke(this, value);
                if (IsPlaying || CurrentPlaybackState == DimmerPlaybackState.PausedDimmer)
                    RaiseIsPlayingChanged();
            }
        }
    }

    public SongModelView? CurrentTrackMetadata => _currentTrackMetadata;

    public async Task InitializeAsync(SongModelView songModel, double pos)
    {
        ThrowIfDisposed();
        await EnsureEngineInitializedAsync(); // Make sure engine is ready
        Stop();

        _currentTrackMetadata = songModel;
        _currentSong.OnNext(songModel);
        OnPropertyChanged(nameof(CurrentTrackMetadata));

        try
        {
            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            _mainSource = new FileSource(songModel.FilePath, 4096, targetSampleRate: sr, targetChannels: ch);

            ApplyPitchAndSpeed();
            //_mainSource.PitchShift = (float)_playbackSpeed - 1.0f;

            this.Duration = _mainSource.Duration;

            _mixer?.Stop();

            _mainSource.Seek(pos);
            CurrentPosition = pos;
            _mainSource.AttachToClock(_mixer!.MasterClock);
            _mixer.AddSource(_mainSource);

            await UpdateSMTCMetadataAsync(songModel);
            Play(pos);
        }
        catch (Exception ex)
        {
            UpdatePlaybackState(DimmerPlaybackState.Error);
            OnErrorOccurred($"Failed to initialize track: {songModel.Title}", ex);
        }
    }

    private async Task UpdateSMTCMetadataAsync(SongModelView media)
    {
        try
        {
            var mediaSource = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/silent.mp3"));
            var mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            var props = mediaPlaybackItem.GetDisplayProperties();

            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = media.Title ?? Path.GetFileNameWithoutExtension(media.FilePath) ?? "Unknown Title";
            props.MusicProperties.Artist = media.OtherArtistsName?.ToString() ?? "";

            if (!string.IsNullOrEmpty(media.CoverImagePath) && File.Exists(media.CoverImagePath))
            {
                var coverFile = await StorageFile.GetFileFromPathAsync(media.CoverImagePath);
                props.Thumbnail = RandomAccessStreamReference.CreateFromFile(coverFile);
            }

            mediaPlaybackItem.ApplyDisplayProperties(props);
            _smtcPlayer.Source = mediaPlaybackItem;
        }
        catch { }
    }

    public void Play(double pos)
    {
        if (_mainSource == null || _mixer == null) return;


        _mainSource.Seek(pos);
        foreach (var stem in _activeStems.Values) stem.Seek(pos);
        CurrentPosition = pos;
       
        _mixer.Start();
        _mainSource.Play();
        foreach (var stem in _activeStems.Values) stem.Play();

        UpdatePlaybackState(DimmerPlaybackState.Playing);
        _playStarted?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata) { EventType = DimmerPlaybackState.Playing });

        StartUIUpdateLoop();
    }

    public void Pause()
    {
        if (_mainSource == null || _mixer == null) return;

        _mainSource.Pause();
        foreach (var stem in _activeStems.Values) stem.Pause();

        StopUIUpdateLoop();
        UpdatePlaybackState(DimmerPlaybackState.PausedDimmer);
    }

    public void Stop()
    {
        StopUIUpdateLoop();
        ClearAllStems();

        if (_mainSource != null)
        {
            _mainSource.Stop();
            _mixer?.RemoveSource(_mainSource);
            _mainSource.Dispose();
            _mainSource = null;
        }

        CurrentPosition = 0;
        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);
    }

    public void Seek(double positionSeconds)
    {
        if (_mainSource == null) return;

        var target = Math.Clamp(positionSeconds, 0, Duration);

        _mainSource.Seek(target);
        foreach (var stem in _activeStems.Values) stem.Seek(target);

        CurrentPosition = target;
        SeekCompleted?.Invoke(this, target);
    }

    // --- Multi-Track (Stem) Management ---
    public async Task AddStemAsync(string stemId, string filePath, double initialVolume = 1.0)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath) || _mixer == null) return;

        try
        {
            int sr = OwnaudioNet.Engine!.Config.SampleRate;
            int ch = OwnaudioNet.Engine!.Config.Channels;

            var stemSource = new FileSource(filePath, 4096, targetSampleRate: sr, targetChannels: ch);
            stemSource.Volume = (float)Math.Clamp(initialVolume, 0.0, 1.0);
            stemSource.PitchShift = (float)_playbackSpeed - 1.0f;

            stemSource.AttachToClock(_mixer.MasterClock);
            _mixer.AddSource(stemSource);

            if (_mainSource != null) stemSource.Seek(_mainSource.Position);

            if (CurrentPlaybackState == DimmerPlaybackState.Playing) stemSource.Play();

            _activeStems.AddOrUpdate(stemId, stemSource, (key, old) =>
            {
                _mixer.RemoveSource(old);
                old.Dispose();
                return stemSource;
            });

            await Task.CompletedTask;
        }
        catch (Exception ex) { Debug.WriteLine($"[AudioService] Failed to add stem {stemId}: {ex.Message}"); }
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
        if (_activeStems.TryGetValue(stemId, out var stem)) stem.Volume = (float)Math.Clamp(volume, 0.0, 1.0);
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

    // --- AI Chord Detect ---
    public async Task<(string Key, int Bpm, string Chords)> AnalyzeTrackChordsAsync(string filePath)
    {
        if (!File.Exists(filePath)) return ("Unknown", 0, "");

        return await Task.Run(() =>
        {
            try
            {
                var (chords, key, bpm) = ChordDetect.DetectFromFile(filePath);
                string chordString = string.Join("\n", chords.Select(c => $"{c.StartTime:F1}s - {c.EndTime:F1}s: {c.ChordName}"));
                return (key.KeyName, bpm, chordString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Chord detect failed: {ex}");
                return ("Error", 0, "");
            }
        });
    }

    // --- UI Update Loop ---
    private void StartUIUpdateLoop()
    {
        if (_uiUpdateTask != null && !_uiUpdateTask.IsCompleted) return;
        _uiUpdateCts = new CancellationTokenSource();
        _uiUpdateTask = Task.Run(() => UIUpdateLoopAsync(_uiUpdateCts.Token));
    }

    private void StopUIUpdateLoop()
    {
        _uiUpdateCts?.Cancel();
        _uiUpdateCts?.Dispose();
        _uiUpdateCts = null;
    }

    private async Task UIUpdateLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_mainSource != null && _mixer != null)
                {
                    if (_mainSource.State == AudioState.Playing)
                    {
                        _dispatcherQueue.TryEnqueue(() => CurrentPosition = _mainSource.Position);
                    }
                    else if (_mainSource.State == AudioState.Stopped && CurrentPlaybackState == DimmerPlaybackState.Playing)
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);
                            _playEnded?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata) { EventType = DimmerPlaybackState.PlayCompleted });
                            Stop();
                        });
                        break;
                    }
                }
                await Task.Delay(100, token);
            }
        }
        catch (OperationCanceledException) { }
    }

    // --- Output Management ---
    private async Task GetSetUpOutPutDevices()
    {
        var devices = OwnaudioNet.Engine?.UnderlyingEngine.GetOutputDevices();
        if (devices != null)
        {
            PlaybackDevices = devices.Select(d => new AudioOutputDevice
            {
                Id = d.DeviceId,
                Name = d.Name,
                IsDefaultDevice = d.IsDefault,
                IsPlaybackDevice = true
            }).ToList();
            OnPropertyChanged(nameof(PlaybackDevices));
        }
        await Task.CompletedTask;
    }

    private void RaiseIsPlayingChanged()
    {
        var eventType = IsPlaying ? DimmerPlaybackState.Playing : DimmerPlaybackState.PausedDimmer;
        _isPlayingChanged?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying = IsPlaying, EventType = eventType });
    }

    private void OnErrorOccurred(string message, Exception? exception = null)
    {
        Debug.WriteLine($"[AudioService ERROR] {message} | {exception?.Message}");
        ErrorOccurred?.Invoke(this, new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying = IsPlaying, EventType = DimmerPlaybackState.Error });
    }

    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        _dispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        _dispatcherQueue.TryEnqueue(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(AudioService));
    }
    private bool _isReversed;
    public bool IsReversed
    {
        get => _isReversed;
        set
        {
            if (SetProperty(ref _isReversed, value))
            {
                ApplyPitchAndSpeed();
            }
        }
    }

    private void ApplyPitchAndSpeed()
    {
        // Combines PlaybackSpeed, PitchShift, and Reverse into the FileSource PitchShift property
        float pitchVal = (float)(_pitchShift + (_playbackSpeed - 1.0));
        if (_isReversed) pitchVal = -Math.Abs(pitchVal == 0 ? 1.0f : pitchVal);

        if (_mainSource != null) _mainSource.PitchShift = pitchVal;
        foreach (var stem in _activeStems.Values) stem.PitchShift = pitchVal;
    }
    // Granular Reverb:
    public float ReverbRoomSize
    {
        get => _reverbEffect?.RoomSize ?? 0.8f;
        set { if (_reverbEffect != null) _reverbEffect.RoomSize = (float)Math.Clamp(value, 0.0, 1.0); }
    }

    public float ReverbDamping
    {
        get => _reverbEffect?.Damping ?? 0.4f;
        set { if (_reverbEffect != null) _reverbEffect.Damping = (float)Math.Clamp(value, 0.0, 1.0); }
    }






    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();

        _mixer?.Dispose();
        OwnaudioNet.Stop();
        OwnaudioNet.Shutdown();

        _smtcPlayer?.Dispose();
        _disposables.Dispose();
    }
}