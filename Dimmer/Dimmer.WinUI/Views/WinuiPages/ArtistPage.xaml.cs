using System.DirectoryServices;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.Maui.Core.Extensions;

using Hqub.Lastfm.Entities;

using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media.Imaging;

using Color = System.Drawing.Color;
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

    private TableViewCellSlot? _lastActiveCellSlot;

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
        ArtistNameInArtistPage.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        ArtistImageInArtistPage.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        this.DataContext = MyViewModel;
        pressedCounter = 0;
        var animationBack = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("BackConnectedAnimation");

        if (animationBack is not null)
        {
            ArtistNameInArtistPage.Loaded += async (_, __) =>
            {
                animationBack?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { ArtistImageInArtistPage });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(ArtistImageInArtistPage);
                Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                ApplyEntranceEffect(visual);
                ApplyEntranceEffect(visual2);
            };
            return;
        }
        var animation = ConnectedAnimationService.GetForCurrentView()
       .GetAnimation("ForwardConnectedAnimation");

        if (animation is not null)
        {
            ArtistNameInArtistPage.Loaded += async (_, __) =>
            {
                animation?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { ArtistImageInArtistPage });

                await Task.Delay(500);
                Visual? visual = ElementCompositionPreview.GetElementVisual(ArtistImageInArtistPage);
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
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    animFromSingleSongPage?.TryStart(ArtistNameInArtistPage, new List<UIElement>() { ArtistImageInArtistPage });

                    Visual? visual = ElementCompositionPreview.GetElementVisual(ArtistImageInArtistPage);
                    Visual? visual2 = ElementCompositionPreview.GetElementVisual(ArtistNameInArtistPage);
                    ApplyEntranceEffect(visual);
                    ApplyEntranceEffect(visual2);
                });
            };            
        }
    }
    private async Task LoadWikiOfArtist()
    {
      await  MyViewModel.LoadLastFMArtist(MyViewModel.SelectedArtist);

        if (MyViewModel.SelectedArtist is null ) return;
        if (MyViewModel.SelectedArtist.Bio is null ) return;
        var html = MyViewModel.SelectedArtist.Bio;
        BioBlock.Blocks.Clear();

        Paragraph p = new Paragraph();

        var parts = html.Split(new[] { "<a", "</a>" }, StringSplitOptions.None);

        p.Inlines.Add(new Run { Text = parts[0] });

        // crude but works for Last.fm since it only has 1 link
        if (parts.Length > 1)
        {
            // Extract the URL
            var hrefMatch = Regex.Match(html, "href=\"(.*?)\"");
            string url = hrefMatch.Success ? hrefMatch.Groups[1].Value : "";

            Hyperlink h = new Hyperlink();
            h.Inlines.Add(new Run { Text = "Read more on Last.fm" });
            h.NavigateUri = new Uri(url);

            p.Inlines.Add(h);
        }

        BioBlock.Blocks.Add(p);
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
                visual.CenterPoint = new Vector3((float)ArtistImageInArtistPage.ActualWidth / 2,
                                                 (float)ArtistImageInArtistPage.ActualHeight / 2, 0);
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
    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(ArtistNameInArtistPage) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", ArtistNameInArtistPage);
            //}
            ArtistNameInArtistPage.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            ArtistImageInArtistPage.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            if (ArtistImageInArtistPage != null && VisualTreeHelper.GetParent(ArtistImageInArtistPage) != null)
            {
                ConnectedAnimationService.GetForCurrentView()
                    .PrepareToAnimate("BackConnectedAnimationFromArtistPage", ArtistImageInArtistPage);
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

        //var artistAlbumsCount = MyViewModel.RealmFactory.GetRealmInstance()
        //    .Find<ArtistModel>(MyViewModel.SelectedArtist!.Id)!
        //    .Albums.Count();
        //var allArtistAlbums = MyViewModel.RealmFactory.GetRealmInstance()
        //    .Find<ArtistModel>(MyViewModel.SelectedArtist!.Id)!
        //    .Albums;
        //var menuFlyout = new MenuFlyout();
        //foreach (var album in allArtistAlbums)
        //{
        //    var albumMenuItem = new MenuFlyoutItem
        //    {
        //        Text = $"{album.Name} ({album.SongsInAlbum?.Count()})"
        //    };
        //    albumMenuItem.Click += (s, args) =>
        //    {
        //        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByAlbum(album.Name));

        //        var count = MyViewModel.SearchResults.Count;
        //    };
        //    menuFlyout.Items.Add(albumMenuItem);
        //}
        //AllAlbumsBtn.Flyout = menuFlyout;

        //AllAlbumsBtn.Click += AllAlbumsBtn_Click;
    }

    private void AllAlbumsBtn_Click(object sender, RoutedEventArgs e)
    {
        
        //AllAlbumsBtn.Flyout?.ShowAt(AllAlbumsBtn, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions()
        //{ Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Right});

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
            await MyViewModel.ToggleFavoriteRatingToArtist(MyViewModel.SelectedArtist);

            // Refresh the specific Realm object to get the new state
            // (Assuming your ViewModel doesn't automatically update MyViewModel.SelectedArtist in place)
            var dbArtist = MyViewModel.RealmFactory.GetRealmInstance()
                .Find<ArtistModel>(MyViewModel.SelectedArtist.Id);

            if (dbArtist != null)
            {
                MyViewModel.SelectedArtist = dbArtist.ToArtistModelView()!;
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

        if (MyViewModel.SelectedArtist.IsFavorite)
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
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(MyViewModel.SelectedArtist.Name));
        


    }

    private void AlbumsIR_Loaded(object sender, RoutedEventArgs e)
    {
        
        ObservableCollection<AlbumModelView>? albs = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<ArtistModel>(MyViewModel.SelectedArtist.Id)!
            .Albums.ToList().Select(x =>
            {
                var modelV = x.ToAlbumModelView();
                modelV.NumberOfTracks=x.SongsInAlbum.Count();
                modelV.TrackTotal=x.SongsInAlbum.Count();
                modelV.Artists = x.Artists.Select(a => a.ToArtistModelView()).ToList();
                modelV.SongsInAlbum = x.SongsInAlbum.AsEnumerable().Select(x=>x.ToSongModelView()).ToObservableCollection();
                if(modelV.ImagePath == "musicalbum.png" || string.IsNullOrEmpty(modelV.ImagePath))
            {
                var firstSongInAlbumWithValidCoverImage = x.SongsInAlbum
                    .FirstOrDefault(s => !string.IsNullOrEmpty(s.CoverImagePath));
                    if (firstSongInAlbumWithValidCoverImage != null)
                    {
                        modelV.ImagePath = firstSongInAlbumWithValidCoverImage.CoverImagePath;
                        MyViewModel.UpdateAlbumImage(modelV, firstSongInAlbumWithValidCoverImage.CoverImagePath);
                    }
                    
            }
                return modelV;
            }).ToObservableCollection();
     
        MyViewModel.SelectedArtist.AlbumsByArtist = albs;
        AlbumsIR.ItemsSource = MyViewModel.SelectedArtist.AlbumsByArtist;
    }

    Button prevAlbBtn;
    private async void Album_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
                var album = (AlbumModelView)send.DataContext;

        AlbumModelView prevAlb ;
        if (prevAlbBtn is not null)
        {
            prevAlbBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Transparent);
             prevAlb = prevAlbBtn.DataContext as AlbumModelView;

        }
        prevAlbBtn = send;

        send.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.DarkSlateBlue);
        send.BorderThickness = new Microsoft.UI.Xaml.Thickness(2);

        MyViewModel.SearchSongForSearchResultHolder($"Songs by {MyViewModel.SelectedArtist!.Name} and in album {album.Name}");
        var albmInDb = MyViewModel.RealmFactory.GetRealmInstance()
            .All<SongModel>()
            .Where(x => x.AlbumName == album.Name);
        var albmInDbThree = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<AlbumModel>(album.Id);
            
        var count = albmInDb.Count();
        var count3 = albmInDbThree.SongsInAlbum.Count();
        if (album != null && album.ImagePath == "musicalbum.png")
        {
            var firstSongInAlbumWithValidCoverImage = MyViewModel.RealmFactory.GetRealmInstance()
                .Find<AlbumModel>(album.Id)!
                .SongsInAlbum
                .FirstOrDefault(s => !string.IsNullOrEmpty(s.CoverImagePath));
            if (firstSongInAlbumWithValidCoverImage != null)
                album.ImagePath = firstSongInAlbumWithValidCoverImage.CoverImagePath;
            MyViewModel.UpdateAlbumImage(album, firstSongInAlbumWithValidCoverImage.CoverImagePath);
        }
        await Task.Delay(1500);

        var realCount = MyViewModel.SearchResults.Count;
        Debug.WriteLine(realCount);
        if(realCount ==0)
        {
            var realm = MyViewModel.RealmFactory.GetRealmInstance();
            var frst= albmInDbThree.SongsInAlbum.FirstOrDefault();
            if (frst == null) return;
            var realTrack = new ATL.Track(frst.FilePath);
            var albInDB = realm.All<AlbumModel>().FirstOrDefault(x => x.Name == realTrack.Album);
            await realm.WriteAsync(() =>
            {
            if (albInDB != null)
            {
               
                    foreach (var song in albmInDbThree.SongsInAlbum)
                    {
                        song.Album = albInDB;
                        song.AlbumName = albInDB.Name;

                    }
            }
            else
            {
                AlbumModel newAlbum = new AlbumModel();
                newAlbum.Name = realTrack.Album;
                newAlbum.ImagePath = albmInDbThree.ImagePath;
                newAlbum.DiscNumber = albmInDbThree.DiscNumber;
                newAlbum.ImagePath = albmInDbThree.ImagePath;
                foreach (var song in albmInDbThree.SongsInAlbum)
                {
                    song.Album = newAlbum;
                    song.AlbumName = newAlbum.Name;

                }
                }
            });

            Debug.WriteLine($"New Count ${albmInDbThree.SongsInAlbum.Count()}");
        }
    }

    private void CardBorder_DropCompleted(UIElement sender, Microsoft.UI.Xaml.DropCompletedEventArgs args)
    {

    }

    private async void CardBorder_Drop(object sender, DragEventArgs e)
    {
        var frameworkElt = (FrameworkElement)sender;
        var songV = frameworkElt.DataContext as SongModelView;
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
            var songID = await e.DataView.GetTextAsync();
            var song = MyViewModel.SearchResults.First(x => x.Id.ToString() == songID);
            
            songV.CoverImagePath =song.CoverImagePath;

          await MyViewModel.AssignImageToSong(songV);
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
            args.Data.SetText(song.Id.ToString());
            
            args.Data.RequestedOperation = (Windows.ApplicationModel.DataTransfer.DataPackageOperation)DataPackageOperation.Copy;
        }
    }

    private void ResetAblums_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.ByArtist(MyViewModel.SelectedArtist.Name));
    }

    private async void TitleSection_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        
    }

    private async void PlayBtn_Click(object sender, RoutedEventArgs e)
    {
        // e.OriginalSource is the specific UI element that received the tap 
        // (e.g., a TextBlock, an Image, a Grid, etc.).
        var element = e.OriginalSource as FrameworkElement;
        SongModelView? song = null;
        if (element == null)
            return;



        
        if (element.DataContext is SongModelView currentSong)
        {
            song = currentSong;
        }
        
        var songs = ArtistDataTable.Items;



        var SongsEnumerable = songs.OfType<SongModelView>();

        if (song != null)
        {
            // You found the song! Now you can call your ViewModel command.
            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            await MyViewModel.PlaySong(song, curPage: CurrentPage.AllArtistsPage, songs: SongsEnumerable);
        }
    }

    private async void ArtistImageInArtistPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var img = (Image)sender;
        var element = e.OriginalSource as FrameworkElement;
        if (element == null)
            return;

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".png");
        picker.CommitButtonText = "Select as Artist Image";
        picker.SuggestedStartLocation = PickerLocationId.Downloads;
        picker.ViewMode = PickerViewMode.Thumbnail;

        var window = MyViewModel.MainWindow;
        if (window != null)
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hWnd);
        }

        // 3. Pick the file
        var file = await picker.PickSingleFileAsync();

        if (file == null) return; // User cancelled

        // 4. Create the BitmapImage
        var bitmap = new  BitmapImage();

        // 5. Open the stream and assign it to the bitmap
        using (var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
        {
            await bitmap.SetSourceAsync(stream);
        }

        // 6. Assign to the Image Control
        img.Source = bitmap;
        ArtistImage.Source = bitmap;
        await MyViewModel.AssignImageToArtist(MyViewModel.SelectedArtist);
        // TODO: Update your ViewModel or Database with 'file.Path' to persist the change
        // ViewModel.SelectedArtist.ImagePath = file.Path;
    }

    private void ArtistSongTitle_Click(object sender, RoutedEventArgs e)
    {
        var songFrameworkElement = (FrameworkElement)sender;
        var selectedSong = songFrameworkElement.DataContext as SongModelView;

        var row = ArtistDataTable.ContainerFromItem(selectedSong) as FrameworkElement;
        var image = PlatUtils.FindVisualChild<Image>(row, "coverArtImage");
        if (image == null) return;

        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ArtistToSongDetailsAnim", image);
        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ArtistToSongDetailsAnim", songFrameworkElement);



        MyViewModel.SelectedSong = selectedSong;
        var dimmerWindow = MyViewModel.winUIWindowMgrService.GetWindow<DimmerWin>();
        dimmerWindow ??= MyViewModel.winUIWindowMgrService.CreateWindow<DimmerWin>();



        //MyViewModel.DimmerMultiWindowCoordinator.BringToFront()
        if (dimmerWindow != null)
            dimmerWindow.NavigateToPage(typeof(SongDetailPage));

    }

    private async void BioBlock_Loaded(object sender, RoutedEventArgs e)
    {
      await  LoadWikiOfArtist();
    }
}