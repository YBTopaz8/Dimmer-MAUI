using AndroidX.Media3.Common;



namespace Dimmer.DimmerAudio;

public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    private ExoPlayerServiceBinder? _binder;
    private ExoPlayerService? Service => _binder?.Service;
    private IPlayer? Player => Service?.GetPlayerInstance();

    // Store the last known song model to provide context in events.
    private SongModelView? _currentSongModel;


    public SongModelView? CurrentTrackMetadata => ExoPlayerService.CurrentSongExposed;

    #region IDimmerAudioService Implementation (Properties)

    public bool IsPlaying => Player?.IsPlaying ?? false;
    public double CurrentPosition => (Player?.CurrentPosition ?? 0) / 1000.0;
    public double Duration => Player?.Duration > 0 ? Player.Duration / 1000.0 : 0;

    public double Volume
    {
        get
        {
            return Player?.Volume ?? 1.0f;
        }

        set
        {
            if (Player != null)
            {
                RxSchedulers.UI.ScheduleTo(() =>
                {

                    Player.Volume = (float)Math.Clamp(value, 0.0, 1.0);
                    NotifyPropertyChanged();
                });
            }
        }
    }

    public IEnumerable<AudioOutputDevice>? PlaybackDevices { get; }
    public double AmbienceVolume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    private bool _isFrequencyVisualizationEnabled;
    public bool IsFrequencyVisualizationEnabled
    {
        get => _isFrequencyVisualizationEnabled;
        set
        {
            if (_isFrequencyVisualizationEnabled != value)
            {
                _isFrequencyVisualizationEnabled = value;
                UpdateVisualizerState();
                NotifyPropertyChanged();
            }
        }
    }

    #endregion

    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);

    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
    #region IDimmerAudioService Implementation (Events)

    // These events are raised in response to the native service's events.
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? IsPlayingChanged;
    public event EventHandler<PlaybackEventArgs>? PlayEnded;
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed;
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<double>? VolumeChanged;
    public event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event EventHandler<byte[]?>? FrequencyDataAvailable;

    // Unused events from interface, kept for compatibility.
    public event EventHandler<double>? DurationChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    public void SetBinder(ExoPlayerServiceBinder? binder)
    {
        if (_binder == binder)
            return;

        DisconnectEvents();
        _binder = binder;

        if (_binder?.Service != null)
        {
            ConnectEvents();
            Console.WriteLine("[AudioService] Binder set and events connected.");
            NotifyAllPropertiesChanged();
        }
        else
        {
            Console.WriteLine("[AudioService] Binder set to null or service not available.");
        }
    }

    #region IDimmerAudioService Implementation (Commands)

    public Task InitializeAsync(SongModelView songModel, double pos)
    {
        _currentSongModel = songModel;
        // Tell the native service to prepare the track.
        var finalArtName = songModel.HasSyncedLyrics ? "🎙️ " + songModel.OtherArtistsName : songModel.OtherArtistsName;
        long positionMs = (long)(pos * 1000.0);
        Service?.Prepare(songModel.FilePath, songModel.Title, finalArtName, songModel.AlbumName, songModel,startPositionMs: positionMs);

        return Task.CompletedTask;
    }

    public void InitializePlaylist(SongModelView song, IEnumerable<SongModelView> songModels)
    {
        _currentSongModel = song;
        //Service?.PreparePlaylist(song, songModels);
    }

    public void Play(double pos)
    {
        Player?.Play();
        Seek(pos);
    }

    public void Pause() => Player?.Pause();
    public void Stop() => Player?.Stop();

    public void Seek(double positionSeconds)
    {
        // We just send the command. We do NOT raise the SeekCompleted event here.
        // The native service will raise its event when the seek is actually done.
        long positionMs = (long)(positionSeconds * 1000.0);
        Player?.SeekTo(positionMs);
    }

    public List<AudioOutputDevice>? GetAllAudioDevices() => Service?.GetAvailableAudioOutputMAUI();
    public bool SetPreferredOutputDevice(AudioOutputDevice dev) => Service?.SetPreferredDevice(dev) ?? false;

    #endregion

    #region Event Wiring

    private void ConnectEvents()
    {
        if (Service == null)
            return;

        // Subscribe to events coming *from* the ExoPlayerService
        Service.PlaybackStateChanged  += OnNativePlaybackStateChanged; // Use the central state event
        Service.IsPlayingChanged  += OnNativeIsPlayingChanged;
        Service.PlayingEnded += OnNativePlayEnded;
        Service.PositionChanged += OnNativePositionChanged;
        Service.DeviceVolumeChanged += OnDeviceVolumeChanged;
        Service.SeekCompleted += OnNativeSeekCompleted;
        Service.VolumeChanged += OnNativeVolumeChanged;
        Service.PlayNextPressed += OnNativePlayNextPressed;
        Service.PlayPreviousPressed += OnNativePlayPreviousPressed;
    }

    private void DisconnectEvents()
    {
        if (Service == null)
            return;

        // Unsubscribe from all events
        Service.PlaybackStateChanged -= OnNativePlaybackStateChanged;
        Service.IsPlayingChanged -= OnNativeIsPlayingChanged;
        Service.PlayingEnded -= OnNativePlayEnded;
        Service.PositionChanged -= OnNativePositionChanged;
        Service.SeekCompleted -= OnNativeSeekCompleted;
        Service.VolumeChanged -= OnNativeVolumeChanged;
        Service.DeviceVolumeChanged -= OnDeviceVolumeChanged;
        Service.PlayNextPressed -= OnNativePlayNextPressed;
        Service.PlayPreviousPressed -= OnNativePlayPreviousPressed;
    }

    private void OnDeviceVolumeChanged(object? sender, (double newVol, bool isDeviceMuted, int devMavVol) e)
    {
        DeviceVolumeChanged?.Invoke(sender, e);
    }

    private void OnNativeVolumeChanged(object? sender, double e)
    {
        VolumeChanged?.Invoke(this, e);
    }

    #endregion

    #region Native Event Handlers (The Translation Layer)

    // These methods receive events from the native service and translate them
    // into the cross-platform events without any other logic.

    private void OnNativePlaybackStateChanged(object? sender, PlaybackEventArgs e)
    {
        // Simply forward the event. The ViewModel will handle the logic.
        PlaybackStateChanged?.Invoke(this, e);
    }

    private void OnNativeIsPlayingChanged(object? sender, PlaybackEventArgs e)
    {
        // Simply forward the event. The ViewModel will handle the logic.
        IsPlayingChanged?.Invoke(this, e);
        NotifyPropertyChanged(nameof(IsPlaying)); // Update the property for any direct bindings
        
        // Update visualizer state when playback state changes
        UpdateVisualizerState();
    }

    private void OnNativePlayEnded(object? sender, PlaybackEventArgs e)
    {


        // Simply forward the event.
        PlayEnded?.Invoke(this, e);
    }

    private void OnNativePositionChanged(object? sender, long position)
    {
        // Translate and forward.
        PositionChanged?.Invoke(this, position / 1000.0);
        NotifyPropertyChanged(nameof(CurrentPosition));
    }

    private void OnNativeSeekCompleted(object? sender, double position)
    {
        // The native service confirms the seek is done. NOW we raise our event.
        SeekCompleted?.Invoke(this, position);
    }

    private void OnNativePlayNextPressed(object? sender, PlaybackEventArgs e)
    {
        MediaKeyNextPressed?.Invoke(this, e);
    }

    private void OnNativePlayPreviousPressed(object? sender, PlaybackEventArgs e)
    {
        MediaKeyPreviousPressed?.Invoke(this, e);
    }

    #endregion

    #region PropertyChanged Implementation

    private void NotifyAllPropertiesChanged()
    {
        NotifyPropertyChanged(nameof(IsPlaying));
        NotifyPropertyChanged(nameof(CurrentPosition));
        NotifyPropertyChanged(nameof(Duration));
        NotifyPropertyChanged(nameof(Volume));
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Frequency Visualization

    private IAudioVisualizerService? _visualizerService;

    private void InitializeVisualizer()
    {
        if (_visualizerService == null)
        {
            _visualizerService = new AudioVisualizerService();
            _visualizerService.FftDataAvailable += OnVisualizerFftDataAvailable;
        }
    }

    private void UpdateVisualizerState()
    {
        if (_isFrequencyVisualizationEnabled && IsPlaying && Player != null)
        {
            InitializeVisualizer();
            var audioSessionId = Player.AudioSessionId;
            _visualizerService?.Start(audioSessionId);
        }
        else
        {
            _visualizerService?.Stop();
        }
    }

    private void OnVisualizerFftDataAvailable(object? sender, byte[]? fftData)
    {
        FrequencyDataAvailable?.Invoke(this, fftData);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        DisconnectEvents();
        _visualizerService?.Stop();
        if (_visualizerService != null)
        {
            _visualizerService.FftDataAvailable -= OnVisualizerFftDataAvailable;
            _visualizerService = null;
        }
        _binder = null;
        await Task.CompletedTask;
    }

    public Task SetDefaultAsync(AudioOutputDevice device)
    {
        Service.SetPreferredDevice(device);
        return Task.CompletedTask;
    }

    public Task MuteDevice(bool mute)
    {
        Player.DeviceMuted = mute;
        return Task.CompletedTask;
    }


    public Task SetVolume(double volume)
    {
        Volume = volume;
        return Task.CompletedTask;
    }

    public double GetCurrentVolume()
    {
        
        return Player is null? 0: Player.Volume;
    }

    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        var currentDevice = ExoPlayerService.GetCurrentAudioOutputDevice();
        return currentDevice;

    }

    public Task InitializeAmbienceAsync(string filePath)
    {
        throw new NotImplementedException();
    }

    public void ToggleAmbience(bool isEnabled)
    {
        throw new NotImplementedException();
    }

    public Task SendNextSong(SongModelView nextSong)
    {
        Service?.PrepareNext(nextSong);
        return Task.CompletedTask;
    }
}