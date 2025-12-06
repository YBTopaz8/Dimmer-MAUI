using System.DirectoryServices;
using System.Globalization;

using CommunityToolkit.Maui.Core.Extensions;

using Windows.ApplicationModel.DataTransfer;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Colors = Microsoft.UI.Colors;
using DataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;
using DragEventArgs = Microsoft.UI.Xaml.DragEventArgs;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>

public sealed partial class ArtistPage : Page
{
    public ArtistPage()
    {
        InitializeComponent();
        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        _rootVisual = ElementCompositionPreview.GetElementVisual(this);
        // TODO: load from user settings or defaults
        _userPrefAnim = SongTransitionAnimation.Slide;
    }

    private readonly Microsoft.UI.Composition.Visual _rootVisual;
    private readonly Microsoft.UI.Composition.Compositor _compositor;
    private readonly SongTransitionAnimation _userPrefAnim;


    BaseViewModelWin MyViewModel { get; set; }

    private TableViewCellSlot _lastActiveCellSlot;

    public SongModelView? DetailedSong { get; set; }
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            MyViewModel = (args.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            MyViewModel.CurrentWinUIPage = this;
            DetailedSong = args.Song;
        }
        MyViewModel.IsBackButtonVisible = WinUIVisibility.Visible;

        this.DataContext = MyViewModel;
        pressedCounter = 0;
        var animation = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("ForwardConnectedAnimation");

