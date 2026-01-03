using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Dimmer.Data.ModelView;
using Dimmer.Utilities;
using Dimmer.WinUI.ViewModel;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// Now Playing page that displays the current song with cover art, metadata, and synced lyrics.
/// </summary>
public sealed partial class NowPlayingPage : Page
{
    private BaseViewModelWin? _viewModel;
    private readonly CompositeDisposable _disposables = new();
    private SongModelView? _currentSong;
    private IReadOnlyList<LyricPhraseModelView>? _allLyrics;
    private int _currentLyricIndex = -1;

    public NowPlayingPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        _viewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        
        if (_viewModel != null)
        {
            _currentSong = _viewModel.CurrentPlayingSongView;
            LoadCurrentSongData();
            SetupLyricsObservables();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _disposables.Clear();
    }

    private async void LoadCurrentSongData()
    {
        if (_currentSong == null)
            return;

        // Load song metadata
        TitleTextBlock.Text = _currentSong.Title;
        ArtistTextBlock.Text = _currentSong.OtherArtistsName ?? _currentSong.ArtistName;
        GenreTextBlock.Text = _currentSong.GenreName ?? "Unknown Genre";
        YearTextBlock.Text = _currentSong.ReleaseYear?.ToString() ?? "Unknown Year";

        // Load cover image
        await LoadCoverImageAsync(_currentSong.CoverImagePath);

        // Load and apply blur background
        await LoadBlurredBackgroundAsync(_currentSong.CoverImagePath);

        // Load lyrics
        await LoadLyricsAsync();
    }

    private async Task LoadCoverImageAsync(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            // Use default image
            CoverImage.Source = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage(new Uri(imagePath));
            CoverImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading cover image: {ex.Message}");
        }
    }

    private async Task LoadBlurredBackgroundAsync(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return;

        try
        {
            // Apply blur filter to the image
            var blurredBytes = await ImageFilterUtils.ApplyFilter(imagePath, FilterType.Blur);
            if (blurredBytes != null && blurredBytes.Length > 0)
            {
                using var stream = new MemoryStream(blurredBytes);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                BackgroundImageBrush.ImageSource = bitmap;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading blurred background: {ex.Message}");
        }
    }

    private async Task LoadLyricsAsync()
    {
        if (_viewModel == null || _currentSong == null)
            return;

        try
        {
            // Get lyrics from LyricsMgtFlow
            var lyrics = await _viewModel._lyricsMgtFlow.GetLyrics(_currentSong);
            
            if (lyrics == null || !lyrics.Any())
            {
                // No lyrics available
                ShowNoLyricsPlaceholder();
                return;
            }

            _allLyrics = lyrics.ToList();
            DisplayLyrics(_allLyrics);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading lyrics: {ex.Message}");
            ShowNoLyricsPlaceholder();
        }
    }

    private void DisplayLyrics(IReadOnlyList<LyricPhraseModelView> lyrics)
    {
        LyricsContainer.Children.Clear();
        NoLyricsPanel.Visibility = Visibility.Collapsed;
        LyricsScrollViewer.Visibility = Visibility.Visible;

        if (lyrics == null || !lyrics.Any())
        {
            ShowNoLyricsPlaceholder();
            return;
        }

        for (int i = 0; i < lyrics.Count; i++)
        {
            var lyric = lyrics[i];
            var lyricIndex = i;

            var textBlock = new TextBlock
            {
                Text = lyric.Text,
                FontSize = 20,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(204, 128, 128, 128)), // Grey with 0.8 opacity
                Padding = new Thickness(0, 8, 0, 8),
                Tag = lyricIndex
            };

            // Make lyrics clickable
            textBlock.PointerPressed += LyricLine_PointerPressed;
            textBlock.PointerEntered += LyricLine_PointerEntered;
            textBlock.PointerExited += LyricLine_PointerExited;

            LyricsContainer.Children.Add(textBlock);
        }
    }

    private void ShowNoLyricsPlaceholder()
    {
        LyricsScrollViewer.Visibility = Visibility.Collapsed;
        NoLyricsPanel.Visibility = Visibility.Visible;
    }

    private void SetupLyricsObservables()
    {
        if (_viewModel == null)
            return;

        // Subscribe to current lyric index changes
        _viewModel._lyricsMgtFlow.CurrentLyricIndex
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(index =>
            {
                _currentLyricIndex = index;
                UpdateLyricsHighlight(index);
            })
            .DisposeWith(_disposables);

        // Subscribe to song changes
        _viewModel.CurrentSongChanged
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(async song =>
            {
                _currentSong = song;
                LoadCurrentSongData();
            })
            .DisposeWith(_disposables);
    }

    private void UpdateLyricsHighlight(int currentIndex)
    {
        if (LyricsContainer.Children.Count == 0)
            return;

        for (int i = 0; i < LyricsContainer.Children.Count; i++)
        {
            if (LyricsContainer.Children[i] is TextBlock textBlock)
            {
                if (i < currentIndex)
                {
                    // Past lyrics - white
                    textBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                    textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                }
                else if (i == currentIndex)
                {
                    // Current lyric - white and bold
                    textBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
                    textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                    
                    // Scroll to current lyric
                    ScrollToLyric(textBlock);
                }
                else
                {
                    // Upcoming lyrics - grey with 0.8 opacity
                    textBlock.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(204, 128, 128, 128));
                    textBlock.FontWeight = Microsoft.UI.Text.FontWeights.Normal;
                }
            }
        }
    }

    private void ScrollToLyric(FrameworkElement element)
    {
        try
        {
            element.StartBringIntoView(new BringIntoViewOptions
            {
                AnimationDesired = true,
                VerticalAlignmentRatio = 0.3 // Position at 30% from top
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scrolling to lyric: {ex.Message}");
        }
    }

    private void LyricLine_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.Tag is int lyricIndex)
        {
            SeekToLyric(lyricIndex);
        }
    }

    private void LyricLine_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            textBlock.Opacity = 0.7;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }

    private void LyricLine_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            textBlock.Opacity = 1.0;
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }

    private void SeekToLyric(int lyricIndex)
    {
        if (_viewModel == null || _allLyrics == null || lyricIndex < 0 || lyricIndex >= _allLyrics.Count)
            return;

        var lyric = _allLyrics[lyricIndex];
        var positionInSeconds = lyric.TimeStampMs / 1000.0;

        _viewModel.SeekTrackPositionCommand?.Execute(positionInSeconds);
    }

    private void QueueChip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        // Navigate to queue or show queue dialog
        // This can be implemented based on existing queue functionality
    }

    private void SearchLyricsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || _currentSong == null)
            return;

        // Navigate to lyrics editor page
        _viewModel.SelectedSong = _currentSong;
        
        var navParams = new SongDetailNavArgs
        {
            Song = _currentSong,
            ViewModel = _viewModel
        };

        Frame.Navigate(typeof(SingleSongLyrics), navParams);
    }
}
