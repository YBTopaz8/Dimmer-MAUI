using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using static Dimmer.DimmerSearch.TQlStaticMethods;
using Border = Microsoft.UI.Xaml.Controls.Border;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
using Visibility = Microsoft.UI.Xaml.Visibility;
using Window = Microsoft.UI.Xaml.Window;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class NativeLyricsOverlayWindow : Window
{
    public BaseViewModelWin MyViewModel { get; }

    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private readonly CompositeDisposable _disposables = new();
    private readonly DispatcherTimer _hideControlsTimer = new();

    private bool _isSliderBeingDragged = false;
    private bool _isControlsVisible = false;

    public NativeLyricsOverlayWindow(BaseViewModelWin vm)
    {
        InitializeComponent();
        MyViewModel = vm;
        RootGrid.DataContext = vm;

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        _presenter = (_appWindow.Presenter as OverlappedPresenter)!;
        _presenter.IsAlwaysOnTop = true;
        _presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        _presenter.IsResizable = true;

        this.SystemBackdrop = new DesktopAcrylicBackdrop();

        // Hide native title bar completely
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        _hideControlsTimer.Interval = TimeSpan.FromSeconds(3);
        _hideControlsTimer.Tick += (s, e) =>
        {
            if (!_isSliderBeingDragged) // Don't hide if actively seeking
            {
                FadeOutControls.Begin();
                _isControlsVisible = false;
                _hideControlsTimer.Stop();
            }
        };

        SetupBetterLyricControl();
        this.Closed += OnWindowClosed;
    }

    private void SetupBetterLyricControl()
    {
        // Custom UI Colors
        BetterLyricControl.PlayedCurrentLineFillColor = Microsoft.UI.Colors.White;
        BetterLyricControl.PlayedTextStrokeColor = Microsoft.UI.Colors.Transparent;
        BetterLyricControl.UnplayedCurrentLineFillColor = Microsoft.UI.ColorHelper.FromArgb(200, 255, 255, 255);
        BetterLyricControl.NonCurrentLineFillColor = Microsoft.UI.ColorHelper.FromArgb(120, 200, 200, 200);

        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
            .Subscribe(_ => DispatcherQueue.TryEnqueue(UpdateLyricsCanvas))
            .DisposeWith(_disposables);

        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.AllLines), v => MyViewModel.AllLines)
            .Subscribe(_ => DispatcherQueue.TryEnqueue(UpdateLyricsCanvas))
            .DisposeWith(_disposables);

        UpdateLyricsCanvas();
    }

    private void UpdateLyricsCanvas()
    {
        var song = MyViewModel?.CurrentPlayingSongView;
        if (song == null || string.IsNullOrWhiteSpace(song.SyncLyrics) || MyViewModel.AllLines == null || MyViewModel.AllLines.Count == 0)
        {
            BetterLyricControl.Visibility = Visibility.Collapsed;
            BetterLyricControl.CurrentLyricsData = null;
            return;
        }

        BetterLyricControl.Visibility = Visibility.Visible;
        try
        {
            var lyricLines = new List<DevWinUI.LyricLine>();
            var phrases = MyViewModel.AllLines.ToList();

            for (int i = 0; i < phrases.Count; i++)
            {
                var phrase = phrases[i];
                if (string.IsNullOrWhiteSpace(phrase.Text)) continue;

                int startMs = phrase.TimestampStart;
                int endMs = (i + 1 < phrases.Count) ? phrases[i + 1].TimestampStart : startMs + 5000;

                lyricLines.Add(new DevWinUI.LyricLine
                {
                    PrimaryText = phrase.Text,
                    StartMs = startMs,
                    EndMs = endMs,
                    IsPrimaryHasRealSyllableInfo = true,
                    PrimarySyllables = new List<DevWinUI.BaseLyric> { new DevWinUI.BaseLyric { Text = phrase.Text, StartMs = startMs, EndMs = endMs } }
                });
            }

            BetterLyricControl.CurrentLyricsData = new DevWinUI.LyricData(lyricLines);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BetterLyric] Failed: {ex.Message}");
        }
    }

    // ==========================================================
    // "DRAG ANYWHERE" WINDOW MAGIC (Win32 API)
    // ==========================================================
    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RootGrid);
        if (point.Properties.IsLeftButtonPressed)
        {
            // Only drag if the user isn't clicking the slider or buttons
            if (e.OriginalSource is not FrameworkElement el || (el.Name != "TrackSlider" && !(el is Button)))
            {
                ReleaseCapture();
                SendMessage(_hwnd, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }
    }

    // ==========================================================
    // DPI-AWARE CORNER SNAPPING
    // ==========================================================
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public void SnapToCorner(int cornerIndex)
    {
        double dpiScale = GetDpiForWindow(_hwnd) / 96.0;

        // Desired Logical Size
        int logicalWidth = 500;
        int logicalHeight = 600;
        int logicalMargin = 24;

        // Physical Size required by AppWindow
        int width = (int)(logicalWidth * dpiScale);
        int height = (int)(logicalHeight * dpiScale);
        int margin = (int)(logicalMargin * dpiScale);

        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        int targetX = workArea.X + margin;
        int targetY = workArea.Y + margin;

        switch (cornerIndex)
        {
            case 1: // TR
                targetX = workArea.X + workArea.Width - width - margin;
                targetY = workArea.Y + margin;
                break;
            case 2: // BL
                targetX = workArea.X + margin;
                targetY = workArea.Y + workArea.Height - height - margin;
                break;
            case 3: // BR
                targetX = workArea.X + workArea.Width - width - margin;
                targetY = workArea.Y + workArea.Height - height - margin;
                break;
        }

        _appWindow.MoveAndResize(new RectInt32(targetX, targetY, width, height));
    }

    private void DockPosition_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && int.TryParse(item.Tag?.ToString(), out int corner))
            SnapToCorner(corner);
    }

    // ==========================================================
    // AUTO-HIDE UX & SEEK DEBOUNCING
    // ==========================================================
    private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!_isControlsVisible)
        {
            FadeInControls.Begin();
            _isControlsVisible = true;
        }
        _hideControlsTimer.Start();
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e) => _hideControlsTimer.Start();

    private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSliderBeingDragged)
        {
            FadeOutControls.Begin();
            _isControlsVisible = false;
            _hideControlsTimer.Stop();
        }
    }

    private void TrackSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isSliderBeingDragged = true;
        _hideControlsTimer.Stop(); // Don't fade out while seeking!
    }

    private async void TrackSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isSliderBeingDragged)
        {
            _isSliderBeingDragged = false;
            _hideControlsTimer.Start(); // Resume auto-hide countdown

            // Convert percentage back to seconds
            double percentage = TrackSlider.Value;
            double duration = MyViewModel.CurrentPlayingSongView?.DurationInSeconds ?? 0;
            double targetSeconds = (percentage / 100.0) * duration;

            await MyViewModel.SeekTrackPositionAsync(targetSeconds);
        }
    }

    private async void BetterLyricControl_LineClicked(object sender, int lineIndex)
    {
        var lines = BetterLyricControl.CurrentLyricsData?.LyricsLines;
        if (lines == null || lineIndex < 0 || lineIndex >= lines.Count) return;

        double seekTimeSeconds = lines[lineIndex].StartMs / 1000.0;
        await MyViewModel.SeekTrackPositionAsync(seekTimeSeconds);
    }

    // ==========================================================
    // QUICK ACTIONS
    // ==========================================================
    private void PinButton_Click(object sender, RoutedEventArgs e) => _presenter.IsAlwaysOnTop = PinButton.IsChecked ?? true;
    private void PlayPause_Click(object sender, RoutedEventArgs e) => MyViewModel.PlayPauseToggleCommand.Execute(null);
    private void Prev_Click(object sender, RoutedEventArgs e) => MyViewModel.PreviousTrackCommand.Execute(false);
    private void Next_Click(object sender, RoutedEventArgs e) => MyViewModel.NextTrackCommand.Execute(false);
    private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Close();

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        this.Closed -= OnWindowClosed;
        _disposables.Dispose();
        _hideControlsTimer.Stop();
        BetterLyricControl.CurrentLyricsData = null;
    }
}