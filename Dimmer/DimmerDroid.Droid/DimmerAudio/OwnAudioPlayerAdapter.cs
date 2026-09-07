using AndroidX.Media3.Common.Text;
using AndroidX.Media3.ExoPlayer;

using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.DimmerAudio;

using AndroidX.Media3.Common;


public partial class OwnAudioPlayerAdapter : Java.Lang.Object, IPlayer
{
    private readonly IDimmerAudioService _audioService;
    private readonly List<IPlayerListener> _listeners = new();

    private bool _playWhenReady = false;
    private PlaybackParameters _playbackParameters = PlaybackParameters.Default!;
    private MediaItem? _currentMediaItem;
    private AudioAttributes _audioAttributes = AudioAttributes.Default!;

    // We store our custom timeline so Android knows a track exists
    private Timeline _currentTimeline = Timeline.Empty!;

    public OwnAudioPlayerAdapter(IDimmerAudioService audioService)
    {
        _audioService = audioService;

        // 1. Listen for Song Changes (THIS DRIVES THE NOTIFICATION UI)
        _audioService.CurrentSong.Subscribe(song =>
        {
            if (song != null)
            {
                UpdateAndroidMetadata(song, _audioService.Duration);
            }
        });
        _audioService.DurationChanged += (s, durationInSeconds) =>
        {
            if (_currentMediaItem != null)
            {
                UpdateAndroidMetadata(_audioService.CurrentTrackMetadata!, durationInSeconds);
            }
        };

        // 2. Bridge Playback State
        _audioService.PlaybackStateChanged += (s, e) =>
        {
            int media3State = e.EventType switch
            {
                DimmerPlaybackState.Playing => 3,       // STATE_READY
                DimmerPlaybackState.PausedUser => 3,    // STATE_READY
                DimmerPlaybackState.PausedDimmer => 3,  // STATE_READY
                DimmerPlaybackState.PlayCompleted => 4, // STATE_ENDED
                DimmerPlaybackState.Opening => 2,       // STATE_BUFFERING
                _ => 1                                  // STATE_IDLE
            };

            foreach (var listener in _listeners)
                listener.OnPlaybackStateChanged(media3State);
        };

        // 3. Bridge IsPlaying State
        _audioService.IsPlayingChanged += (s, e) =>
        {
            _playWhenReady = e.IsPlaying;
            foreach (var listener in _listeners)
            {
                listener.OnPlayWhenReadyChanged(_playWhenReady, 1);
                listener.OnIsPlayingChanged(e.IsPlaying);
            }
        };

        // 4. Bridge Volume
        _audioService.VolumeChanged += (s, vol) =>
        {
            foreach (var listener in _listeners)
                listener.OnVolumeChanged((float)vol);
        };
    }

    private void UpdateAndroidMetadata(SongModelView song, double durationInSeconds)
    {
        var metadataBuilder = new MediaMetadata.Builder()?
            .SetTitle(song.Title)?
            .SetArtist(song.ArtistName)?
            .SetAlbumTitle(song.AlbumName);

        if (!string.IsNullOrEmpty(song.CoverImagePath))
        {
            // Must use Uri.Parse("file://...") for local files so the notification can load the album art
            metadataBuilder?.SetArtworkUri(global::Android.Net.Uri.Parse($"file://{song.CoverImagePath}"));
        }

        _currentMediaItem = new MediaItem.Builder()?
            .SetMediaId(song.Id.ToString())?
            .SetMediaMetadata(metadataBuilder?.Build())?
            .Build();


        long durationMs = (long)(durationInSeconds * 1000.0);
        if (_currentMediaItem != null)
        {
            _currentTimeline = new DimmerTimeline(_currentMediaItem, durationMs);

           
            foreach (var listener in _listeners)
            {
                listener.OnTimelineChanged(_currentTimeline, 1);
                listener.OnMediaItemTransition(_currentMediaItem, 3);
                listener.OnMediaMetadataChanged(_currentMediaItem.MediaMetadata);
            }
        }
    }

    // ==========================================================
    // 1. CORE IMPLEMENTATION
    // ==========================================================

    public Looper? ApplicationLooper => Looper.MainLooper;
    public PlayerCommands? AvailableCommands => new PlayerCommands.Builder()
        .Add(1)? // COMMAND_PLAY_PAUSE
        .Add(4)? // COMMAND_SET_SPEED_AND_PITCH
        .Add(5)? // COMMAND_SEEK_IN_CURRENT_MEDIA_ITEM (Enables the Seekbar tracking)
        .Add(7)? // COMMAND_SEEK_TO_PREVIOUS
        .Add(9)? // COMMAND_SEEK_TO_NEXT
        .Build();
    public Timeline? CurrentTimeline => _currentTimeline;

    public long CurrentPosition => (long)(_audioService.CurrentPosition * 1000.0);
    public long Duration => (long)(_audioService.Duration * 1000.0);
    public long BufferedPosition => Duration;
    public long TotalBufferedDuration => Duration;
    public long ContentBufferedPosition => Duration;
    public long ContentDuration => Duration;
    public long ContentPosition => CurrentPosition;

