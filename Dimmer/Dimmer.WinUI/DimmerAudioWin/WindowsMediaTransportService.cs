using Dimmer.DimmerAudio;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace Dimmer.WinUI.DimmerAudioWin;

public partial class WindowsMediaTransportService : IDisposable
{
    private readonly IDimmerAudioService _audioService;
    private readonly MediaPlayer _dummyMediaPlayer;
    private readonly SystemMediaTransportControls _smtc;
    private readonly CompositeDisposable _disposables = new();

    private double _currentDuration = 0;

    public WindowsMediaTransportService(IDimmerAudioService audioService)
    {
        _audioService = audioService;
        // We create a MediaPlayer but NEVER give it audio. We just want its SMTC UI.
        _dummyMediaPlayer = new MediaPlayer();
        _dummyMediaPlayer.CommandManager.IsEnabled = false; // DISABLE auto-handling so we can route to OwnAudioService

        _smtc = _dummyMediaPlayer.SystemMediaTransportControls;
        _smtc.IsEnabled = true;
        _smtc.IsPlayEnabled = true;
        _smtc.IsPauseEnabled = true;
        _smtc.IsNextEnabled = true;
        _smtc.IsPreviousEnabled = true;

        // Subscribe to Lockscreen / Keyboard Media Buttons
        _smtc.ButtonPressed += Smtc_ButtonPressed;

        // 2. BIND TO YOUR REACTIVE AUDIO ENGINE
        _audioService.CurrentSongObs
            .Where(song => song != null)
            .Subscribe(song => UpdateMetadataAsync(song!).FireAndForget())
            .DisposeWith(_disposables);

        _audioService.PlaybackStateObs
            .Subscribe(state => UpdatePlaybackState(state))
            .DisposeWith(_disposables);

        _audioService.DurationObs
            .Subscribe(duration => _currentDuration = duration)
            .DisposeWith(_disposables);

        // Update the seekbar on the lockscreen/overlay
        _audioService.PositionObs
            .Sample(TimeSpan.FromSeconds(1)) // Don't spam the OS UI
            .Subscribe(pos => UpdateTimeline(pos))
            .DisposeWith(_disposables);
        new WindowsDeepIntegrationService(audioService);

    }
    public void Initialize()
    {
        // This method is intentionally left empty. It serves as a trigger to ensure the service is instantiated and initialized.
    }
    private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        // Must dispatch to background/main thread as SMTC calls this from an OS thread
        Task.Run(async () =>
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await _audioService.PlayAsync();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    await _audioService.PauseAsync();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    if (_audioService is OwnAudioService srvNext) srvNext.TriggerNext();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    if (_audioService is OwnAudioService srvPrev) srvPrev.TriggerPrevious();
                    break;
            }
        });
    }

    private async Task UpdateMetadataAsync(SongModelView song)
    {
        var updater = _smtc.DisplayUpdater;
        updater.Type = MediaPlaybackType.Music;
        updater.MusicProperties.Title = song.Title ?? "Unknown Title";
        updater.MusicProperties.Artist = song.OtherArtistsName ?? "Unknown Artist";
        updater.MusicProperties.AlbumTitle = song.AlbumName ?? "Unknown Album";

        // Load Album Art for the Windows Volume Overlay
        if (!string.IsNullOrEmpty(song.CoverImagePath) && File.Exists(song.CoverImagePath))
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(song.CoverImagePath);
                updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(file);
            }
            catch
            {
                updater.Thumbnail = null;
            }
        }
        else
        {
            updater.Thumbnail = null;
        }

        updater.Update();
    }

    private void UpdatePlaybackState(DimmerPlaybackState state)
    {
        _smtc.PlaybackStatus = state switch
        {
            DimmerPlaybackState.Playing => MediaPlaybackStatus.Playing,
            DimmerPlaybackState.PausedUser => MediaPlaybackStatus.Paused,
            DimmerPlaybackState.PausedDimmer => MediaPlaybackStatus.Paused,
            DimmerPlaybackState.Opening => MediaPlaybackStatus.Changing,
            _ => MediaPlaybackStatus.Stopped
        };
    }

    private void UpdateTimeline(double positionInSeconds)
    {
        if (_currentDuration <= 0) return;

        var timeline = new SystemMediaTransportControlsTimelineProperties
        {
            StartTime = TimeSpan.Zero,
            MinSeekTime = TimeSpan.Zero,
            Position = TimeSpan.FromSeconds(positionInSeconds),
            MaxSeekTime = TimeSpan.FromSeconds(_currentDuration),
            EndTime = TimeSpan.FromSeconds(_currentDuration)
        };

        _smtc.UpdateTimelineProperties(timeline);
    }

    public void Dispose()
    {
        _smtc.ButtonPressed -= Smtc_ButtonPressed;
        _dummyMediaPlayer.Dispose();
        _disposables.Dispose();
    }
}

// Helper extension to swallow unawaited tasks safely in events
public static class TaskExtensions
{
    public static void FireAndForget(this Task task)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted) System.Diagnostics.Debug.WriteLine($"Error: {t.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}