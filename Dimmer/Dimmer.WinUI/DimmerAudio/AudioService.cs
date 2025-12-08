using System.Reactive.Subjects;

//using NAudio.CoreAudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi;
using DeviceType = AudioSwitcher.AudioApi.DeviceType;
using System.Threading.Tasks;
namespace Dimmer.WinUI.DimmerAudio;


/// <summary>
/// Provides audio playback services using Windows.Media.Playback.MediaPlayer.
/// Implements IDimmerAudioService, INotifyPropertyChanged, and IAsyncDisposable.
/// Designed for robustness, asynchronous operations, and clear state management.
/// </summary>
public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    #region Singleton & Initialization


    private static readonly Lazy<AudioService> lazyInstance = new(() => new AudioService());
    public static IDimmerAudioService Current => lazyInstance.Value;

    private MediaPlaybackList _playbackList;

    private readonly MediaPlayer _mediaPlayer; 
    private readonly MediaPlayer _ambiencePlayer;
    private readonly DispatcherQueue _dispatcherQueue;
    private CancellationTokenSource? _initializationCts;
    private SongModelView? _currentTrackMetadata;
    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);

    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
    private bool _isDisposed;
    private string? _currentAudioDeviceId;
    private readonly CoreAudioController _controller;
    private readonly object _sync = new();
    public IEnumerable<AudioOutputDevice>? PlaybackDevices
    { get; set; }

    public CoreAudioDevice? DefaultPlaybackDevice
    {
        get
        {
            return _controller.GetDefaultDevice(AudioSwitcher.AudioApi.DeviceType.Playback, AudioSwitcher.AudioApi.Role.Multimedia);
        }
    }

    public AudioService()
    {
        _controller = new CoreAudioController();
        AudioSwitcher.AudioApi.CoreAudio.CoreAudioDevice defaultPlaybackDevice = _controller.DefaultPlaybackDevice;
        //defaultPlaybackDevice.StateChanged += DefaultPlaybackDevice_StateChanged;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("AudioService must be initialized on a thread with a DispatcherQueue (typically the UI thread).");



        _playbackList = new MediaPlaybackList();
        _playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;


        _mediaPlayer = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Media,
            CommandManager = { IsEnabled = true },


        };

        _ambiencePlayer = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.GameMedia, // 'GameMedia' often mixes better as background fx
            IsLoopingEnabled = true, // Crucial: Rain must loop forever
            Volume = 0.5 // Default starting volume
        };
        _ambiencePlayer.CommandManager.IsEnabled = false;

        SubscribeToPlayerEvents();
        SubscribeToSystemEvents();

        MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;


        _volume = _mediaPlayer.Volume;
        _isMuted = _mediaPlayer.IsMuted;
        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);

        _ = Task.Run(async () => await GetSetUpOutPutDevices());
    }

    private void PlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
    {

        if (args.NewItem == null) return;
        var props = args.NewItem.GetDisplayProperties();


    }

    private async Task GetSetUpOutPutDevices()
    {
        var outputDevices = new List<AudioOutputDevice>();
        try
        {

            string selector = MediaDevice.GetAudioRenderSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);

            foreach (var device in devices)
            {
                outputDevices.Add(new AudioOutputDevice { Id = device.Id, Name = device.Name });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error getting audio output devices: {ex}");
            OnErrorOccurred("Failed to enumerate audio output devices.", ex);
        }
        PlaybackDevices = outputDevices;
    }

    private void SubscribeToSystemEvents()
    {
        _controller.AudioDeviceChanged.Subscribe(e =>
        {
            switch (e)
            {

                case DefaultDeviceChangedArgs def: 
                    Debug.WriteLine($"Default changed: {def.Device.Name}"); 
                    break;
                case DeviceAddedArgs add: 
                    Debug.WriteLine($"Device added: {add.Device.Name}"); 
                    break;
                case DeviceRemovedArgs rem: 
                    Debug.WriteLine($"Device removed: {rem.Device.Name}"); 
                    break;
                case DeviceChangedArgs chg: 
                    Debug.WriteLine($"Device property changed: {chg.Device.Name}"); 
                    break;
            }
        });
        
    }
    public AudioOutputDevice? GetCurrentAudioOutputDevice()
    {
        var currentDev = _controller.DefaultPlaybackDevice;
        if (currentDev is null) return null;
        return new AudioOutputDevice
        {
            Id = currentDev.Id.ToString(),
            Name = currentDev.Name,
            IsDefaultDevice = currentDev.IsDefaultDevice,
            IsMuted = currentDev.IsMuted,
            Volume = currentDev.Volume
        };
    }
    public double GetCurrentVolume()
    {
        return _controller.DefaultPlaybackDevice.Volume;
    }

    public async Task SetVolume(double volume)
    {
        var dev = _controller.DefaultPlaybackDevice;
        if (dev != null)
            await dev.SetVolumeAsync(volume);
    }

    public async Task SetDefaultAsync(AudioOutputDevice device)
    {
    
        CoreAudioDevice newDev = _controller.GetDevice(Guid.Parse(device.Id!)) as CoreAudioDevice;
        if (device == null) return;
        await newDev.SetAsDefaultAsync();
    }
    public void WatchVolume()
    {
        var dev = _controller.GetDefaultDevice(DeviceType.Playback, AudioSwitcher.AudioApi.Role.Multimedia);
        dev.VolumeChanged.Subscribe(x =>
        {
            Debug.WriteLine($"Volume: {x.Volume}");
        });
        dev.MuteChanged.Subscribe(x =>
        {
            Debug.WriteLine($"Muted: {x.IsMuted}");
        });
    }
  
    public async Task MuteDevice(bool mute)
    {
        var dev = _controller.DefaultPlaybackDevice;
        if (dev != null)
           await dev.SetMuteAsync(mute);
    }
    private void SubscribeToPlayerEvents()
    {
        _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        _mediaPlayer.PlaybackSession.NaturalDurationChanged += PlaybackSession_NaturalDurationChanged;
        _mediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;
        _mediaPlayer.PlaybackSession.MediaPlayer.VolumeChanged +=MediaPlayer_VolumeChanged;

        _mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
        _mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;


        _mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        _mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
    }

    private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
    {
    }

    private void UnsubscribeFromPlayerEvents()
    {

        if (_mediaPlayer == null)
            return;

        _mediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
        _mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
        _mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;

        var session = _mediaPlayer.PlaybackSession;
        if (session != null)
        {
            session.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            session.PositionChanged -= PlaybackSession_PositionChanged;
            session.NaturalDurationChanged -= PlaybackSession_NaturalDurationChanged;
            session.SeekCompleted -= PlaybackSession_SeekCompleted;
        }

        var commandManager = _mediaPlayer.CommandManager;
        if (commandManager != null)
        {
            commandManager.PlayReceived -= CommandManager_PlayReceived;
            commandManager.PauseReceived -= CommandManager_PauseReceived;
            commandManager.NextReceived -= CommandManager_NextReceived;
            commandManager.PreviousReceived -= CommandManager_PreviousReceived;
            commandManager.IsEnabled = false;
        }
    }

    #endregion

    private double _requestedSeekPosition = -1;
    #region Events (Interface + Additional)


    private EventHandler<PlaybackEventArgs>? _isPlayingChanged;
    public event EventHandler<PlaybackEventArgs> IsPlayingChanged
    {
        add => _isPlayingChanged += value;
        remove => _isPlayingChanged -= value;
    }

    private EventHandler<PlaybackEventArgs>? _playEnded;
    public event EventHandler<PlaybackEventArgs> PlayEnded
    {
        add => _playEnded += value;
        remove => _playEnded -= value;
    }

    private EventHandler<PlaybackEventArgs>? _playStarted;
    public event EventHandler<PlaybackEventArgs> PlayStarted
    {
        add => _playStarted += value;
        remove => _playStarted -= value;
    }


    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? PositionChanged;
    public event EventHandler<double>? SeekCompleted;
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed;
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed;
    public event PropertyChangedEventHandler? PropertyChanged;



    #endregion

    #region Properties

    private DimmerPlaybackState _playbackState = DimmerPlaybackState.PlayCompleted;
    public DimmerPlaybackState CurrentPlaybackState
    {
        get => _playbackState;
        private set => SetProperty(ref _playbackState, value);
    }

    public bool IsPlaying => CurrentPlaybackState == DimmerPlaybackState.Playing;

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
                {
                    RaiseIsPlayingChanged();
                }
            }
        }
    }
    private double _currentPositionValue;
    private readonly BehaviorSubject<double> _currPositionBS = new(0);

    public IObservable<double> CurrPositionObs => _currPositionBS.AsObservable();
    public double CurrentPosition
    {
        get => _currentPositionValue;
        private set
        {
            if ((Math.Abs(_currentPositionValue - value) > 0.1 || Math.Abs(value) < 0.0001 || Math.Abs(value - Duration) < 0.0001))
            {
                _currPositionBS.OnNext(value);
                if (SetProperty(ref _currentPositionValue, value))
                {
                    PositionChanged?.Invoke(this, value);
                }
            }
        }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get
        {
            if (_mediaPlayer is null)
            {
                return _volume;
            }
            else
            {
                return _mediaPlayer.Volume;
            }

        }

        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_mediaPlayer.Volume - clampedValue) > 0.001)
            {
                _mediaPlayer.Volume = clampedValue;

                SetProperty(ref _volume, clampedValue, nameof(Volume));
            }
        }
    }
    private readonly BehaviorSubject<bool?> _isMutedObs = new(false);

    public IObservable<bool?> IsMutedObs => _isMutedObs.AsObservable();

    private bool _isMuted;
    public bool Muted
    {
        get
        {
            return _mediaPlayer.IsMuted;
        }

        set
        {
            if (_mediaPlayer.IsMuted != value)
            {
                _mediaPlayer.IsMuted = value;
                _isMutedObs.OnNext(value);
                SetProperty(ref _isMuted, value, nameof(Muted));
            }
        }
    }


    private double _balance;
    public double Balance
    {
        get => _balance;
        set => SetProperty(ref _balance, Math.Clamp(value, -1.0, 1.0)); // Store value, but no effect yet
    }

    public SongModelView? CurrentTrackMetadata => _currentTrackMetadata;

    private bool _isAmbienceEnabled = false;

    private double _ambienceVolume = 0.5;
    public double AmbienceVolume
    {
        get => _ambienceVolume;
        set
        {
            // Clamp and set
            double clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetProperty(ref _ambienceVolume, clamped))
            {
                if (_ambiencePlayer != null)
                {
                    _ambiencePlayer.Volume = clamped;
                }
            }
        }
    }

    public async Task InitializeAmbienceAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        try
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
            var source = MediaSource.CreateFromStorageFile(file);

            // Create item and set to player
            _ambiencePlayer.Source = new MediaPlaybackItem(source);

            Debug.WriteLine($"[AudioService] Ambience loaded: {filePath}");

            // If music is already playing and ambience is enabled, start it immediately
            if (IsPlaying && _isAmbienceEnabled)
            {
                _ambiencePlayer.Play();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Failed to load ambience: {ex.Message}");
        }
    }

    public void ToggleAmbience(bool isEnabled)
    {
        _isAmbienceEnabled = isEnabled;

        if (_ambiencePlayer.Source == null) return;

        if (isEnabled && IsPlaying)
        {
            _ambiencePlayer.Play();
        }
        else
        {
            _ambiencePlayer.Pause();
        }
    }

    #endregion


    public async Task SendNextSong(SongModelView nextSong)
    {
        var mediaPBItem = await CreateMediaPlaybackItemAsync(nextSong);
        _playbackList.Items.Add(mediaPBItem);

    }

    #region Core Playback Methods (Async)

    /// <summary>
    /// Initializes the player with the specified track metadata and plays at the speficified position. Stops any current playback.
    /// </summary>
    /// <param name="metadata">The metadata of the track to load.</param>
    /// <returns>Task indicating completion.</returns>
    public async Task InitializeAsync(SongModelView songModel,double pos)
    {

        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(songModel);



            CancellationTokenSource? oldCts = null;
            CancellationTokenSource newCts = new CancellationTokenSource();

            lock (this)
            {
                oldCts = _initializationCts;
                _initializationCts = newCts;
            }

            if (oldCts != null)
            {
                Debug.WriteLine("[AudioService] InitializeAsync: Cancelling previous initialization task.");
                await oldCts.CancelAsync();
                oldCts.Dispose();
            }


            var token = newCts.Token;



            _currentTrackMetadata = songModel;
            _currentSong.OnNext(songModel);
            OnPropertyChanged(nameof(CurrentTrackMetadata));


            _mediaPlayer.Pause();
            _playbackList.Items.Clear();
            Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer paused and source nulled.");


            MediaPlaybackItem? mediaPlaybackItem = null;
            bool success = false;

            try
            {
                mediaPlaybackItem = await CreateMediaPlaybackItemAsync(songModel, null, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                if (mediaPlaybackItem != null)
                {
                    _playbackList.Items.Clear(); // Clear previous queue
                    _playbackList.Items.Add(mediaPlaybackItem);
                    success = true;

                    if (pos > 0)
                    {
                        _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(pos);
                    }
                    _mediaPlayer.Source = _playbackList;
                    _mediaPlayer.Play();
                    Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer source SET for {SongTitle}. Waiting for MediaOpened", songModel.Title);
                }
                else
                {
                    Debug.WriteLine("[AudioService] InitializeAsync: CreateMediaPlaybackItemAsync returned null for {SongTitle}. Cannot set source.", songModel.Title);


                }
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine($"[AudioService] InitializeAsync: Operation CANCELED while creating/setting source for {songModel.Title}. {ex.Message}");

            }
            catch(Exception ee)
            {
                Debug.WriteLine(ee.Message);
            }
            finally
            {


                lock (_lockObject)
                {
                    if (_initializationCts == newCts)
                    {
                        _initializationCts = null;
                    }
                }
                newCts.Dispose();

                if (!success)
                {
                    Debug.WriteLine("[AudioService] InitializeAsync: Finalizing with FAILED status for {SongTitle}.", songModel.Title);

                    if (ReferenceEquals(_currentTrackMetadata, songModel))
                    {
                        _currentTrackMetadata = null;
                        OnPropertyChanged(nameof(CurrentTrackMetadata));
                    }
                    UpdatePlaybackState(DimmerPlaybackState.Error);
                    OnErrorOccurred($"Failed to initialize track: {songModel?.Title}", null);
                }


            }
        }
    }

    private void HandleSegmentEnd()
    {
        if (_currentSong == null)
            return;

        
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public void Play(double pos)
    {
        ThrowIfDisposed();
        if (_mediaPlayer.Source == null)
        {
            Debug.WriteLine("[AudioService] PlayAsync called but no source is set.");
        }


        try
        {
            Debug.WriteLine("[AudioService] PlayAsync executing.");
            _mediaPlayer.Play();
            _mediaPlayer.Position = TimeSpan.FromSeconds(pos);
            if (_isAmbienceEnabled && _ambiencePlayer.Source != null)
            {
                _ambiencePlayer.Play();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error calling Play(): {ex}");
            OnErrorOccurred("Failed to start playback.", ex);
            UpdatePlaybackState(DimmerPlaybackState.Error);
        }
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public void Pause()
    {
        ThrowIfDisposed();
        if (_mediaPlayer.PlaybackSession.CanPause)
        {

            try
            {
                Debug.WriteLine("[AudioService] PauseAsync executing.");
                _mediaPlayer.Pause();
                if (_ambiencePlayer.PlaybackSession.CanPause)
                {
                    _ambiencePlayer.Pause();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Error calling Pause(): {ex}");

                OnErrorOccurred("Failed to pause playback.", ex);
            }
        }
        else
        {
            Debug.WriteLine("[AudioService] PauseAsync called but cannot pause in current state.");
        }
    }

    /// <summary>
    /// Stops playback, resets position, and clears the current source.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public void Stop()
    {
        try
        {

        ThrowIfDisposed();
        Debug.WriteLine("[AudioService] StopAsync executing.");
        _mediaPlayer.Pause();
        _mediaPlayer.Source = null;
        _currentTrackMetadata = null;
        OnPropertyChanged(nameof(CurrentTrackMetadata));
        CurrentPosition = 0;
        Duration = 0;
        UpdatePlaybackState(DimmerPlaybackState.PausedDimmer);

            _ambiencePlayer.Pause();
            _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = null;

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Seeks to the specified position in seconds.
    /// </summary>
    /// <param name="positionSeconds">The target position in seconds.</param>
    /// <returns>Task indicating completion of the seek request (not necessarily the completion of the seek operation itself).</returns>
    public void Seek(double positionSeconds)
    {
        ThrowIfDisposed(); 

        if (_mediaPlayer.PlaybackSession.CanSeek)
        {
            
            var targetPositionSeconds = Math.Clamp(positionSeconds, 0, _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds);
            var targetPosition = TimeSpan.FromSeconds(targetPositionSeconds);

            
            if (Math.Abs(_mediaPlayer.PlaybackSession.Position.TotalSeconds - targetPosition.TotalSeconds) > 0.2)
            {
                
                _requestedSeekPosition = targetPositionSeconds;

                Debug.WriteLine($"[AudioService] Storing requested position ({_requestedSeekPosition}) and seeking to: {targetPosition}");

                
                _mediaPlayer.PlaybackSession.Position = targetPosition;
            }
        }
        else
        {
            
            CurrentPosition = positionSeconds;
            Debug.WriteLine("[AudioService] Seek requested but session cannot seek.");
        }
    }

    #endregion

    #region Media Item Creation

    private static async Task<MediaPlaybackItem?> CreateMediaPlaybackItemAsync(SongModelView media, byte[]? ImageBytes = null, CancellationToken token = default)
    {


        if (string.IsNullOrWhiteSpace(media.FilePath))
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: No FilePath for '{media.Title ?? "Unknown"}', cannot create item.");
            return null;
        }

        Uri? uri = null;
        StorageFile? storageFile = null;

        try
        {

            if (Uri.TryCreate(media.FilePath, UriKind.Absolute, out var parsedUri) && !parsedUri.IsFile)
            {
                uri = parsedUri;
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Using direct URI: {uri} for '{media.Title}'");
            }
            else
            {
                string fullPath = Path.GetFullPath(media.FilePath);
                if (!File.Exists(fullPath))
                {
                    Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: File does not exist at resolved path '{fullPath}' for '{media.Title}'.");
                    return null;
                }


                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Attempting StorageFile for path: {fullPath} for '{media.Title}'");
                storageFile = await StorageFile.GetFileFromPathAsync(fullPath).AsTask(token);
            }

            token.ThrowIfCancellationRequested();

            MediaSource? mediaSource;
            if (storageFile != null)
            {
                mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from StorageFile for '{media.Title}'. ContentType: {storageFile.ContentType}");
            }
            else if (uri != null)
            {
                mediaSource = MediaSource.CreateFromUri(uri);
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from URI for '{media.Title}'.");
            }
            else
            {

                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Could not determine how to create MediaSource for '{media.Title}'.");
                return null;
            }


            var mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            var props = mediaPlaybackItem.GetDisplayProperties();
            props.Type = MediaPlaybackType.Music;

            props.MusicProperties.Title = media.Title ?? Path.GetFileNameWithoutExtension(media.FilePath) ?? "Unknown Title";
            props.MusicProperties.Artist = media.Id.ToString();
            props.MusicProperties.AlbumTitle = media.AlbumName ?? string.Empty;
            props.MusicProperties.AlbumArtist = media.ArtistName;
            if (!string.IsNullOrEmpty(media.CoverImagePath) && File.Exists(media.CoverImagePath))
            {
                try
                {
                    var coverFile = await StorageFile.GetFileFromPathAsync(media.CoverImagePath);
                    props.Thumbnail = RandomAccessStreamReference.CreateFromFile(coverFile);
                    Debug.WriteLine($"[AudioService] Successfully created thumbnail reference for '{media.Title}'.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AudioService] Error creating thumbnail for '{media.Title}' from path '{media.CoverImagePath}': {ex.Message}");
                    // Optionally, set a default placeholder image here
                }
            }
            else
            {
                Debug.WriteLine($"[AudioService] Cover image path is missing or file does not exist for '{media.Title}'. Path: '{media.CoverImagePath}'");
                // Optionally, set a default placeholder image here
            }
            mediaPlaybackItem.ApplyDisplayProperties(props);
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Successfully created MediaPlaybackItem for '{media.Title}'.");
            return mediaPlaybackItem;
            
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Operation CANCELED for '{media.Title ?? media.FilePath}'.");
            throw;
        }
        catch (FileNotFoundException fnfEx)
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: File not found for '{media.FilePath}': {fnfEx.Message}");
            return null;
        }
        catch (UnauthorizedAccessException uaEx)
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Access denied for '{media.FilePath}': {uaEx.Message}. Check capabilities (e.g., broadFileSystemAccess) or file permissions.");
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Generic error creating MediaSource for '{media.FilePath}': {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Player Event Handlers

    private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        MediaPlaybackState winuiState = sender.PlaybackState;
        var newState = ConvertPlaybackState(winuiState);
        Debug.WriteLine($"[AudioService] PlaybackStateChanged: {winuiState} -> {newState}");
        if (newState.Item2)
        {
            UpdatePlaybackState(newState.Item1);
        }
    }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        CurrentPosition = sender.Position.TotalSeconds;
    }

    private void PlaybackSession_NaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        var newDuration = sender.NaturalDuration.TotalSeconds;

        if (newDuration > 0)
        {
            Debug.WriteLine($"[AudioService] NaturalDurationChanged: {newDuration}");
            Duration = newDuration;
        }
    }

    private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
    {

        if (_requestedSeekPosition >= 0)
        {
            var confirmedPosition = _requestedSeekPosition;
            _requestedSeekPosition = -1; // Reset for the next operation

            // This debug line will now show the CORRECT value
            Debug.WriteLine($"[AudioService] PlaybackSession_SeekCompleted fired. Using confirmed position: {confirmedPosition}");

            // Update your service's internal state
            CurrentPosition = confirmedPosition;

            // Invoke your custom event with the RELIABLE data
            SeekCompleted?.Invoke(this, confirmedPosition);
        }
        else
        {
            // This might happen if the player seeks for its own reasons (e.g., buffering).
            // You can decide if you want to handle this or just log it.
            Debug.WriteLine($"[AudioService] PlaybackSession_SeekCompleted fired unexpectedly. Sender position: {sender.Position.TotalSeconds}");
        }
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {

        Debug.WriteLine($"[AudioService] MediaOpened: {_currentTrackMetadata?.Title ?? "Unknown"}");
        Duration = sender.PlaybackSession.NaturalDuration.TotalSeconds;
        CurrentPosition = sender.PlaybackSession.Position.TotalSeconds;
        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.Playing };
        _playStarted?.Invoke(this, eventArgs);
        
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        Debug.WriteLine($"[AudioService] MediaEnded: {_currentTrackMetadata?.Title ?? "Unknown"}");
        _ambiencePlayer.Pause();
        CurrentPosition = Duration;
        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted);


        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayCompleted };
        _playEnded?.Invoke(this, eventArgs);

    }
        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] MediaFailed: Error={args.Error}, Code={args.ExtendedErrorCode}, Msg={args.ErrorMessage}");
        OnErrorOccurred($"Playback failed: {args.ErrorMessage}", args.ExtendedErrorCode, args.Error);


        _currentTrackMetadata = null;
        OnPropertyChanged(nameof(CurrentTrackMetadata));
        UpdatePlaybackState(DimmerPlaybackState.Error);
        CurrentPosition = 0;
        Duration = 0;
    }

    #endregion

    #region SMTC Command Handlers

    private void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Play Received");

        var deferral = args.GetDeferral();
        try
        {
            if (_mediaPlayer.Source != null)
            {
                Play(CurrentPosition);
                args.Handled = true;
            }
            else
            {
                args.Handled = false;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Pause Received");
        var deferral = args.GetDeferral();
        try
        {
            if (_mediaPlayer.PlaybackSession.CanPause)
            {
                Pause();
                args.Handled = true;
            }
            else
            {
                args.Handled = false;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Next Received");

        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType= DimmerPlaybackState.PlayNextUser };

        MediaKeyNextPressed?.Invoke(this, eventArgs);
        args.Handled = true;
    }

    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Previous Received");

        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayPreviousUser };
        MediaKeyPreviousPressed?.Invoke(this, eventArgs);
        args.Handled = true;
    }

    #endregion

    #region Audio Output Management

    /// <summary>
    /// Gets a list of available audio output devices.
    /// </summary>
    public async Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync()
    {
        ThrowIfDisposed();
      
        var outputDevices = new List<AudioOutputDevice>();
        try
        {

            string selector = MediaDevice.GetAudioRenderSelector();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(selector);

            foreach (var device in devices)
            {
                outputDevices.Add(new AudioOutputDevice { Id = device.Id, Name = device.Name });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error getting audio output devices: {ex}");
            OnErrorOccurred("Failed to enumerate audio output devices.", ex);
        }
        return outputDevices;
    }

    /// <summary>
    /// Sets the audio output device for the MediaPlayer.
    /// </summary>
    /// <param name="deviceId">The ID of the device to use, or null to use the system default.</param>
    public async Task SetAudioOutputDeviceAsync(string? deviceId)
    {
        ThrowIfDisposed();
        try
        {
            DeviceInformation? deviceInfo = null;
            if (!string.IsNullOrEmpty(deviceId))
            {
                deviceInfo = await DeviceInformation.CreateFromIdAsync(deviceId);
            }


            _mediaPlayer.AudioDevice = deviceInfo;
            _ambiencePlayer.AudioDevice = deviceInfo;
            _currentAudioDeviceId = deviceInfo?.Id;
            Debug.WriteLine($"[AudioService] Audio output device set to: {deviceInfo?.Name ?? "System Default"} (ID: {_currentAudioDeviceId})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error setting audio output device (ID: {deviceId}): {ex}");
            OnErrorOccurred($"Failed to set audio output device to {deviceId}.", ex);
        }
    }


    private async void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] System default audio render device changed. Role: {args.Role}, New ID: {args.Id}");









        if (!string.IsNullOrEmpty(_currentAudioDeviceId) && _currentAudioDeviceId != args.Id)
        {

            try
            {
                var currentDevice = await DeviceInformation.CreateFromIdAsync(_currentAudioDeviceId);

                Debug.WriteLine($"[AudioService] Still using explicitly selected device: {currentDevice.Name}");
            }
            catch
            {

                Debug.WriteLine($"[AudioService] Previously selected device ID {_currentAudioDeviceId} is no longer valid. Resetting to default.");
                await SetAudioOutputDeviceAsync(null);
            }
        }
        else if (string.IsNullOrEmpty(_currentAudioDeviceId))
        {

            Debug.WriteLine("[AudioService] Using system default, MediaPlayer should adapt.");
        }
    }


    #endregion

    #region State Management & Helpers

    private void UpdatePlaybackState(DimmerPlaybackState newState)
    {

        if (SetProperty(ref _playbackState, newState, nameof(CurrentPlaybackState)))
        {

            OnPropertyChanged(nameof(IsPlaying));



            var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  newState };
            PlaybackStateChanged?.Invoke(this, args);


            RaiseIsPlayingChanged();

        }
    }


    private static (DimmerPlaybackState, bool) ConvertPlaybackState(MediaPlaybackState state)
    {
        switch (state)
        {
            case MediaPlaybackState.None:
                return (DimmerPlaybackState.None, false);

            case MediaPlaybackState.Opening:
                return (DimmerPlaybackState.Opening, false);
            case MediaPlaybackState.Buffering:
                return (DimmerPlaybackState.Buffering, false);
            case MediaPlaybackState.Playing:
                return (DimmerPlaybackState.Playing, true);
            case MediaPlaybackState.Paused:
                return (DimmerPlaybackState.PausedDimmer, true);
            default:
                return (DimmerPlaybackState.PlayCompleted, true);
        }

    }

    private void RaiseIsPlayingChanged()
    {
        // Use current state to construct the event args
        DimmerPlaybackState eventType = IsPlaying ? DimmerPlaybackState.Playing : DimmerPlaybackState.PausedDimmer;

        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  eventType };
        _isPlayingChanged?.Invoke(this, args);
    }

    private void OnErrorOccurred(string message, Exception? exception = null, MediaPlayerError? playerError = null)
    {

        Debug.WriteLine($"[AudioService ERROR] {message} | Exception: {exception?.Message} | PlayerError: {playerError}");
        
        DimmerPlaybackState dimmerPBError =DimmerPlaybackState.Error;
        if(playerError is not null)
        {
            dimmerPBError = playerError.Value == MediaPlayerError.SourceNotSupported ? DimmerPlaybackState.ErrorAudioSourceNotSupported : DimmerPlaybackState.Error;
        }
        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType= dimmerPBError };
        ErrorOccurred?.Invoke(this, args);
    }


    #endregion


    #region INotifyPropertyChanged

    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;


        _dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
        return true;
    }


    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }

    #endregion

    #region IAsyncDisposable

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(AudioService));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        Debug.WriteLine("[AudioService] Starting asynchronous disposal...");


        MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;


        if (_initializationCts is not null)
        {
            await _initializationCts.CancelAsync();
        }
        _initializationCts?.Dispose();
        _initializationCts = null;


        _mediaPlayer?.Pause();
        _mediaPlayer?.Source = null;

        _controller?.Dispose();

        UnsubscribeFromPlayerEvents();
        try
        {
            _ambiencePlayer?.Pause();
            _ambiencePlayer?.Source = null;
            _ambiencePlayer?.Dispose();
        }
        catch { /* Ignore ambience dispose errors */ }


        _mediaPlayer?.Dispose();
        Debug.WriteLine("[AudioService] MediaPlayer disposed.");


        _isPlayingChanged = null;
        _playEnded = null;
        _playStarted = null;
        PlaybackStateChanged = null;
        ErrorOccurred = null;
        DurationChanged = null;
        PositionChanged = null;
        SeekCompleted = null;
        MediaKeyNextPressed = null;
        MediaKeyPreviousPressed = null;
        PropertyChanged = null;

        Debug.WriteLine("[AudioService] Asynchronous disposal complete.");


        await Task.CompletedTask;
    }


    #endregion

    /// <summary>
    /// Copies data from a regular Stream to an IRandomAccessStream.
    /// </summary>
    /// <param name="fileStream">The source stream.</param>
    /// <param name="randomAccessStream">The target random access stream.</param>
    /// <param name="token">A cancellation token.</param>
    /// <param name="progressHandler">A progress handler reporting the number of bytes copied.</param>
    public static async Task CopyFileStreamToRandomAccessStreamAsync(Stream fileStream, IRandomAccessStream randomAccessStream, CancellationToken token, IProgress<long> progressHandler)
    {

        using (Stream outputStream = randomAccessStream.GetOutputStreamAt(0).AsStreamForWrite())
        {

            const int bufferSize = 81920;
            byte[] buffer = new byte[bufferSize];
            long totalBytesCopied = 0;
            int bytesRead;


            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead, token);
                totalBytesCopied += bytesRead;
                progressHandler?.Report(totalBytesCopied);
            }


            await outputStream.FlushAsync(token);
            token.ThrowIfCancellationRequested();
        }
    }

    private readonly object _lockObject = new object();



 

    public bool SetPreferredOutputDevice(AudioOutputDevice dev)
    {
        if (dev?.Id == null)
            return false;

        try
        {
            if (dev?.Id == null)
                return false;

            try
            {
                // The library works with its own device objects. Get it by its ID.
                // The ID from NAudio is compatible.
                var deviceToSet = _controller.GetDevice(new Guid(dev.Id));
                
                if (deviceToSet == null)
                {
                    Debug.WriteLine($"Device with ID {dev.Id} not found by AudioSwitcher.");
                    return false;
                }

                // This one line does it all. It's clean, safe, and readable.
                deviceToSet.SetAsDefault();
                // You can also set the communications default separately if needed
                deviceToSet.SetAsDefaultCommunications();

                Debug.WriteLine($"Successfully set default audio output device to: {dev.Name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting default audio device: {ex.Message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in SetPreferredOutputDevice: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a list of all active audio output devices using AudioSwitcher for consistency.
    /// </summary>
    public List<AudioOutputDevice> GetAllAudioDevices()
    {
        // Get all active playback devices from the controller.
        //var devices = _audioController.GetPla ybackDevices(AudioSwitcher.AudioApi.DeviceState.Active);
        IEnumerable<CoreAudioDevice>? devices = _controller.GetPlaybackDevices(AudioSwitcher.AudioApi.DeviceState.Active)
            . Where(x=>x.DeviceType == AudioSwitcher.AudioApi.DeviceType.Playback);

        // Map them to your own simple model.
        return devices.Select(d => new AudioOutputDevice
        {
            // Note: The library provides the ID as a Guid. Convert to string.
            Id = d.Id.ToString(),
            Name = d.FullName,
            Type = d.DeviceType.ToString(),
            ProductName = d.InterfaceName,
            IsPlaybackDevice=d.IsPlaybackDevice,
            IconString=d.Icon.ToString(),
            State=d.State.ToString(),
            Volume= d.Volume,
            IsMuted=d.IsMuted,
            IsDefaultCommunicationsDevice=d.IsDefaultCommunicationsDevice,
            IsDefaultDevice=d.IsDefaultDevice,

        }).ToList();
    }


    public void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels)
    {
        try
        {

            Task.Run(async () => await InitializeAsync(songModelView,0));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

}