    public bool IsPlaying => _audioService.IsPlaying;

    public int PlaybackState => _audioService.CurrentPlaybackState switch
    {
        DimmerPlaybackState.Playing => 3,
        DimmerPlaybackState.PausedUser => 3,
        DimmerPlaybackState.PausedDimmer => 3,
        DimmerPlaybackState.PlayCompleted => 4,
        DimmerPlaybackState.Opening => 2,
        _ => 1
    };

    public float Volume
    {
        get => (float)_audioService.Volume;
        set => _audioService.SetVolume(value);
    }

    public bool PlayWhenReady
    {
        get => _playWhenReady;
        set
        {
            _playWhenReady = value;
            if (value)
                Play();
            else
                Pause();
        }
    }

    public void Play()
    {
        _playWhenReady = true;
        _ = _audioService.PlayAsync(-1);
    }

    public void Pause()
    {
        _playWhenReady = false;
        _ = _audioService.PauseAsync();
    }

    public void Stop()
    {
        _playWhenReady = false;
        _audioService.Stop();
    }

    public void SeekTo(long positionMs) => _ = _audioService.SeekAsync(positionMs / 1000.0);
    public void SeekTo(int mediaItemIndex, long positionMs) => SeekTo(positionMs);
    public void SeekToDefaultPosition() => SeekTo(0);
    public void SeekToDefaultPosition(int mediaItemIndex) => SeekTo(0);
    public void SeekBack() => SeekTo(System.Math.Max(0, CurrentPosition - SeekBackIncrement));
    public void SeekForward() => SeekTo(System.Math.Min(Duration, CurrentPosition + SeekForwardIncrement));

    // ==========================================================
    // 2. METADATA & MEDIA ITEMS
    // ==========================================================

    public MediaItem? CurrentMediaItem => _currentMediaItem;
    public MediaMetadata? MediaMetadata => _currentMediaItem?.MediaMetadata ?? MediaMetadata.Empty;
    public int MediaItemCount => _currentMediaItem != null ? 1 : 0;

    public PlaybackParameters? PlaybackParameters
    {
        get => _playbackParameters;
        set
        {
            _playbackParameters = value ?? PlaybackParameters.Default!;

            if (_audioService is OwnAudioService ownAudio)
            {
                // Hook up your Nightcore / speed controls!
                ownAudio.SetPitchAndSpeed(0f, _playbackParameters.Speed);
            }

            foreach (var listener in _listeners)
                listener.OnPlaybackParametersChanged(_playbackParameters);
        }
    }

    public void SetPlaybackSpeed(float speed) => PlaybackParameters = new PlaybackParameters(speed, PlaybackParameters?.Pitch ?? 1f);

    // ==========================================================
    // 3. LISTENERS & STUBS
    // ==========================================================

    public void AddListener(IPlayerListener? listener) { if (listener != null && !_listeners.Contains(listener)) _listeners.Add(listener); }
    public void RemoveListener(IPlayerListener? listener) { if (listener != null) _listeners.Remove(listener); }
    public void Release() => _listeners.Clear();

    public AudioAttributes? AudioAttributes => _audioAttributes;
    public void SetAudioAttributes(AudioAttributes? attributes, bool handleAudioFocus) => _audioAttributes = attributes ?? AudioAttributes.Default!;

    public int BufferedPercentage => Duration > 0 ? 100 : 0;
    public int CurrentAdGroupIndex => C.IndexUnset;
    public int CurrentAdIndexInAdGroup => C.IndexUnset;
    public CueGroup? CurrentCues => CueGroup.EmptyTimeZero;
    public long CurrentLiveOffset => C.TimeUnset;
    public Java.Lang.Object? CurrentManifest => null;
    public int CurrentMediaItemIndex => 0;
    public int CurrentPeriodIndex => 0;
    public Tracks? CurrentTracks => Tracks.Empty;
    public int CurrentWindowIndex => 0;
    public AndroidX.Media3.Common.DeviceInfo? DeviceInfo => AndroidX.Media3.Common.DeviceInfo.Unknown;
    public bool DeviceMuted { get; set; } = false;
    public int DeviceVolume { get; set; } = 100;
    public bool IsCurrentMediaItemDynamic => false;
    public bool IsCurrentMediaItemLive => false;
    public bool IsCurrentMediaItemSeekable => true;
    public bool IsCurrentWindowDynamic => false;
    public bool IsCurrentWindowLive => false;
    public bool IsCurrentWindowSeekable => true;
    public bool IsLoading => false;
    public bool IsPlayingAd => false;
    public long MaxSeekToPreviousPosition => 3000;
    public int NextMediaItemIndex => C.IndexUnset;
    public int NextWindowIndex => C.IndexUnset;
    public int PreviousMediaItemIndex => C.IndexUnset;
    public int PreviousWindowIndex => C.IndexUnset;
    public int PlaybackSuppressionReason => 0;
    public PlaybackException? PlayerError => null;
    public MediaMetadata? PlaylistMetadata { get; set; } = MediaMetadata.Empty;
    public int RepeatMode { get; set; } = 0;
    public bool ShuffleModeEnabled { get; set; } = false;
    public long SeekBackIncrement => 5000;
    public long SeekForwardIncrement => 5000;
    public AndroidX.Media3.Common.Util.Size? SurfaceSize => AndroidX.Media3.Common.Util.Size.Unknown;
    public TrackSelectionParameters? TrackSelectionParameters { get; set; } = AndroidX.Media3.Common.TrackSelectionParameters.Default;
    public VideoSize? VideoSize => AndroidX.Media3.Common.VideoSize.Unknown;