        //CoordinatedPanel.Loaded += (_, __) =>
        //{
        //    animation?.TryStart(CoordinatedPanel);
        //};
        if (animation is not null)
        {
            ArtistNameInArtistPage.Loaded += async (_, __) =>
            {
                animation?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { CoordinatedPanel });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(CoordinatedPanel);
                Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                ApplyEntranceEffect(visual);
                ApplyEntranceEffect(visual2);
            };
            return;
        }

        var animFromSingleSongPage = ConnectedAnimationService.GetForCurrentView().
            GetAnimation("MoveViewToArtistPageFromSongDetailPage");
        if (animFromSingleSongPage is not null)
        {
            ArtistNameInArtistPage.Loaded += async (s, ee) =>
            {
                animFromSingleSongPage?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { CoordinatedPanel });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(CoordinatedPanel);
                Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                ApplyEntranceEffect(visual);
                ApplyEntranceEffect(visual2);
            };            
        }
    }

    private void ApplyEntranceEffect(Visual visual, SongTransitionAnimation defAnim = SongTransitionAnimation.Spring)
    {

        switch (defAnim)
        {
            case SongTransitionAnimation.Fade:
                visual.Opacity = 0f;
                var fade = _compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(1f, 1f);
                fade.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Opacity", fade);
                break;

            case SongTransitionAnimation.Scale:
                visual.CenterPoint = new Vector3((float)CoordinatedPanel.ActualWidth / 2,
                                                 (float)CoordinatedPanel.ActualHeight / 2, 0);
                visual.Scale = new Vector3(0.8f);
                var scale = _compositor.CreateVector3KeyFrameAnimation();
                scale.InsertKeyFrame(1f, Vector3.One);
                scale.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Scale", scale);
                break;

            case SongTransitionAnimation.Slide:
                visual.Offset = new Vector3(80f, 0, 0);
                var slide = _compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(1f, Vector3.Zero);
                slide.Duration = TimeSpan.FromMilliseconds(350);
                visual.StartAnimation("Offset", slide);
                break;

            case SongTransitionAnimation.Spring:
            default:
                var spring = _compositor.CreateSpringVector3Animation();
                spring.FinalValue = new Vector3(0, 0, 0);
                spring.DampingRatio = 0.5f;
                spring.Period = TimeSpan.FromMilliseconds(350);
                visual.Offset = new Vector3(0, 40, 0);//c matching
                visual.StartAnimation("Offset", spring);
                break;
        }
    }
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(ArtistNameInArtistPage) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", ArtistNameInArtistPage);
            //}
            if (CoordinatedPanel != null && VisualTreeHelper.GetParent(CoordinatedPanel) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimation", CoordinatedPanel);
            }
        }
    }

    private void CoordinatedPanel2_Click(object sender, RoutedEventArgs e)
    {
        // Standard navigation back
        if (Frame.CanGoBack)
        {
            //var image = detailedImage;
            //ConnectedAnimationService.GetForCurrentView()
            //    .PrepareToAnimate("BackwardConnectedAnimation", image);
            Frame.GoBack();
        }
    }
    int pressedCounter = 0;
    private void MyArtistPage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if(pressedCounter > 0)
        {
            return;
        }
        var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
        var properties = e.GetCurrentPoint(nativeElement).Properties;


        var point = e.GetCurrentPoint(nativeElement);

        if (properties.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.XButton1Pressed)
        {
            CoordinatedPanel2_Click(this, e);
            pressedCounter++;
        }

    }

    private void IsArtFavorite_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {

    }

    private void CheckBox_Click(object sender, RoutedEventArgs e)
    {

    }

    private async void AllAlbumsBtn_Loaded(object sender, RoutedEventArgs e)
    {

        var artistAlbumsCount = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<ArtistModel>(MyViewModel.SelectedArtist!.Id)!
            .Albums.Count();
        var allArtistAlbums = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<ArtistModel>(MyViewModel.SelectedArtist!.Id)!
            .Albums;
        var menuFlyout = new MenuFlyout();
        foreach (var album in allArtistAlbums)
        {
            var albumMenuItem = new MenuFlyoutItem
            {
                Text = $"{album.Name} ({album.SongsInAlbum?.Count()})"
            };
            albumMenuItem.Click += (s, args) =>
            {
                MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(album.Name));

                var count = MyViewModel.SearchResults.Count;
            };
            menuFlyout.Items.Add(albumMenuItem);
        }
        AllAlbumsBtn.Flyout = menuFlyout;

        AllAlbumsBtn.Click += AllAlbumsBtn_Click;
    }

    private void AllAlbumsBtn_Click(object sender, RoutedEventArgs e)
    {
        
        AllAlbumsBtn.Flyout?.ShowAt(AllAlbumsBtn, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions()
        { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Right});

    }

    private void ArtistNameInArtistPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(MyViewModel.SelectedArtist.Name));

    }

    private void ArtistAlbums_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props.IsXButton1Pressed)
        {
            if (Frame.CanGoBack)
            {
                //var image = detailedImage;
                //ConnectedAnimationService.GetForCurrentView()
                //    .PrepareToAnimate("BackwardConnectedAnimation", image);
                Frame.GoBack();
            }
        }
    }

    private async void MostPlayedSongCoverImg_Loaded(object sender, RoutedEventArgs e)
    {
        var topRankedSong = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<ArtistModel>(MyViewModel.SelectedArtist!.Id)!
            .Songs
            .OrderByDescending(x => x.RankInArtist)
            .FirstOrDefault();
        if (topRankedSong != null)
            {
            await MyViewModel.LoadSongImageAsync(topRankedSong.ToSongModelView(), MostPlayedSongCoverImg);
        }
    }

    // 1. A flag to prevent double clicks while processing
    private bool _isTogglingFavorite = false;

    private void IsArtFavorite_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        // 2. SAFETY: Remove the handler first to ensure we never have duplicates 
        // if Loaded fires multiple times (e.g. scrolling/virtualization)
        btn.Click -= OnFavoriteArtistClicked;
        btn.Click += OnFavoriteArtistClicked;

        // 3. Just update the UI visuals
        UpdateFavoriteButtonVisuals(btn);
    }

    private async void OnFavoriteArtistClicked(object sender, RoutedEventArgs e)
    {
        // 4. DEBOUNCER: If we are already working, ignore this click
        if (_isTogglingFavorite) return;

        _isTogglingFavorite = true;
        var btn = (Button)sender;

        try
        {
            // Perform the async DB work
            await MyViewModel.ToggleFavoriteRatingToArtist(DetailedSong.Artist);

            // Refresh the specific Realm object to get the new state
            // (Assuming your ViewModel doesn't automatically update DetailedSong.Artist in place)
            var dbArtist = MyViewModel.RealmFactory.GetRealmInstance()
                .Find<ArtistModel>(DetailedSong.Artist.Id);

            if (dbArtist != null)
            {
                DetailedSong.Artist = dbArtist.ToArtistModelView()!;
            }

            // Update the UI to match the new state
            UpdateFavoriteButtonVisuals(btn);
        }
        finally
        {
            // 5. Release the lock so the user can click again
            _isTogglingFavorite = false;
        }
    }

    // Helper method to draw the button content
    private void UpdateFavoriteButtonVisuals(Button btn)
    {
        // Check if artist is null to prevent crashes
        if (DetailedSong?.Artist == null) return;

        var fontIcon = new FontIcon();
        var toggleFavTxt = new TextBlock();

        // Create the StackPanel
        var favStackPanel = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        if (DetailedSong.Artist.IsFavorite)
        {
            fontIcon.Glyph = "\uEB52"; // Filled Heart
            toggleFavTxt.Text = "Love";
            btn.Background = new SolidColorBrush(Colors.DarkSlateBlue);
        }
        else
        {
            fontIcon.Glyph = "\uEA92"; // Empty Heart
            toggleFavTxt.Text = "UnLove";
            btn.Background = new SolidColorBrush(Colors.Transparent);
        }

        favStackPanel.Children.Add(fontIcon);
        favStackPanel.Children.Add(toggleFavTxt);

        btn.Content = favStackPanel;
    }

    private void ArtistDataTable_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(DetailedSong.Artist.Name));
        


    }

    private void AlbumsIR_Loaded(object sender, RoutedEventArgs e)
    {
        ObservableCollection<AlbumModelView?>? albs = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<ArtistModel>(DetailedSong.Artist.Id)!
            .Albums.ToList().Select(x => x.ToAlbumModelView()).ToObservableCollection();
        DetailedSong.Artist.AlbumsByArtist = albs;
        AlbumsIR.ItemsSource = DetailedSong.Artist.AlbumsByArtist;
    }

    private void Album_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
                var album = (AlbumModelView)send.DataContext;

        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(album.Name));


    }

    private void CardBorder_DropCompleted(UIElement sender, Microsoft.UI.Xaml.DropCompletedEventArgs args)
    {

    }

    private async void CardBorder_Drop(object sender, DragEventArgs e)
    {
        // 1. Check if the drop contains the data format we expect
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            // Example: User dragged a file from Windows Explorer onto the Image
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 && items[0] is StorageFile file)
            {
                // Get the ViewModel associated with the Border we dropped onto
                if (sender is FrameworkElement border && border.DataContext is SongModelView targetSong)
                {
                    // Logic to update the cover image
                    // await MyViewModel.UpdateCoverImage(targetSong, file);
                    Debug.WriteLine($"Dropped file {file.Path} onto song {targetSong.Title}");
                }
            }
        }
        // 2. Check for internal drag (Reordering or swapping)
        else if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var text = await e.DataView.GetTextAsync();
            // Handle internal logic
        }
    }

    private void CardBorder_DragOver(object sender, DragEventArgs e)
    {
        // 1. Allow the drop (Change the icon from "No" to "Copy" or "Move")
        e.AcceptedOperation = (Windows.ApplicationModel.DataTransfer.DataPackageOperation)DataPackageOperation.Copy;

        // Optional: Change the tooltip text next to the cursor
        e.DragUIOverride.Caption = "Drop to Edit Image";
        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsContentVisible = true;
        e.DragUIOverride.IsGlyphVisible = true;
    }

    private void CardBorder_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {

    }

    private void CardBorder_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {

    }

    private void CardBorder_DragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        if (sender is FrameworkElement element && element.DataContext is SongModelView song)
        {
            args.Data.SetText(song.Title);

            args.Data.RequestedOperation = (Windows.ApplicationModel.DataTransfer.DataPackageOperation)DataPackageOperation.Copy;
        }
    }

    private void ResetAblums_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(DetailedSong.ArtistName));
    }

    private async void TitleSection_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // e.OriginalSource is the specific UI element that received the tap 
        // (e.g., a TextBlock, an Image, a Grid, etc.).
        var element = e.OriginalSource as FrameworkElement;
        SongModelView? song = null;
        if (element == null)
            return;



        while (element != null && element != sender)
        {
            if (element.DataContext is SongModelView currentSong)
            {
                song = currentSong;
                break; // Found it!
            }
            element = element.Parent as FrameworkElement;
        }
        var songs = ArtistDataTable.Items;
        

        // now we need items as enumerable of SongModelView

        var SongsEnumerable = songs.OfType<SongModelView>();

        Debug.WriteLine(SongsEnumerable.Count());


        if (song != null)
        {
            // You found the song! Now you can call your ViewModel command.
            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            await MyViewModel.PlaySong(song,curPage:CurrentPage.AllArtistsPage, songs: SongsEnumerable);
        }
    }
}