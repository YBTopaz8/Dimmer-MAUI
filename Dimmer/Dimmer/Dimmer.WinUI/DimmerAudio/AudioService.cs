using Dimmer.Utils;
using System.IO.Pipelines;

namespace Dimmer.WinUI.DimmerAudio;
#nullable enable // Enable nullable reference types for better compile-time safety

using Dimmer.Utilities.Enums;
using Dimmer.Utilities.Events; // Assuming PlaybackEventArgs, ErrorEventArgs are here
using Microsoft.UI.Dispatching; // For potential UI thread marshaling
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading; // For CancellationToken
using System.Threading.Tasks;
using Windows.Devices.Enumeration; // For GetAvailableAudioOutputsAsync
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Devices; // For GetAvailableAudioOutputsAsync, DefaultAudioRenderDeviceChangedEventArgs
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using MediaPlayer = Windows.Media.Playback.MediaPlayer;

// --- Define EventArgs if not already present ---
// namespace Dimmer.Utilities.Events
// {
//     public class PlaybackEventArgs : EventArgs
//     {
//         public bool IsPlaying { get; }
//         public PlaybackState State { get; } // More specific state
//         public double CurrentPosition { get; }
//         public double Duration { get; }
//
//         public PlaybackEventArgs(bool isPlaying, PlaybackState state, double currentPosition, double duration)
//         {
//             IsPlaying = isPlaying;
//             State = state;
//             CurrentPosition = currentPosition;
//             Duration = duration;
//         }
//     }
//
//     // Define PlaybackState enum if needed, mirroring DimmerPlaybackState but potentially simpler
//     public enum PlaybackState { Stopped, Paused, Playing, Buffering, Opening, Failed, Ended }
//
//     public class ErrorEventArgs : EventArgs
//     {
//         public string Message { get; }
//         public Exception? Exception { get; }
//         public MediaPlayerError? PlayerError { get; } // Specific player error code
//
//         public ErrorEventArgs(string message, Exception? exception = null, MediaPlayerError? playerError = null)
//         {
//             Message = message;
//             Exception = exception;
//             PlayerError = playerError;
//         }
//     }
//
//     // Example for AudioOutputDevice - adapt as needed
//     public class AudioOutputDevice
//     {
//         public string Id { get; set; } = string.Empty;
//         public string Name { get; set; } = string.Empty;
//     }
// }
// ---------------------------------------------



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
    private bool _isDisposed = false;
    private string? _currentAudioDeviceId = null; // Store the ID of the explicitly selected output device

    public AudioService()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException("AudioService must be initialized on a thread with a DispatcherQueue (typically the UI thread).");

        _mediaPlayer = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Media,
            CommandManager = { IsEnabled = true } // Enable SMTC integration early
        };

        // Subscribe to events for state management and cleanup
        SubscribeToPlayerEvents();

        // Subscribe to audio device changes
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

        // SMTC Command Handlers
        _mediaPlayer.CommandManager.PlayReceived += CommandManager_PlayReceived;
        _mediaPlayer.CommandManager.PauseReceived += CommandManager_PauseReceived;
        _mediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
        _mediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;
        // Add more command handlers if needed (e.g., Shuffle, Repeat)

        // Enabling rules (can be adjusted based on playlist logic)
        _mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Auto;
        _mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Auto;
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
    public event EventHandler<DimmerPlaybackState>? PlaybackStateChanged; // More detailed state
    public event EventHandler<ErrorEventArgs>? ErrorOccurred;
    public event EventHandler<double>? DurationChanged;
    public event EventHandler<double>? PositionChanged; // Fired frequently
    public event EventHandler<double>? SeekCompleted; // Fired after seek finishes
    public event EventHandler? PlayNext; // Raised by SMTC Next command
    public event EventHandler? PlayPrevious; // Raised by SMTC Previous command
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

    public bool IsPlaying => CurrentPlaybackState == DimmerPlaybackState.Playing || CurrentPlaybackState == DimmerPlaybackState.Buffering;

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
                if (IsPlaying || CurrentPlaybackState == DimmerPlaybackState.Paused)
                {
                    RaiseIsPlayingChanged();
                }
            }
        }
    }

    private double _currentPosition;
    public double CurrentPosition
    {
        get => _currentPosition;
        private set
        {
            // Reduce noise by checking for significant change
            if (Math.Abs(_currentPosition - value) > 0.1 || value == 0 || value == Duration)
            {
                if (SetProperty(ref _currentPosition, value))
                {
                    PositionChanged?.Invoke(this, value);
                }
            }
        }
    }

    private double _volume = 1.0;
    public double Volume
    {
        get => _mediaPlayer.Volume; // Directly get from player
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
    private double _balance = 0;
    public double Balance
    {
        get => _balance;
        set => SetProperty(ref _balance, Math.Clamp(value, -1.0, 1.0)); // Store value, but no effect yet
        // TODO: Implement balance using AudioGraph if needed
    }

    public SongModelView? CurrentTrackMetadata => _currentTrackMetadata;

    #endregion

    #region Core Playback Methods (Async)

    /// <summary>
    /// Initializes the player with the specified track metadata. Stops any current playback.
    /// </summary>
    /// <param name="metadata">The metadata of the track to load.</param>
    /// <returns>Task indicating completion.</returns>
    public async Task InitializeAsync(SongModelView metadata)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(metadata);

        // Cancel previous initialization if any
        _initializationCts?.Cancel();
        _initializationCts = new CancellationTokenSource();
        var token = _initializationCts.Token;

        Debug.WriteLine($"[AudioService] Initializing track: {metadata.Title ?? "Unknown"}");
        UpdatePlaybackState(DimmerPlaybackState.Opening);
        _currentTrackMetadata = metadata;
        OnPropertyChanged(nameof(CurrentTrackMetadata));

        // Ensure player is stopped before changing source
        _mediaPlayer.Pause(); // Pause first
        _mediaPlayer.Source = null; // Clear existing source immediately

        // Reset position/duration before loading new media
        CurrentPosition = 0;
        Duration = 0;

        try
        {
            var mediaPlaybackItem = await CreateMediaPlaybackItemAsync(metadata, token);
            token.ThrowIfCancellationRequested();

            if (mediaPlaybackItem != null)
            {
                _mediaPlayer.Source = mediaPlaybackItem;
                // State will transition via MediaOpened or MediaFailed events
                Debug.WriteLine($"[AudioService] Source set for: {metadata.Title ?? "Unknown"}");
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
            Debug.WriteLine($"[AudioService] Error initializing track '{metadata?.Title}': {ex}");
            OnErrorOccurred($"Failed to load track: {metadata?.Title}", ex);
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

    private static async Task<MediaPlaybackItem?> CreateMediaPlaybackItemAsync(SongModelView media, CancellationToken token=default)
    {
        MediaSource? mediaSource = null;
        string? mimeType = null;

        
            var uri = new Uri(media.FilePath);
            Debug.WriteLine($"[AudioService] Attempting to create MediaSource from URI: {uri}");
            try
            {
                StorageFile? storageFile = null;
                if (uri.IsFile)
                {
                    // Using StorageFile.GetFileFromPathAsync is generally more robust for file access permissions
                    storageFile = await StorageFile.GetFileFromPathAsync(media.FilePath).AsTask(token);
                    token.ThrowIfCancellationRequested();
                    mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                    mimeType = storageFile.ContentType; // Get MIME type from StorageFile
                    Debug.WriteLine($"[AudioService] Created MediaSource from StorageFile. ContentType: {mimeType}");
                }
                else // Handle non-file URIs (e.g., http, ms-appdata)
                {
                    // Note: Network streams might require background media capabilities in Package.appxmanifest
                    mediaSource = MediaSource.CreateFromUri(uri);
                    Debug.WriteLine($"[AudioService] Created MediaSource directly from URI: {uri}");
                    // Guess MIME type for non-file URIs if needed, though often not required
                    // mimeType = GetMimeType(media.FilePath);
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
        // ... (rest of the metadata/thumbnail logic) ...
        return mediaPlaybackItem;

    }

    private string GetMimeType(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "application/octet-stream";

        // Use a more robust method if possible (e.g., registry lookup or a library)
        // Basic extension mapping:
        return Path.GetExtension(filePath)?.ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".mp4" => "audio/mp4", // Can contain audio
            ".wav" => "audio/wav",
            ".wma" => "audio/x-ms-wma",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".opus" => "audio/opus",
            _ => "application/octet-stream", // Generic fallback
        };
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

        // State might already be Playing/Paused depending on timing, rely on PlaybackStateChanged
        // UpdatePlaybackState(ConvertPlaybackState(sender.PlaybackSession.PlaybackState));
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        Debug.WriteLine($"[AudioService] MediaEnded: {_currentTrackMetadata?.Title ?? "Unknown"}");
        // Set position exactly to duration
        CurrentPosition = Duration;
        UpdatePlaybackState(DimmerPlaybackState.Stopped); // Set specific ended state

        // Raise the specific PlayEnded event (as per interface)
        var eventArgs = new PlaybackEventArgs() { EventType=PlaybackEventType.StoppedAuto };
        _playEnded?.Invoke(this, eventArgs);

        // Optional: Automatically trigger next track logic
        // NextTrackRequested?.Invoke(this, EventArgs.Empty);
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
        PlayNext?.Invoke(this, EventArgs.Empty);
        args.Handled = true; // Assume it will be handled
    }

    private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
    {
        Debug.WriteLine("[AudioService] SMTC Previous Received");
        // Raise event for ViewModel/PlaylistManager to handle
        PlayPrevious?.Invoke(this, EventArgs.Empty);
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
            PlaybackStateChanged?.Invoke(this, newState);

            // Raise the general IsPlayingChanged event (from interface)
            RaiseIsPlayingChanged();

            // Update SMTC state
            UpdateSmtcState();
        }
    }

    private void UpdateSmtcState()
    {
        var session = _mediaPlayer.PlaybackSession;
        if (session == null)
            return;

        var updater = _mediaPlayer.SystemMediaTransportControls.DisplayUpdater;

        // This updates the state shown in the SMTC (e.g., Play/Pause button icon)
        // It should align with the PlaybackSession.PlaybackState
        // No direct call needed here as MediaPlayer usually handles this link internally.
        // However, ensure metadata is updated if needed:

        if (_currentTrackMetadata != null && updater.Type == MediaPlaybackType.Music)
        {
            // Re-apply metadata if needed, though usually done on Initialize
            // updater.MusicProperties.Title = _currentTrackMetadata.Title ?? "Unknown Title";
            // updater.MusicProperties.Artist = _currentTrackMetadata.ArtistName ?? "Unknown Artist";
            // updater.Update(); // Call if you manually change properties
        }
    }

    // Converts the Windows enum to our simpler enum
    private DimmerPlaybackState ConvertPlaybackState(MediaPlaybackState state)
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
                return DimmerPlaybackState.Paused; // Or Failed if context suggests
            default:
                return DimmerPlaybackState.Stopped; // Or Failed if context suggests
        }
     
    }

    private void RaiseIsPlayingChanged()
    {
        // Use current state to construct the event args
        //var args = new PlaybackEventArgs(){IsPlaying= IsPlaying,  CurrentPlaybackState, CurrentPosition, Duration);
        PlaybackEventType eventType = IsPlaying ? PlaybackEventType.Play : PlaybackEventType.Stopped;
      
        var args = new PlaybackEventArgs() { IsPlaying= IsPlaying, EventType=  eventType };
        _isPlayingChanged?.Invoke(this, args);
    }

    private void OnErrorOccurred(string message, Exception? exception = null, MediaPlayerError? playerError = null)
    {
        // Log the error details
        Debug.WriteLine($"[AudioService ERROR] {message} | Exception: {exception?.Message} | PlayerError: {playerError}");
        ErrorOccurred?.Invoke(this, new ErrorEventArgs( exception));

        // Optional: Show user alert via MAUI main thread
        // _dispatcherQueue.TryEnqueue(() =>
        // {
        //     MauiApplication.Current?.MainPage?.DisplayAlert("Audio Error", message, "OK");
        // });
    }


    #endregion

    #region Legacy/Compatibility Methods

    // Implement older synchronous methods if strictly needed for compatibility,
    // but encourage use of Async versions.

    public void Initialize(SongModelView? media, byte[]? ImageBytes)
    {
        // Note: ImageBytes parameter is redundant if it's part of SongModelView
        if (media == null)
        {
            // Handle null media case, perhaps by stopping?
            _ = StopAsync();
            return;
        }
        // Update ImageBytes in the model if provided separately
        if (media.ImageBytes == null && ImageBytes != null)
        {
            media.ImageBytes = ImageBytes;
        }

        // Call the async version and wait (not recommended on UI thread)
        InitializeAsync(media).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public void Play(bool s) // Parameter 's' seems unused/unclear
    {
        _ = PlayAsync(); // Just call the async version
    }

    public void Pause()
    {
        _ = PauseAsync();
    }

    public void Resume(double positionInSeconds)
    {
        // Combine Seek and Play
        Task.Run(async () =>
        {
            await SeekAsync(positionInSeconds);
            await PlayAsync();
        }).ConfigureAwait(false); // Run async off thread if called from UI thread
    }

    public void SetCurrentTime(double positionInSec)
    {
        _ = SeekAsync(positionInSec);
    }

    // --- Methods from interface requiring AudioGraph ---
    public void ApplyEqualizerSettings(float[] bands)
    {
        Debug.WriteLine("[AudioService] ApplyEqualizerSettings - Not Implemented (Requires AudioGraph).");
        // throw new NotImplementedException("Equalizer requires AudioGraph API.");
    }

    public void ApplyEqualizerPreset(EqualizerPresetName presetName)
    {
        Debug.WriteLine("[AudioService] ApplyEqualizerPreset - Not Implemented (Requires AudioGraph).");
        // throw new NotImplementedException("Equalizer presets require AudioGraph API.");
    }

    public Task PreloadNextTrackAsync(SongModelView? nextTrackMetadata)
    {
        // MediaPlayer doesn't directly support preloading arbitrary tracks.
        // Using MediaPlaybackList is the standard way to achieve pre-buffering/gapless.
        Debug.WriteLine("[AudioService] PreloadNextTrackAsync - Not Directly Supported (Consider MediaPlaybackList).");
        return Task.CompletedTask;
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

    // Overload for cases where dispatcher is not strictly needed (e.g., internal updates)
    // or when the property name is different from the caller member name.
    private bool SetProperty<T>(ref T backingStore, T value, string propertyName, bool useDispatcher = true)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        if (useDispatcher)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }
        else
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
        _initializationCts?.Cancel();
        _initializationCts?.Dispose();
        _initializationCts = null;

        // Stop playback and clear source (synchronous parts first)
        _mediaPlayer?.Pause();
        if (_mediaPlayer != null)
            _mediaPlayer.Source = null; // Release source reference

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
        PlayNext = null;
        PlayPrevious = null;
        PropertyChanged = null; // Clear this last

        Debug.WriteLine("[AudioService] Asynchronous disposal complete.");

        // Suppress finalization
        GC.SuppressFinalize(this);

        await Task.CompletedTask; // Return completed task as cleanup is mostly synchronous
    }

    // Optional Finalizer (safeguard, but rely on DisposeAsync)
    // ~AudioService()
    // {
    //     Debug.WriteLine("[AudioService] Finalizer called - DisposeAsync was likely missed!");
    //     // Don't call async methods here. Perform minimal sync cleanup if absolutely needed.
    //     if (!_isDisposed)
    //     {
    //         // Unsubscribe sync events? Difficult to do reliably here.
    //         // Best effort: Try to dispose the player if not already done.
    //         _mediaPlayer?.Dispose();
    //     }
    // }

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
}

