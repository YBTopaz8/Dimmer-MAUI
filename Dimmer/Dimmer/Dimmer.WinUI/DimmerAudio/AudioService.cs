using System.Reactive.Subjects;

using Dimmer.Interfaces.Services.Interfaces;

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
    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);

    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
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
        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted); // Initial state
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
                // Also raise PlaybackEventArgs when duration changes while playing/paused
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
        get => _currentPositionValue; // Or directly from _mediaPlayer.PlaybackSession.Position.TotalSeconds if always preferred live
        private set
        {
            if ((Math.Abs(_currentPositionValue - value) > 0.1 || Math.Abs(value) < 0.0001 || Math.Abs(value - Duration) < 0.0001))
            {
                _currPositionBS.OnNext(value);
                if (SetProperty(ref _currentPositionValue, value)) // SetProperty updates the backing field
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
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(songModel);


            // --- Critical Section for managing _initializationCts ---
            CancellationTokenSource? oldCts = null;
            CancellationTokenSource newCts = new CancellationTokenSource(); // Create new CTS for this attempt

            lock (this) // Ensure thread-safe swap of CTS
            {
                oldCts = _initializationCts;
                _initializationCts = newCts;
            }

            if (oldCts != null)
            {
                Debug.WriteLine("[AudioService] InitializeAsync: Cancelling previous initialization task.");
                await oldCts.CancelAsync(); // Signal cancellation to previous task
                oldCts.Dispose();           // Dispose the old CTS
            }
            // --- End Critical Section ---

            var token = newCts.Token; // Use token from the NEW CTS

            // Set metadata immediately, UI can show "loading..." for this track
            // But clear it if initialization ultimately fails
            _currentTrackMetadata = songModel;
            _currentSong.OnNext(songModel);
            OnPropertyChanged(nameof(CurrentTrackMetadata)); // Notify UI about the new track being loaded



            // It's crucial to pause and null out the source *before* any await that might be cancelled.
            // This prevents the player from being in an indeterminate state if cancellation occurs
            // during an async operation in CreateMediaPlaybackItemAsync.
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
            Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer paused and source nulled.");

            // Reset position/duration for the new track
            // These will be updated by MediaOpened or NaturalDurationChanged if successful
            _dispatcherQueue.TryEnqueue(() => // Ensure UI thread for property changes that might affect UI
            {
                CurrentPosition = 0;
                Duration = 0;
            });


            MediaPlaybackItem? mediaPlaybackItem = null;
            bool success = false;

            try
            {
                mediaPlaybackItem = await CreateMediaPlaybackItemAsync(songModel, null, token).ConfigureAwait(false); // Pass the token
                token.ThrowIfCancellationRequested(); // Check if *this* operation was cancelled

                if (mediaPlaybackItem != null)
                {
                    // This is the most critical point for the player
                    _mediaPlayer.Source = mediaPlaybackItem;
                    // DO NOT AWAIT HERE. MediaOpened event will signal readiness or MediaFailed will signal error.
                    // Player will go to Opening state.
                    Debug.WriteLine("[AudioService] InitializeAsync: MediaPlayer source SET for {SongTitle}. Waiting for MediaOpened/MediaFailed.", songModel.Title);
                    success = true; // Assume success for now; MediaFailed will correct this
                }
                else
                {
                    Debug.WriteLine("[AudioService] InitializeAsync: CreateMediaPlaybackItemAsync returned null for {SongTitle}. Cannot set source.", songModel.Title);
                    // No need to throw the "Failed to create" exception here if CreateMediaPlaybackItemAsync already logged and returned null.
                    // The 'success' flag will remain false.
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[AudioService] InitializeAsync: Operation CANCELED while creating/setting source for {SongTitle}.", songModel.Title);
                // 'success' remains false. If _initializationCts was newCts, this means this *current* init was cancelled.
            }
            catch (Exception ex)
            {
            }
            finally
            {
                // Critical: Only dispose the CTS if it's the one we created for *this* call
                // and it hasn't been replaced by a newer initialization attempt.
                lock (this)
                {
                    if (_initializationCts == newCts)
                    {
                        _initializationCts = null; // Clear the current CTS reference
                    }
                }
                newCts.Dispose(); // Always dispose the CTS we created for this call

                if (!success)
                {
                    Debug.WriteLine("[AudioService] InitializeAsync: Finalizing with FAILED status for {SongTitle}.", songModel.Title);
                    // If we failed to set a source, or it was cancelled before setting.
                    if (ReferenceEquals(_currentTrackMetadata, songModel)) // Only clear if it's still the one we were trying to init
                    {
                        _currentTrackMetadata = null;
                        OnPropertyChanged(nameof(CurrentTrackMetadata));
                    }
                    UpdatePlaybackState(DimmerPlaybackState.Error); // Signal an error state
                    OnErrorOccurred($"Failed to initialize track: {songModel?.Title}", null); // Raise specific error event
                }
                // If success was true, we rely on MediaOpened/MediaFailed to set the final state.
                // Player might be in Opening state.
            }
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
            _mediaPlayer.Volume=1;
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
        UpdatePlaybackState(DimmerPlaybackState.PausedDimmer); // Explicitly set stopped state

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

    private static async Task<MediaPlaybackItem?> CreateMediaPlaybackItemAsync(SongModelView media, byte[]? ImageBytes = null, CancellationToken token = default)
    {

        // 1) Guard empty or null paths
        if (string.IsNullOrWhiteSpace(media.FilePath))
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: No FilePath for '{media.Title ?? "Unknown"}', cannot create item.");
            return null;
        }

        Uri? uri = null;
        StorageFile? storageFile = null; // Keep a reference if created

        try
        {
            // 2) Attempt to treat as an absolute URI or a local file path
            if (Uri.TryCreate(media.FilePath, UriKind.Absolute, out var parsedUri) && !parsedUri.IsFile)
            {
                uri = parsedUri; // It's a non-file URI (e.g., http)
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Using direct URI: {uri} for '{media.Title}'");
            }
            else // Treat as a local file path
            {
                string fullPath = Path.GetFullPath(media.FilePath); // Resolve relative paths
                if (!File.Exists(fullPath)) // Explicit check if file exists
                {
                    Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: File does not exist at resolved path '{fullPath}' for '{media.Title}'.");
                    return null; // File truly doesn't exist
                }
                // For local files, StorageFile is preferred for robustness & metadata
                // uri = new Uri(fullPath); // URI for StorageFile.GetFileFromPathAsync
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Attempting StorageFile for path: {fullPath} for '{media.Title}'");
                storageFile = await StorageFile.GetFileFromPathAsync(fullPath).AsTask(token);
            }

            token.ThrowIfCancellationRequested(); // Check before creating MediaSource

            MediaSource? mediaSource;
            if (storageFile != null)
            {
                mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from StorageFile for '{media.Title}'. ContentType: {storageFile.ContentType}");
            }
            else if (uri != null) // Must be a non-file URI from above
            {
                mediaSource = MediaSource.CreateFromUri(uri);
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Created MediaSource from URI for '{media.Title}'.");
            }
            else
            {
                // This case should ideally not be reached if logic above is correct
                Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Could not determine how to create MediaSource for '{media.Title}'.");
                return null;
            }

            // --- Create MediaPlaybackItem and add Metadata ---
            var mediaPlaybackItem = new MediaPlaybackItem(mediaSource);
            var props = mediaPlaybackItem.GetDisplayProperties();

            props.Type = MediaPlaybackType.Music;
            props.MusicProperties.Title = media.Title ?? Path.GetFileNameWithoutExtension(media.FilePath) ?? "Unknown Title";
            props.MusicProperties.Artist = media.ArtistName ?? "Unknown Artist";
            props.MusicProperties.AlbumTitle = media.AlbumName ?? string.Empty;
            // props.MusicProperties.TrackNumber = (uint)(media.TrackNumber ?? 0); // If you have track number

            // Thumbnail: Removed manual ImageBytes handling.
            // Windows/MediaPlayer will attempt to load it from:
            // 1. Embedded metadata in the StorageFile.
            // 2. System music library caches if the file is indexed there.
            // If 'storageFile' is used, this is more likely to work. For raw URIs, it's less certain.
            // If you used MediaSource.CreateFromStream, you'd set props.Thumbnail = RandomAccessStreamReference.CreateFromStream(...)
            // but that was for the ImageBytes. For the song's actual cover, it's usually embedded.

            mediaPlaybackItem.ApplyDisplayProperties(props);
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Successfully created MediaPlaybackItem for '{media.Title}'.");
            return mediaPlaybackItem;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Operation CANCELED for '{media.Title ?? media.FilePath}'.");
            throw; // Re-throw to be handled by InitializeAsync
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
            Debug.WriteLine($"[AudioService] CreateMediaPlaybackItemAsync: Generic error creating MediaSource for '{media.FilePath}': {ex.ToString()}"); // Log full exception
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
        UpdatePlaybackState(DimmerPlaybackState.PlayCompleted); // Set specific ended state

        // Raise the specific PlayEnded event (as per interface)
        var eventArgs = new PlaybackEventArgs(_currentTrackMetadata) { EventType=DimmerPlaybackState.PlayCompleted };
        _playEnded?.Invoke(this, eventArgs);

    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Debug.WriteLine($"[AudioService] MediaFailed: Error={args.Error}, Code={args.ExtendedErrorCode}, Msg={args.ErrorMessage}");
        OnErrorOccurred($"Playback failed: {args.ErrorMessage}", args.ExtendedErrorCode, args.Error);

        // Try to clean up
        // sender.Source = null; // This might be too aggressive if you want to retry.
        // However, for a fatal media error, clearing the source is reasonable.
        // The key is that _currentTrackMetadata is nulled and state is Error.
        _currentTrackMetadata = null; // Correct
        OnPropertyChanged(nameof(CurrentTrackMetadata)); // Correct
        UpdatePlaybackState(DimmerPlaybackState.Error); // Correct
        CurrentPosition = 0; // Correct
        Duration = 0;      // Correct
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
    private static (DimmerPlaybackState, bool) ConvertPlaybackState(MediaPlaybackState state)
    {
        switch (state)
        {
            case MediaPlaybackState.None:
                return (DimmerPlaybackState.None, false); // Or Failed if context suggests

            case MediaPlaybackState.Opening:
                return (DimmerPlaybackState.Opening, false); // Or Failed if context suggests
            case MediaPlaybackState.Buffering:
                return (DimmerPlaybackState.Buffering, false); // Or Failed if context suggests
            case MediaPlaybackState.Playing:
                return (DimmerPlaybackState.Playing, true); // Or Failed if context suggests
            case MediaPlaybackState.Paused:
                return (DimmerPlaybackState.PausedDimmer, true); // Or Failed if context suggests
            default:
                return (DimmerPlaybackState.PlayCompleted, true); // Or Failed if context suggests
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