    public bool IsCommandAvailable(int command) => true;
    public bool CanAdvertiseSession() => true;
    public void Prepare() { }

    // Stubs
    public void SetMediaItem(MediaItem? p0) { }
    public void SetMediaItem(MediaItem? p0, bool p1) { }
    public void SetMediaItem(MediaItem? p0, long p1) { }
    public void ClearMediaItems() { }
    public MediaItem? GetMediaItemAt(int p0) => null;
    public bool HasNextMediaItem => false;
    public bool HasPreviousMediaItem => false;
    public void AddMediaItem(MediaItem? p0) { }
    public void AddMediaItem(int p0, MediaItem? p1) { }
    public void AddMediaItems(int p0, IList<MediaItem>? p1) { }
    public void AddMediaItems(IList<MediaItem>? p0) { }
    public void MoveMediaItem(int p0, int p1) { }
    public void MoveMediaItems(int p0, int p1, int p2) { }
    public void RemoveMediaItem(int p0) { }
    public void RemoveMediaItems(int p0, int p1) { }
    public void ReplaceMediaItem(int p0, MediaItem? p1) { }
    public void ReplaceMediaItems(int p0, int p1, IList<MediaItem>? p2) { }
    public void SetMediaItems(IList<MediaItem>? p0) { }
    public void SetMediaItems(IList<MediaItem>? p0, bool p1) { }
    public void SetMediaItems(IList<MediaItem>? p0, int p1, long p2) { }
    public void SeekToNext() 
    {
        SeekToNextMediaItem();
    }
    public void SeekToNextMediaItem()
    {
        if (_audioService is OwnAudioService srv)
            srv.TriggerNext();
    }
    public void SeekToPrevious()
    {
        SeekToPreviousMediaItem();
    }

    public void SeekToPreviousMediaItem()
    {
        if (_audioService is OwnAudioService srv)
            srv.TriggerPrevious();
    }
    public void DecreaseDeviceVolume() { }
    public void DecreaseDeviceVolume(int p0) { }
    public void IncreaseDeviceVolume() { }
    public void IncreaseDeviceVolume(int p0) { }
    public void Mute() { }
    public void Unmute() { }
    public void SetDeviceMuted(bool p0, int p1) { }
    public void SetDeviceVolume(int p0, int p1) { }
    public void ClearVideoSurface() { }
    public void ClearVideoSurface(Android.Views.Surface? p0) { }
    public void ClearVideoSurfaceHolder(Android.Views.ISurfaceHolder? p0) { }
    public void ClearVideoSurfaceView(Android.Views.SurfaceView? p0) { }
    public void ClearVideoTextureView(Android.Views.TextureView? p0) { }
    public void SetVideoSurface(Android.Views.Surface? p0) { }
    public void SetVideoSurfaceHolder(Android.Views.ISurfaceHolder? p0) { }
    public void SetVideoSurfaceView(Android.Views.SurfaceView? p0) { }
    public void SetVideoTextureView(Android.Views.TextureView? p0) { }
}

// =========================================================================
// CUSTOM TIMELINE: Media3 requires this to draw the Notification UI properly
// =========================================================================
public class DimmerTimeline : Timeline
{
    private readonly MediaItem _mediaItem;
    private readonly long _durationUs;

    public DimmerTimeline(MediaItem mediaItem, long durationMs)
    {
        _mediaItem = mediaItem;
        _durationUs = durationMs > 0 ? durationMs * 1000L : C.TimeUnset;
    }

    public override int WindowCount => 1;
    public override int PeriodCount => 1;

    public override Window? GetWindow(int windowIndex, Window? window, long defaultPositionProjectionUs)
    {
        window?.Set(
            Java.Lang.Object.FromArray(Array.Empty<object>()),
            _mediaItem,
            null,
            C.TimeUnset,
            C.TimeUnset,
            C.TimeUnset,
            true,  // isSeekable
            false, // isDynamic
            null,
            0,
            _durationUs,
            0,
            0,
            0
        );
        return window;
    }

    public override Period? GetPeriod(int periodIndex, Period? period, bool setIds)
    {
        period?.Set(
            null,
            null,
            0,
            _durationUs,
            0
        );
        return period;
    }

    public override int GetIndexOfPeriod(Java.Lang.Object? uid) => 0;
    public override Java.Lang.Object? GetUidOfPeriod(int periodIndex) => new Java.Lang.String("dimmer_period");
}
