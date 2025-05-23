using static Vanara.PInvoke.Kernel32;

namespace Dimmer.WinUI.DimmerAudio;


/// <summary>
/// Provides audio playback services using Windows.Media.Playback.MediaPlayer.
/// Implements IDimmerAudioService, INotifyPropertyChanged, and IAsyncDisposable.
/// Designed for robustness, asynchronous operations, and clear state management.
/// </summary>
public partial class AudioService : IDimmerAudioService, INotifyPropertyChanged, IAsyncDisposable
{
    #region Singleton & Initialization

    // Thread-safe singleton pattern
    private static readonly Lazy<AudioService> lazyInstance = new(() => new AudioService());
    public static IDimmerAudioService Current => lazyInstance.Value;

    private readonly MediaPlayer _mediaPlayer;
    private readonly DispatcherQueue _dispatcherQueue; // For UI thread safety if needed
    private CancellationTokenSource? _initializationCts;
    private SongModelView? _currentTrackMetadata;
    private bool _isDisposed;
    private string? _currentAudioDeviceId; // Store the ID of the explicitly selected output device

    public AudioService()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("AudioService must be initialized on a thread with a DispatcherQueue (typically the UI thread).");

        _mediaPlayer = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Media,
            CommandManager = { IsEnabled = true } // Enable SMTC integration early
        };

        // SubscribeAsync to events for state management and cleanup
        SubscribeToPlayerEvents();

        // SubscribeAsync to audio device changes
        MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;

        // Initial state setup
        _volume = _mediaPlayer.Volume;
        _isMuted = _mediaPlayer.IsMuted;
        UpdatePlaybackState(DimmerPlaybackState.Stopped); // Initial state
    }

    private void SubscribeToPlayerEvents()
    {
        _mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        _mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        _mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

        _mediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
        _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
        _mediaPlayer.PlaybackSession.NaturalDurationChanged += PlaybackSession_NaturalDurationChanged;
        _mediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted; // Useful for knowing when seek is done
        _mediaPlayer.PlaybackSession.MediaPlayer.VolumeChanged +=MediaPlayer_VolumeChanged;
        // SMTC Command Handlers
        _mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
        _mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
        // Add more command handlers if needed (e.g., Shuffle, Repeat)

        // Enabling rules (can be adjusted based on playlist logic)
        _mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
        _mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
    }

    private void MediaPlayer_VolumeChanged(MediaPlayer sender, object args)
    {
        
    }

    private void UnsubscribeFromPlayerEvents()
    {
        // Check if player exists before unsubscribing
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
            commandManager.IsEnabled = false; // Disable SMTC on dispose
        }
    }

    #endregion

    #region Events (Interface + Additional)

    // Interface Events (using backing fields for safety)
    private EventHandler<PlaybackEventArgs>? _isPlayingChanged;
    event EventHandler<PlaybackEventArgs> IDimmerAudioService.IsPlayingChanged
    {
        add => _isPlayingChanged += value;
        remove => _isPlayingChanged -= value;
    }

    private EventHandler<PlaybackEventArgs>? _playEnded;
    event EventHandler<PlaybackEventArgs> IDimmerAudioService.PlayEnded
    {
        add => _playEnded += value;
        remove => _playEnded -= value;
    }

    // Additional, more granular events
    public event EventHandler<PlaybackEventArgs>? PlaybackStateChanged; // More detailed state
    public event EventHandler<PlaybackEventArgs>? ErrorOccurred;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? PositionChanged; // Fired frequently
    public event EventHandler<double>? SeekCompleted; // Fired after seek finishes
    public event EventHandler<PlaybackEventArgs>? MediaKeyNextPressed; // Raised by SMTC Next command
    public event EventHandler<PlaybackEventArgs>? MediaKeyPreviousPressed; // Raised by SMTC Previous command
    public event PropertyChangedEventHandler? PropertyChanged;
    // public event EventHandler<long>? IsSeekedFromNotificationBar; // Consider using SeekCompleted instead
    // public event EventHandler? PlayStopAndShowWindow; // UI concern, better handled in ViewModel

    #endregion

    #region Properties

    private DimmerPlaybackState _playbackState = DimmerPlaybackState.Stopped;
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
                // Also raise PlaybackEventArgs when duration changes while playing/paused
                if (IsPlaying || CurrentPlaybackState == DimmerPlaybackState.PausedUI)
                {
                    RaiseIsPlayingChanged();
                }
            }
        }
    }

    public double CurrentPosition
    {
        get;
        private set
        {
            // Reduce noise by checking for significant change
            if ((Math.Abs(field - value) > 0.1 || Math.Abs(value) < 0.0001 || Math.Abs(value - Duration) < 0.0001)&&SetProperty(ref field, value))
            {
                PositionChanged?.Invoke(this, value);
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
                return _mediaPlayer.Volume; // Directly get from player
            }
                
        }

        set
        {
            var clampedValue = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_mediaPlayer.Volume - clampedValue) > 0.001)
            {
                _mediaPlayer.Volume = clampedValue;
                // No need to cache separately unless player is disposed/recreated often
                SetProperty(ref _volume, clampedValue, nameof(Volume)); // Update backing field for INPC
            }
        }
    }

    private bool _isMuted;
    public bool Muted
    {
        get => _mediaPlayer.IsMuted;
        set
        {
            if (_mediaPlayer.IsMuted != value)
            {
                _mediaPlayer.IsMuted = value;
                SetProperty(ref _isMuted, value, nameof(Muted));
            }
        }
    }

    // Balance requires AudioGraph API, not directly supported by MediaPlayer
    private double _balance;
    public double Balance
    {
        get => _balance;
        set => SetProperty(ref _balance, Math.Clamp(value, -1.0, 1.0)); // Store value, but no effect yet        
    }

    public SongModelView? CurrentTrackMetadata => _currentTrackMetadata;

    #endregion

    #region Core Playback Methods (Async)

    /// <summary>
    /// Initializes the player with the specified track metadata. Stops any current playback.
    /// </summary>
    /// <param name="metadata">The metadata of the track to load.</param>
    /// <returns>Task indicating completion.</returns>
    public async Task InitializeAsync(SongModelView songModel, byte[]? SongCoverImage)
    {
        // 1) guard empty or null paths
       
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(songModel);

        // Cancel previous initialization if any
        if (_initializationCts is not null)
        {
            await _initializationCts.CancelAsync();
        }
        _initializationCts = new CancellationTokenSource();
        var token = _initializationCts.Token;

        _currentTrackMetadata = songModel;
        Debug.WriteLine($"[AudioService] Initializing track: {songModel.Title ?? "Unknown"}");
        OnPropertyChanged(nameof(CurrentTrackMetadata));

        // Ensure player is stopped before changing source
        _mediaPlayer.Pause(); // Pause first
        _mediaPlayer.Source = null; // Clear existing source immediately

        // Reset position/duration before loading new media
        CurrentPosition = 0;
        Duration = 0;

        try
        {
            var mediaPlaybackItem = await CreateMediaPlaybackItemAsync(songModel, SongCoverImage, token);
            token.ThrowIfCancellationRequested();

            if (mediaPlaybackItem != null)
            {
                _mediaPlayer.Source = mediaPlaybackItem;
                // State will transition via MediaOpened or MediaFailed events
                Debug.WriteLine($"[AudioService] Source set for: {songModel.Title ?? "Unknown"}");
            }
            else
            {
                // Handle case where item creation failed but wasn't cancelled
                throw new InvalidOperationException("Failed to create MediaPlaybackItem.");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("[AudioService] Initialization cancelled.");
            // State might remain Opening or revert based on subsequent actions
            if (_mediaPlayer.Source == null) // If cancellation happened before setting source
            {
                UpdatePlaybackState(DimmerPlaybackState.Stopped);
                _currentTrackMetadata = null; // Clear metadata if initialization was fully cancelled
                OnPropertyChanged(nameof(CurrentTrackMetadata));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error initializing track '{songModel?.Title}': {ex}");
            OnErrorOccurred($"Failed to load track: {songModel?.Title}", ex);
            UpdatePlaybackState(DimmerPlaybackState.Error);
            _currentTrackMetadata = null;
            OnPropertyChanged(nameof(CurrentTrackMetadata));
            // Ensure player source is null after failure
            _mediaPlayer.Source = null;
        }
        finally
        {
            // Clean up CTS
            _initializationCts?.Dispose();
            _initializationCts = null;
        }
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public Task PlayAsync()
    {
        ThrowIfDisposed();
        if (_mediaPlayer.Source == null)
        {
            Debug.WriteLine("[AudioService] PlayAsync called but no source is set.");
            return Task.CompletedTask; // Nothing to play
        }

        // Play is synchronous, but return Task for interface consistency
        try
        {
            Debug.WriteLine("[AudioService] PlayAsync executing.");
            _mediaPlayer.Play();
            // State update will happen via PlaybackStateChanged event
        }
        catch (Exception ex) // Catch potential errors during Play() call
        {
            Debug.WriteLine($"[AudioService] Error calling Play(): {ex}");
            OnErrorOccurred("Failed to start playback.", ex);
            UpdatePlaybackState(DimmerPlaybackState.Error); // Reflect failure state
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pauses playback.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public Task PauseAsync()
    {
        ThrowIfDisposed();
        if (_mediaPlayer.PlaybackSession.CanPause)
        {
            // Pause is synchronous
            try
            {
                Debug.WriteLine("[AudioService] PauseAsync executing.");
                _mediaPlayer.Pause();
                // State update will happen via PlaybackStateChanged event
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Error calling Pause(): {ex}");
                // Less likely to fail, but good to handle
                OnErrorOccurred("Failed to pause playback.", ex);
            }
        }
        else
        {
            Debug.WriteLine("[AudioService] PauseAsync called but cannot pause in current state.");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops playback, resets position, and clears the current source.
    /// </summary>
    /// <returns>Task indicating completion.</returns>
    public Task StopAsync()
    {
        ThrowIfDisposed();
        Debug.WriteLine("[AudioService] StopAsync executing.");
        _mediaPlayer.Pause(); // Ensure paused state first
        _mediaPlayer.Source = null; // Release the media source
        _currentTrackMetadata = null;
        OnPropertyChanged(nameof(CurrentTrackMetadata));
        CurrentPosition = 0;
        Duration = 0;
        UpdatePlaybackState(DimmerPlaybackState.Stopped); // Explicitly set stopped state

        // Cancel any pending initialization
        _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = null;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Seeks to the specified position in seconds.
    /// </summary>
    /// <param name="positionSeconds">The target position in seconds.</param>
    /// <returns>Task indicating completion of the seek request (not necessarily the completion of the seek operation itself).</returns>
    public Task SeekAsync(double positionSeconds)
    {
        ThrowIfDisposed();
        if (_mediaPlayer.PlaybackSession.CanSeek)
        {
            var targetPosition = TimeSpan.FromSeconds(Math.Clamp(positionSeconds, 0, Duration));
            // Only seek if the position is significantly different
            if (Math.Abs(_mediaPlayer.PlaybackSession.Position.TotalSeconds - targetPosition.TotalSeconds) > 0.2)
            {
                Debug.WriteLine($"[AudioService] Seeking to: {targetPosition}");
                _mediaPlayer.PlaybackSession.Position = targetPosition;
                // Actual position update and SeekCompleted event will fire asynchronously
            }
        }
        else
        {
            Debug.WriteLine("[AudioService] SeekAsync requested but cannot seek.");
        }
        return Task.CompletedTask; // Position setting is synchronous
    }

    #endregion

    #region Media Item Creation

    private static async Task<MediaPlaybackItem?> CreateMediaPlaybackItemAsync(SongModelView media, byte[]? ImageBytes = null, CancellationToken token=default)
    {

        // 1) guard empty or null paths
        if (string.IsNullOrWhiteSpace(media.FilePath))
        {
            Debug.WriteLine($"[AudioService] No FilePath for '{media.Title}', skipping MediaSource.");
            return null;
        }
        // 2) try parse into a URI
        if (!Uri.TryCreate(media.FilePath, UriKind.Absolute, out var uri))
        {
            // maybe it’s a local Windows path, so force file Uri
            var full = Path.GetFullPath(media.FilePath);
            uri = new Uri(full.StartsWith("\\\\")
                ? $"file:///{full}"      // UNC
                : new UriBuilder { Scheme = "file", Path = full }.Uri.ToString());
            Debug.WriteLine($"[AudioService] Forced file‐URI: {uri}");
        }

        MediaSource? mediaSource = null;
        string? mimeType = null;
       
            Debug.WriteLine($"[AudioService] Attempting to create MediaSource from URI: {uri}");
            try
            {
                StorageFile? storageFile = null;
                if (uri.IsFile)
                {
                    // Using StorageFile.GetFileFromPathAsync is generally more robust for file access permissions
                    storageFile = await StorageFile.GetFileFromPathAsync(media.FilePath).AsTask(token);
                    
                    mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                    mimeType = storageFile.ContentType; // Get MIME type from StorageFile
                    Debug.WriteLine($"[AudioService] Created MediaSource from StorageFile. ContentType: {mimeType}");
                }
                else // Handle non-file URIs (e.g., http, ms-appdata)
                {
                    // Note: Network streams might require background media capabilities in Package.appxmanifest
                    mediaSource = MediaSource.CreateFromUri(uri);
                    Debug.WriteLine($"[AudioService] Created MediaSource directly from URI: {uri}");
                    
                }
            }
            catch (FileNotFoundException fnfEx)
            {
                Debug.WriteLine($"[AudioService] File not found at URI '{media.FilePath}': {fnfEx.Message}. Will attempt stream fallback if possible.");
                mediaSource = null;
            }
            catch (UnauthorizedAccessException uaEx)
            {
                Debug.WriteLine($"[AudioService] Access denied for URI '{media.FilePath}': {uaEx.Message}. Check capabilities (e.g., broadFileSystemAccess) or file permissions. Will attempt stream fallback.");
                mediaSource = null;
            }
            catch (OperationCanceledException) { throw; } // Re-throw cancellation
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Error creating MediaSource from URI '{media.FilePath}': {ex.Message}. Will attempt stream fallback.");
                mediaSource = null;
            }
        


        // --- Now proceed ONLY if mediaSource is NOT NULL ---
        if (mediaSource == null)
        {
            // This means both URI and Stream attempts failed or were cancelled
            Debug.WriteLine($"[AudioService] Failed to create MediaSource for '{media.Title}'.");
            // Optionally call OnErrorOccurred again or ensure it was called appropriately before
            return null;
        }

        // --- Create MediaPlaybackItem and add Metadata ---
        var mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
        var props = mediaPlaybackItem.GetDisplayProperties(); // Get the properties object

        props.Type = MediaPlaybackType.Music; // Tell the system it's music

        // Set Music Specific Properties from your SongModel
        props.MusicProperties.Title = media.Title ?? Path.GetFileNameWithoutExtension(media.FilePath) ?? "Unknown Title"; // Use Title, fallback to FileName, then default
        props.MusicProperties.Artist = media.ArtistName ?? "Unknown Artist";
        props.MusicProperties.AlbumTitle = media.AlbumName ?? string.Empty;
        
        // Handle Thumbnail (if image bytes are available in your model)
        if (ImageBytes != null && ImageBytes.Length > 0)
        {
            try
            {
                // Create a stream for the thumbnail
                using var imageStream = new InMemoryRandomAccessStream();
                using (var writer = new DataWriter(imageStream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(ImageBytes);
                    await writer.StoreAsync().AsTask(token); // Use await and token
                    await writer.FlushAsync().AsTask(token); // Ensure data is written
                }
                //token.ThrowIfCancellationRequested(); // Check cancellation
                imageStream.Seek(0); // Reset stream position

                // Create the reference and assign it
                props.Thumbnail = RandomAccessStreamReference.CreateFromStream(imageStream);
            }
            catch (OperationCanceledException) { throw; } // Re-throw cancellation
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioService] Error creating thumbnail stream: {ex.Message}");
                // Non-critical error, continue without thumbnail
            }
        }

        // Apply the configured properties back to the item!
        mediaPlaybackItem.ApplyDisplayProperties(props);
        return mediaPlaybackItem;

    }

    #endregion

    #region Player Event Handlers

            private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
            {
                MediaPlaybackState winuiState = sender.PlaybackState;
                var newState = ConvertPlaybackState(winuiState);
                Debug.WriteLine($"[AudioService] PlaybackStateChanged: {winuiState} -> {newState}");
                UpdatePlaybackState(newState);
            }

    private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        CurrentPosition = sender.Position.TotalSeconds;
    }

    private void PlaybackSession_NaturalDurationChanged(MediaPlaybackSession sender, object args)
    {
        var newDuration = sender.NaturalDuration.TotalSeconds;
        // Sometimes duration might be reported as 0 initially, ignore until valid
        if (newDuration > 0)
        {
            Debug.WriteLine($"[AudioService] NaturalDurationChanged: {newDuration}");
            Duration = newDuration;
        }
    }

    private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
    {
        var seekedPosition = sender.Position.TotalSeconds;
        Debug.WriteLine($"[AudioService] SeekCompleted at: {seekedPosition}");
        CurrentPosition = seekedPosition; // Ensure property is accurate post-seek
        SeekCompleted?.Invoke(this, seekedPosition);
        // IsSeekedFromNotificationBar?.Invoke(this, (long)seekedPosition * 1000); // If needed
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        // Media is ready to play (or may have already started buffering/playing)
        Debug.WriteLine($"[AudioService] MediaOpened: {_currentTrackMetadata?.Title ?? "Unknown"}");
        Duration = sender.PlaybackSession.NaturalDuration.TotalSeconds;
        CurrentPosition = sender.PlaybackSession.Position.TotalSeconds; // Get current pos

    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        Debug.WriteLine($"[AudioService] MediaEnded: {_currentTrackMetadata?.Title ?? "Unknown"}");
        // Set position exactly to duration
        CurrentPosition = Duration;
        UpdatePlaybackState(DimmerPlaybackState.Stopped); // Set specific ended state

        // Raise the specific PlayEnded event (as per interface)
        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.Ended };
        _playEnded?.Invoke(this, eventArgs);

    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] MediaFailed: Error={args.Error}, Code={args.ExtendedErrorCode}, Msg={args.ErrorMessage}");
        OnErrorOccurred($"Playback failed: {args.ErrorMessage}", args.ExtendedErrorCode, args.Error);

        // Try to clean up
        sender.Source = null; // Clear the failed source
        _currentTrackMetadata = null;
        OnPropertyChanged(nameof(CurrentTrackMetadata));
        UpdatePlaybackState(DimmerPlaybackState.Error);
        CurrentPosition = 0;
        Duration = 0;
    }

    #endregion

    #region SMTC Command Handlers

    private async void CommandManager_PlayReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPlayReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Play Received");
        // Use deferral for async operations within event handlers
        var deferral = args.GetDeferral();
        try
        {
            if (_mediaPlayer.Source != null)
            {
                await PlayAsync();
                args.Handled = true;
            }
            else
            {
                args.Handled = false; // Cannot handle if no source
            }
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void CommandManager_PauseReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPauseReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Pause Received");
        var deferral = args.GetDeferral();
        try
        {
            if (_mediaPlayer.PlaybackSession.CanPause)
            {
                await PauseAsync();
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
        // Raise event for ViewModel/PlaylistManager to handle
        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType= DimmerPlaybackState.PlayNextUser };
        MediaKeyNextPressed?.Invoke(this, eventArgs);
        args.Handled = true; // Assume it will be handled
    }

    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Previous Received");
        // Raise event for ViewModel/PlaylistManager to handle
        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayPreviousUser };
        MediaKeyPreviousPressed?.Invoke(this, eventArgs);
        args.Handled = true; // Assume it will be handled
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
            // Find all active audio rendering devices
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

            // Setting AudioDevice to null resets to the system default
            _mediaPlayer.AudioDevice = deviceInfo;
            _currentAudioDeviceId = deviceInfo?.Id; // Store the currently set device ID
            Debug.WriteLine($"[AudioService] Audio output device set to: {deviceInfo?.Name ?? "System Default"} (ID: {_currentAudioDeviceId})");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Error setting audio output device (ID: {deviceId}): {ex}");
            OnErrorOccurred($"Failed to set audio output device to {deviceId}.", ex);
        }
    }

    // Handle changes to the system default device
    private async void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] System default audio render device changed. Role: {args.Role}, New ID: {args.Id}");

        // If we were explicitly using the system default (deviceId was null),
        // the MediaPlayer should switch automatically.
        // If we had selected a specific device *other* than the old default, we might want to stick with it.
        // If the *currently selected* device became unavailable, we might need to reset to default.

        // Optional: Re-query devices or check if our current device is still valid/default
        // For simplicity, we assume MediaPlayer handles the default change gracefully if AudioDevice is null.
        // If a specific device was set, we keep it unless it becomes invalid.
        if (!string.IsNullOrEmpty(_currentAudioDeviceId) && _currentAudioDeviceId != args.Id)
        {
            // Check if our explicitly selected device is still available
            try
            {
                var currentDevice = await DeviceInformation.CreateFromIdAsync(_currentAudioDeviceId);
                // If it exists, do nothing - keep the explicit selection.
                Debug.WriteLine($"[AudioService] Still using explicitly selected device: {currentDevice.Name}");
            }
            catch
            {
                // Our selected device seems invalid/unavailable now, reset to default.
                Debug.WriteLine($"[AudioService] Previously selected device ID {_currentAudioDeviceId} is no longer valid. Resetting to default.");
                await SetAudioOutputDeviceAsync(null); // Reset to system default
            }
        }
        else if (string.IsNullOrEmpty(_currentAudioDeviceId))
        {
            // We were using the default, no action needed, MediaPlayer should adapt.
            Debug.WriteLine("[AudioService] Using system default, MediaPlayer should adapt.");
        }
    }


    #endregion

    #region State Management & Helpers

    private void UpdatePlaybackState(DimmerPlaybackState newState)
    {
        // Update property only if state actually changed
        if (SetProperty(ref _playbackState, newState, nameof(CurrentPlaybackState)))
        {
            // Update IsPlaying derived property
            OnPropertyChanged(nameof(IsPlaying));

            // Raise the specific PlaybackStateChanged event

            var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  newState };
            PlaybackStateChanged?.Invoke(this, args);

            // Raise the general IsPlayingChanged event (from interface)
            RaiseIsPlayingChanged();

        }
    }

    // Converts the Windows enum to our simpler enum
    private static DimmerPlaybackState ConvertPlaybackState(MediaPlaybackState state)
    {
        switch (state)
        {
            case MediaPlaybackState.None:
                return DimmerPlaybackState.Stopped; // Or Failed if context suggests
                
            case MediaPlaybackState.Opening:
                return DimmerPlaybackState.Opening; // Or Failed if context suggests
            case MediaPlaybackState.Buffering:
                return DimmerPlaybackState.Buffering; // Or Failed if context suggests
            case MediaPlaybackState.Playing:
                return DimmerPlaybackState.Playing; // Or Failed if context suggests
            case MediaPlaybackState.Paused:
                return DimmerPlaybackState.PausedUI; // Or Failed if context suggests
            default:
                return DimmerPlaybackState.Stopped; // Or Failed if context suggests
        }
     
    }

    private void RaiseIsPlayingChanged()
    {
        // Use current state to construct the event args        
        DimmerPlaybackState eventType = IsPlaying ? DimmerPlaybackState.Playing : DimmerPlaybackState.Stopped;
      
        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  eventType };
        _isPlayingChanged?.Invoke(this, args);
    }

    private void OnErrorOccurred(string message, Exception? exception = null, MediaPlayerError? playerError = null)
    {
        // Log the error details
        Debug.WriteLine($"[AudioService ERROR] {message} | Exception: {exception?.Message} | PlayerError: {playerError}");

        var args = new PlaybackEventArgs(_currentTrackMetadata) { IsPlaying= IsPlaying, EventType=  DimmerPlaybackState.Error };
        ErrorOccurred?.Invoke(this, args);
    }


    #endregion


    #region INotifyPropertyChanged

    private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;

        backingStore = value;
        // Use dispatcher queue to ensure PropertyChanged is raised on the correct thread,
        // especially important if service methods are called from background threads.
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
        _isDisposed = true; // Mark as disposed early

        Debug.WriteLine("[AudioService] Starting asynchronous disposal...");

        // Unsubscribe from system events
        MediaDevice.DefaultAudioRenderDeviceChanged -= MediaDevice_DefaultAudioRenderDeviceChanged;

        // Cancel any ongoing initialization
        if (_initializationCts is not null)
        {
            await _initializationCts.CancelAsync();
        }
        _initializationCts?.Dispose();
        _initializationCts = null;

        // Stop playback and clear source (synchronous parts first)
        _mediaPlayer?.Pause();
        _mediaPlayer?.Source = null; // Release source reference

        // Unsubscribe from player events
        UnsubscribeFromPlayerEvents();

        // Dispose the MediaPlayer (this is the main resource)
        _mediaPlayer?.Dispose();
        Debug.WriteLine("[AudioService] MediaPlayer disposed.");

        // Clear event handlers to release subscribers
        _isPlayingChanged = null;
        _playEnded = null;
        PlaybackStateChanged = null;
        ErrorOccurred = null;
        DurationChanged = null;
        PositionChanged = null;
        SeekCompleted = null;
        MediaKeyNextPressed = null;
        MediaKeyPreviousPressed = null;
        PropertyChanged = null; // Clear this last

        Debug.WriteLine("[AudioService] Asynchronous disposal complete.");


        await Task.CompletedTask; // Return completed task as cleanup is mostly synchronous
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
        // Convert IOutputStream to System.IO.Stream
        using (Stream outputStream = randomAccessStream.GetOutputStreamAt(0).AsStreamForWrite())
        {
            // Set a reasonable buffer size.
            const int bufferSize = 81920;
            byte[] buffer = new byte[bufferSize];
            long totalBytesCopied = 0;
            int bytesRead;

            // Read from fileStream and write to outputStream manually to report progress.
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await outputStream.WriteAsync(buffer, 0, bytesRead, token);
                totalBytesCopied += bytesRead;
                progressHandler?.Report(totalBytesCopied);
            }

            // Ensure outputStream finishes writing.
            await outputStream.FlushAsync(token);
            token.ThrowIfCancellationRequested(); // Check cancellation after copy.
        }
    }

    public void InitializePlaylist(IEnumerable<SongModelView> songModels)
    {
        throw new NotImplementedException();
    }

}

