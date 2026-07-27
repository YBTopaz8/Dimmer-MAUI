namespace Dimmer.DimmerAudio;


public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    private static readonly Lazy<AudioService> lazyInstance = new(() => new AudioService());
    public static IDimmerAudioService Current => lazyInstance.Value;

    private ExoPlayerServiceBinder? _binder;
    private ExoPlayerService? Service => _binder?.Service;

    public AudioService() { }

    public SongModelView? CurrentTrackMetadata => ExoPlayerService.CurrentSongExposed;
    public bool IsPlaying => Service?.IsPlaying ?? false;
    public double CurrentPosition => Service?.CurrentPosition ?? 0;
    public double Duration => Service?.Duration ?? 0;

    public double Volume
    {
        get => Service?.Volume ?? 1.0;
        set { if (Service != null) Service.Volume = value; }
    }

    public bool IsMuted
    {
        get => Service?.IsMuted ?? false;
        set { if (Service != null) Service.IsMuted = value; }
    }

    // --- Granular DSP Controls delegated directly to OwnaudioNET Service ---
    public double PlaybackSpeed
    {
        get => Service?.PlaybackSpeed ?? 1.0;
        set { if (Service != null) Service.PlaybackSpeed = value; }
    }

    public double PitchShift
    {
        get => Service?.PitchShift ?? 0.0;
        set { if (Service != null) Service.PitchShift = value; }
    }

    public bool IsReversed
    {
        get => Service?.IsReversed ?? false;
        set { if (Service != null) Service.IsReversed = value; }
    }

    public bool EnableReverb
    {
        get => Service?.EnableReverb ?? false;
        set { if (Service != null) Service.EnableReverb = value; }
    }

    public double ReverbMix
    {
        get => Service?.ReverbMix ?? 0.4;
        set { if (Service != null) Service.ReverbMix = value; }
    }

    public float ReverbRoomSize
    {
        get => Service?.ReverbRoomSize ?? 0.8f;
        set { if (Service != null) Service.ReverbRoomSize = value; }
    }

    public float ReverbDamping
    {
        get => Service?.ReverbDamping ?? 0.4f;
        set { if (Service != null) Service.ReverbDamping = value; }
    }

    public bool EnableLoFi
    {
        get => Service?.EnableLoFi ?? false;
        set { if (Service != null) Service.EnableLoFi = value; }
    }

    public double LoFiCutoffFrequency
    {
        get => Service?.LoFiCutoffFrequency ?? 2000.0;
        set { if (Service != null) Service.LoFiCutoffFrequency = value; }
    }

    public bool EnableSmartMaster
    {
        get => Service?.EnableSmartMaster ?? false;
        set { if (Service != null) Service.EnableSmartMaster = value; }
    }

    // --- Dynamic Stems ---
    public Task AddStemAsync(string stemId, string filePath, double initialVolume = 1.0)
    {
        Service?.AddStem(stemId, filePath, initialVolume);
        return Task.CompletedTask;
    }

    public void RemoveStem(string stemId) => Service?.RemoveStem(stemId);
    public void SetStemVolume(string stemId, double volume) => Service?.SetStemVolume(stemId, volume);
    public void ClearAllStems() => Service?.ClearAllStems();

    // --- AI Chord Analysis ---
    public async Task<(string Key, int Bpm, string Chords)> AnalyzeTrackChordsAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
               
                return ("Nothing",0,"Nil");
            }
            catch { return ("Error", 0, ""); }
        });
    }

    // --- Binder Setup ---
    public void SetBinder(ExoPlayerServiceBinder? binder)
    {
        _binder = binder;
        if (Service != null) ConnectEvents();
    }

    public Task InitializeAsync(SongModelView songModel, double pos)
    {
        Service?.PrepareTrack(songModel, pos);
        return Task.CompletedTask;
    }

    public void Play(double pos) => Service?.Play(pos);
    public void Pause() => Service?.Pause();
    public void Stop() => Service?.Stop();
    public void Seek(double positionSeconds) => Service?.Seek(positionSeconds);

    public IEnumerable<AudioOutputDevice>? PlaybackDevices => Service?.GetAvailableDevices();
    public List<AudioOutputDevice>? GetAllAudioDevices() => Service?.GetAvailableDevices();
    public Task<bool> SetPreferredOutputDeviceAsync(AudioOutputDevice dev)
    {
        var res= Service?.SetPreferredDevice(dev) ?? false;

        return Task.FromResult(res);
    }

    public async Task SetDefaultAsync(AudioOutputDevice device) 
    {
        await SetPreferredOutputDeviceAsync(device); 
       
    }
    public Task MuteDevice(bool mute) { IsMuted = mute; return Task.CompletedTask; }
    public Task SetVolume(double volume) { Volume = volume; return Task.CompletedTask; }
    public double GetCurrentVolume() => Volume;
    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        return Service?.GetAvailableDevices().FirstOrDefault();
        //return GetAvailableAudioOutputsAsync().Result?.FirstOrDefault();
    }

    public Task SendNextSong(SongModelView nextSong) => Task.CompletedTask;
    public void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels) { }

    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);
    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();

    // --- Events ---
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayEnded;
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed;
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void ConnectEvents()
    {
        if (Service == null) return;
        Service.PositionChanged += (s, pos) => PositionChanged?.Invoke(this, pos);
        Service.IsPlayingChanged += (s, isPlaying) => IsPlayingChanged?.Invoke(this, new PlaybackEventArgs(CurrentTrackMetadata) { IsPlaying = isPlaying });
        Service.PlayingEnded += (s, e) => PlayEnded?.Invoke(this, new PlaybackEventArgs(CurrentTrackMetadata));
    }

    public async ValueTask DisposeAsync()
    {
        _binder = null;
        await Task.CompletedTask;
    }
}