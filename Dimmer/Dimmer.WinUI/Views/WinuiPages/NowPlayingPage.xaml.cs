using Dimmer.Data.ModelView;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Dimmer.WinUI.Views.WinuiPages;

public sealed partial class NowPlayingPage : Page
{
    public NowPlayingPage()
    {
        InitializeComponent();
    }

    public BaseViewModelWin MyViewModel { get; internal set; }

    private void ViewLyricsButton_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.OpenLyricsPopUpWindow(1);
    }
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {

        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>()!;
       
        MyViewModel.CurrentWinUIPage = this;
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

       private void SyncLyricsView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

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

                // Play Songs...
                var play = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Play / Queue" };

                var playInAlbum = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Play Songs In This Album" };
                playInAlbum.Click += (_, __) => TryVM(a => a.PlaySongsByArtistInCurrentAlbum(song, artistName));

                var playAll = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Play All by Artist" };
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

        AnimationHelper.TryStart(CurrentPlayingSongImg,
            new List<UIElement> { SongInfoStackPanel },
            AnimationHelper.Key_NowPlayingPage,AnimationHelper.Key_DetailToListFromAlbum,AnimationHelper.Key_ListToDetail);
        
    }


    private async void CurrentPlayingSongImg_Loading(FrameworkElement sender, object args)
    {
        if (MyViewModel.CurrentPlayingSongView is null) return;
        if (!string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.CoverImagePath))
        {
            CurrentPlayingSongImg.Source = new BitmapImage(new Uri(MyViewModel.CurrentPlayingSongView.CoverImagePath));

            var imgBytes = await ImageFilterUtils.ApplyFilter(MyViewModel.CurrentPlayingSongView.CoverImagePath, FilterType.DarkAcrylic);
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
      
    }
}
