using DevWinUI;
using DynamicData.Binding;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using Point = Windows.Foundation.Point;
namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class NowPlayingPage : Page
{
    public LyricData? CurrentLyricData { get; private set; }
    public NowPlayingPage()
    {
        InitializeComponent();


        ProgressSlider.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnSliderPointerPressed), true);
        ProgressSlider.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnSliderPointerReleased), true);
        ProgressSlider.AddHandler(UIElement.PointerCaptureLostEvent, new PointerEventHandler(OnSliderPointerReleased), true);
    }

    public BaseViewModelWin MyViewModel { get; internal set; }

    private void ViewLyricsButton_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.OpenLyricsPopUpWindow(1);
    }
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {

        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>()!;
      

        MyViewModel.CurrentPageEnum = CurrentPage.NowPlayingPage;
        compDisp = new();

    }
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        compDisp.Dispose();
        base.OnNavigatingFrom(e);

    }
    private void ViewSongDetailsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (MyViewModel == null) return;

            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;

            AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail
                , CurrentPlayingSongImg);
            MyViewModel.NavigateToAnyPageOfGivenType(typeof(SongDetailPage));

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

     


    private void Button_Click(object sender, RoutedEventArgs e)
    {

    }

    private void CurrentArtistBtn_Click(object sender, RoutedEventArgs e)
    {

        var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
        
            // --- Source data & guards ---
            SongModelView song = MyViewModel?.CurrentPlayingSongView!;
            var otherArtistsRaw = song.OtherArtistsName ?? string.Empty;

            // Parse artists by multiple dividers
            var dividers = new[] { ',', ';', ':', '|' };
            var namesList = otherArtistsRaw
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (namesList.Length == 0)
            {
                // Fallback: allow acting on primary artist if you have it
                if (!string.IsNullOrWhiteSpace(song.ArtistName))
                    namesList = new[] { song.ArtistName!.Trim() };
                else
                    return; // nothing to show
            }

            // Build flyout
            var flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();

            // ===== Top info block (non-interactive) =====
            var artistLine = namesList.Length == 1 ? namesList[0] : $"{namesList.Length} artists";

            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = $"👤 {artistLine}",
                IsEnabled = false
            });

            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

            // ===== Build per-artist submenus =====
            foreach (var artistName in namesList)
            {
                var artistRoot = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = $"Artist: {artistName}" };

                // Quick View (internal)
                var quickView = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quick View" };
                quickView.Click += (_, __) => TryVM(a => a.QuickViewArtist(song, artistName));

                // View By...
                var viewBy = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "View By..." };
                var viewAlbums = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Albums" };
                viewAlbums.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(song, artistName));
                var viewGenres = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Genres" };
                viewGenres.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(song, artistName)); // customize

                viewBy.Items.Add(viewAlbums);
                viewBy.Items.Add(viewGenres);

                // PlayAsync Songs...
                var play = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "PlayAsync / Queue" };

                var playInAlbum = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "PlayAsync Songs In This Album" };
                playInAlbum.Click += (_, __) => TryVM(a => a.PlaySongsByArtistInCurrentAlbum(song, artistName));

                var playAll = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "PlayAsync All by Artist" };
                playAll.Click += (_, __) => TryVM(a => a.PlayAllSongsByArtist(song, artistName));

                var queueAll = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Queue All by Artist" };
                queueAll.Click += (_, __) => TryVM(a => a.QueueAllSongsByArtist(song, artistName));

                play.Items.Add(playInAlbum);
                play.Items.Add(playAll);
                play.Items.Add(queueAll);

                // Stats (non-interactive)
                var stats = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Stats" };
                var playCount = SafeVM(a => a.GetArtistPlayCount(song, artistName), 0);
                var isFollowed = SafeVM(a => a.IsArtistFollowed(song, artistName), false);

                stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Total plays: {playCount}", IsEnabled = false });
                stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Followed: {(isFollowed ? "Yes" : "No")}", IsEnabled = false });

                // Favorite toggle (if supported)
                var favSupported = HasVM(out IArtistActions? actions);
                bool isFav = favSupported && actions!.IsArtistFavorite(song, artistName);
                var favToggle = new ToggleMenuFlyoutItem { Text = "Favorite", IsChecked = isFav };
                favToggle.Click += (_, __) =>
                {
                    if (HasVM(out var a))
                        a!.ToggleFavoriteArtist(song, artistName, favToggle.IsChecked);
                };

                // Find On...
                var findOn = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Find On..." };
                findOn.Items.Add(MakeExternalLink("Spotify", $"https://open.spotify.com/search/{Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("YouTube Music", $"https://music.youtube.com/search?q={Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("Youtube", $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("Bandcamp", $"https://bandcamp.com/search?q={Uri.EscapeDataString(artistName)}&item_type=b"));
                findOn.Items.Add(MakeExternalLink("SoundCloud", $"https://soundcloud.com/search?q={Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("MusicBrainz", $"https://musicbrainz.org/search?query={Uri.EscapeDataString(artistName)}&type=artist&advanced=0"));
                findOn.Items.Add(MakeExternalLink("Discogs", $"https://www.discogs.com/search/?q={Uri.EscapeDataString(artistName)}&type=artist"));

                // Utilities
                var utils = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Utilities" };
                var copyName = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Copy Artist Name" };
                copyName.Click += (_, __) =>
                {
                    var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dp.SetText(artistName);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

                };

                utils.Items.Add(copyName);

                // Assemble artist root
                artistRoot.Items.Add(quickView);
                artistRoot.Items.Add(viewBy);
                artistRoot.Items.Add(play);
                artistRoot.Items.Add(stats);
                artistRoot.Items.Add(favToggle);
                artistRoot.Items.Add(findOn);
                artistRoot.Items.Add(utils);

                flyout.Items.Add(artistRoot);
            }

            var openArtistPage = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = "Open Artist Page…"
            };
            openArtistPage.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(song, namesList[0]));
            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());
            flyout.Items.Add(openArtistPage);

            // Show at pointer
            try
            {
                // Overload requires FrameworkElement + Point
                flyout.ShowAt(nativeElement,new FlyoutShowOptions() {Placement = FlyoutPlacementMode.Right});
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuFlyout.ShowAt failed: {ex.Message}");
                // fallback: anchor without position
                //flyout.ShowAt(nativeElement);
            }

            // --- local helpers ---

            bool HasVM(out IArtistActions? a)
            {
                a = MyViewModel as IArtistActions;
                return a != null;
            }
        
            void TryVM(Action<IArtistActions> action)
            {
                if (MyViewModel is IArtistActions a) action(a);
                else Debug.WriteLine("IArtistActions not implemented on MyViewModel. No-op.");
            }

            T SafeVM<T>(Func<IArtistActions, T> getter, T fallback)
            {
                try
                {
                    if (MyViewModel is IArtistActions a) return getter(a);
                    return fallback;
                }
                catch { return fallback; }
            }

            static Microsoft.UI.Xaml.Controls.MenuFlyoutItem MakeExternalLink(string label, string url)
            {
                var item = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = label };
                item.Click += async (_, __) =>
                {
                    try { await Windows.System.Launcher.LaunchUriAsync(new Uri(url)); }
                    catch (Exception ex) { Debug.WriteLine($"Open link failed: {ex.Message}"); }
                };
                return item;
            }

        
    }

    private void MainView_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var prop = e.GetCurrentPoint((UIElement)sender).Properties;
        if (prop.IsXButton2Pressed)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
            }

        }
        else
        {
            if (Frame.CanGoForward)
            {
                Frame.GoForward();
            }
        }

    }

    private void ViewAllSongs_Click(object sender, RoutedEventArgs e)
    {

        AnimationHelper.Prepare(AnimationHelper.Key_ListToDetail
            , CurrentPlayingSongImg);
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
    }

    private void ArtistBtn_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AlbumPage));
    }
    private async void CurrentPlayingSongImg_Loaded(object sender, RoutedEventArgs e)
    {

        

    }


    private async void CurrentPlayingSongImg_Loading(FrameworkElement sender, object args)
    {
        if (MyViewModel.CurrentPlayingSongView is null) return;
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(async song =>
            {
                if (!string.IsNullOrEmpty(song.CoverImagePath))
                {
                    CurrentPlayingSongImg.Source = new BitmapImage(new Uri(song.CoverImagePath));

                    var imgBytes = await ImageFilterUtils.ApplyFilter(song.CoverImagePath, FilterType.DarkAcrylic);
                    if (imgBytes is null) return;

                    CurrentPlayingSongImgBG.Source = null;

                    using var stream = new MemoryStream(imgBytes);
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CurrentPlayingSongImgBG.Source = bitmap;

                    });

                }
                else
                {

                }


            }).DisposeWith(compDisp);
        
    }
    CompositeDisposable compDisp;



    private bool _isDragging = false;
    private double _dragStartValue;

    private void OnSliderPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        MyViewModel.IsSliderBeingDragged = true;
    }

    private void OnSliderPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        var point = e.GetCurrentPoint(ProgressSlider);
        var newValue = CalculateValueFromPoint(point.Position);
        ProgressSlider.Value = newValue;

        // Live preview
        //MyViewModel.PreviewTrackPosition(newValue);

        e.Handled = true;
    }

    private async void OnSliderPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (MyViewModel != null)
        {
            // Send the actual seek command to the audio engine
            await MyViewModel.SeekTrackPositionAsync(ProgressSlider.Value);

            // Let the Rx stream resume updating the UI
            MyViewModel.IsSliderBeingDragged = false;
        }
    }

    private double CalculateValueFromPoint(Point point)
    {
        var range = ProgressSlider.Maximum - ProgressSlider.Minimum;
        var percent = point.X / ProgressSlider.ActualWidth;
        return ProgressSlider.Minimum + (percent * range);
    }

    private void OnPreviewTick(object? sender, object e)
    {
        // Update preview label during drag
        //PreviewTimeText.Text = TimeSpan.FromSeconds(ProgressSlider.Value).ToString(@"mm\:ss");
    }


    private void Goeyy_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void NowPlayingSpecViz_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private async void ProgressSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;

        // Send the actual seek command to the audio engine
        await MyViewModel.SeekTrackPositionAsync(ProgressSlider.Value);

        // Let the Rx stream resume updating the UI
        MyViewModel.IsSliderBeingDragged = false;
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        var sentGrid = (Grid)sender;
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.IsEqEnabled), v => MyViewModel.IsEqEnabled)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isEnabled =>
            {
                sentGrid.IsTapEnabled = isEnabled;
            }).DisposeWith(compDisp);

    }

    private void ComboBox_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.GetCurrentAudioDevice();
    }

    private async void BetterLyricControl_LineClicked(object sender, int e)
    {
        var lineIndex = e;
        var lines = BetterLyricControl.CurrentLyricsData?.LyricsLines;
        if (lines == null || lineIndex < 0 || lineIndex >= lines.Count)
            return;

        var targetLine = lines[lineIndex];

        // Convert Milliseconds to Seconds for your AudioService
        double seekTimeSeconds = targetLine.StartMs / 1000.0;

        await MyViewModel.SeekTrackPositionAsync(seekTimeSeconds);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        BetterLyricControl.IsLyricsVisible = true;

        // 1. 🛡️ CRASH SHIELD: Intercept the mouse wheel so it NEVER hits BetterLyric's broken code!
        BetterLyricControl.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler((s, args) =>
        {
            args.Handled = true; // Prevents the NullReferenceException!
        }), true);

        // 2. 🎨 SET EXPLICIT HIGH-CONTRAST COLORS IN C# (Guaranteed to work!)
        BetterLyricControl.PlayedCurrentLineFillColor = Microsoft.UI.Colors.White;
        BetterLyricControl.PlayedTextStrokeColor = Microsoft.UI.Colors.DarkSlateBlue;
        BetterLyricControl.UnplayedCurrentLineFillColor = Microsoft.UI.ColorHelper.FromArgb(180, 200, 200, 200); // 70% White
        BetterLyricControl.NonCurrentLineFillColor = Microsoft.UI.ColorHelper.FromArgb(100, 150, 150, 150);     // Dim Gray

       
        if (MyViewModel != null)
        {
            // Subscribe to song changes to reload the canvas
            MyViewModel.WhenPropertyChanged(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
                .Subscribe(_ => UpdateLyricsCanvas()).DisposeWith(compDisp);

            // Also reload if lyrics were just downloaded/edited
            MyViewModel.WhenPropertyChanged(nameof(MyViewModel.AllLines), v => MyViewModel.AllLines)
                .Subscribe(_ => UpdateLyricsCanvas()).DisposeWith(compDisp);
        }
    }
    private void UpdateLyricsCanvas()
    {
        var song = MyViewModel?.CurrentPlayingSongView;
        if (song == null || string.IsNullOrWhiteSpace(song.SyncLyrics))
        {
            BetterLyricControl.CurrentLyricsData = null;
            BetterLyricControl.IsLyricsVisible = false;
            return;
        }

        try
        {
            var lyricLines = new List<DevWinUI.LyricLine>();

            // We use Dimmer's already parsed AllLines collection
            if (MyViewModel.AllLines != null && MyViewModel.AllLines.Count > 0)
            {
                var phrases = MyViewModel.AllLines.ToList();

                for (int i = 0; i < phrases.Count; i++)
                {
                    var phrase = phrases[i];
                    if (string.IsNullOrWhiteSpace(phrase.Text)) continue;

                    int startMs = phrase.TimestampStart;
                    int endMs = (i + 1 < phrases.Count)
                        ? phrases[i + 1].TimestampStart
                        : (phrase.TimeStampMs > startMs ? phrase.TimeStampMs : startMs + 4000);

                    // Construct exactly like the developer's sample
                    lyricLines.Add(new DevWinUI.LyricLine
                    {
                        PrimaryText = phrase.Text,
                        StartMs = startMs,
                        EndMs = endMs,

                        // 🚨 This tells the engine it has Word-By-Word capability
                        IsPrimaryHasRealSyllableInfo = true,

                        // 🚨 You MUST provide at least one syllable (the whole line) for it to render
                        PrimarySyllables = new List<DevWinUI.BaseLyric>
                    {
                        new DevWinUI.BaseLyric
                        {
                            Text = phrase.Text,
                            StartMs = startMs,
                            EndMs = endMs
                        }
                    }
                    });
                }
            }

            // Apply it
            var lyricData = new DevWinUI.LyricData(lyricLines);
            BetterLyricControl.CurrentLyricsData = lyricData;
            BetterLyricControl.IsLyricsVisible = lyricData.LyricsLines.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BetterLyric] Failed to load lyrics: {ex.Message}");
            BetterLyricControl.IsLyricsVisible = false;
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        BetterLyricControl.IsLyricsVisible = false;
        BetterLyricControl.CurrentLyricsData = null;
    }
}
