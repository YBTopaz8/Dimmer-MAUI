using Dimmer.DimmerAudio;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text;
using Windows.UI.StartScreen;

namespace Dimmer.WinUI.Utils;

public class WindowsDeepIntegrationService : IDisposable
{
    private readonly IDimmerAudioService _audioService;
    private readonly CompositeDisposable _disposables = new();
    private readonly IntPtr _hwnd;
    private ITaskbarList3? _taskbar;

    public WindowsDeepIntegrationService(IDimmerAudioService audioService)
    {
        _audioService = audioService;
        _hwnd = PlatUtils.GetWindowHandle();

        try
        {
            _taskbar = (ITaskbarList3)new CTaskbarList();
            _taskbar.HrInit();

            // Note: Thumbnail Buttons require a window message loop hook in WinUI 3 to catch clicks.
            // For now, we will focus on the Taskbar Progress Bar & JumpList.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Taskbar init failed: {ex.Message}");
        }

        // 1. Update Taskbar Progress Bar
        _audioService.PositionObs
            .CombineLatest(_audioService.DurationObs, (pos, dur) => new { pos, dur })
            .Sample(TimeSpan.FromSeconds(1)) // Update taskbar max once per second
            .Subscribe(x =>
            {
                if (_taskbar != null && x.dur > 0)
                {
                    _taskbar.SetProgressValue(_hwnd, (ulong)x.pos, (ulong)x.dur);
                }
            }).DisposeWith(_disposables);

        // 2. Change Taskbar Progress State based on playback
        _audioService.PlaybackStateObs.Subscribe(state =>
        {
            if (_taskbar == null) return;

            switch (state)
            {
                case DimmerPlaybackState.Playing:
                    _taskbar.SetProgressState(_hwnd, TaskbarProgressBarState.Normal); // Green
                    break;
                case DimmerPlaybackState.PausedUser:
                case DimmerPlaybackState.PausedDimmer:
                    _taskbar.SetProgressState(_hwnd, TaskbarProgressBarState.Paused); // Yellow
                    break;
                case DimmerPlaybackState.Opening:
                    _taskbar.SetProgressState(_hwnd, TaskbarProgressBarState.Indeterminate); // Green Marquee
                    break;
                default:
                    _taskbar.SetProgressState(_hwnd, TaskbarProgressBarState.NoProgress); // Hidden
                    break;
            }
        }).DisposeWith(_disposables);

        // 3. Update JumpList when a new song plays
        _audioService.CurrentSongObs
            .Where(song => song != null)
            .Subscribe(song =>
            {
                _ = UpdateJumpListAsync(song!);
            }).DisposeWith(_disposables);
    }

    private async Task UpdateJumpListAsync(SongModelView song)
    {
        try
        {
            if (string.IsNullOrEmpty(song.FilePath)) return;

            var jumpList = await JumpList.LoadCurrentAsync();

            // Prevents the jump list from getting infinitely long
            var existingItems = jumpList.Items.ToList();
            existingItems.RemoveAll(i => i.Arguments == song.FilePath);

            var newItem = JumpListItem.CreateWithArguments(song.FilePath, song.Title ?? "Unknown Title");
            newItem.Description = song.ArtistName ?? "Unknown Artist";
            newItem.GroupName = "Recently Played";
            // Use your app icon as default
            newItem.Logo = new Uri("ms-appx:///Assets/StoreLogo.png");

            jumpList.Items.Clear();
            jumpList.Items.Add(newItem);

            // Keep only last 10 items
            foreach (var item in existingItems.Take(9))
            {
                jumpList.Items.Add(item);
            }

            await jumpList.SaveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"JumpList Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
        if (_taskbar != null)
        {
            _taskbar.SetProgressState(_hwnd, TaskbarProgressBarState.NoProgress);
        }
    }

    // --- COM INTERFACES FOR WINDOWS TASKBAR ---
    [ComImport]
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    private class CTaskbarList { }

    [ComImport]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);

        [PreserveSig]
        void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        [PreserveSig]
        void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);

        [PreserveSig]
        void SetProgressState(IntPtr hwnd, TaskbarProgressBarState tbpFlags);
    }

    private enum TaskbarProgressBarState
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }
}
