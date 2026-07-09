using System.Text.RegularExpressions;
using DevWinUI;
using Dimmer.ViewModel.StatsVMs;
using Microsoft.UI.Xaml.Documents;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
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
        this.NavigationCacheMode = NavigationCacheMode.Enabled;

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        _rootVisual = ElementCompositionPreview.GetElementVisual(this);

    }

    private readonly Microsoft.UI.Composition.Visual _rootVisual;
    private readonly Microsoft.UI.Composition.Compositor _compositor;


    BaseViewModelWin MyViewModel { get; set; }

    public SongModelView? DetailedSong { get; set; }
    public ArtistModelView? SelectedArtist { get; set; }
    public ArtistStatsViewModel MyArtistStatsViewModel { get; private set; }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is BaseViewModelWin args)
        {
            MyViewModel = args;       // reference, not copy
            MyViewModel.CurrentPageEnum = CurrentPage.SingleArtistPage;
            DetailedSong = args.CurrentPlayingSongView;
        }

        if (e.Parameter is SongDetailNavArgs songDetailNavArgs)
        {
            //Song = _storedSong!,
            //    ExtraParam = MyViewModel,
            //    ViewModel = MyViewModel
            MyViewModel = (songDetailNavArgs.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            MyViewModel.CurrentPageEnum = CurrentPage.SingleArtistPage;
            DetailedSong = songDetailNavArgs.Song;
        }

        MyViewModel ??= IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        MyArtistStatsViewModel ??= IPlatformApplication.Current!.Services.GetService<ArtistStatsViewModel>()!;
        MyViewModel.IsBackButtonVisible = WinUIVisibility.Visible;


        this.DataContext = MyViewModel.SelectedArtist;
        SelectedArtist = MyViewModel.SelectedArtist;
        pressedCounter = 0;
        MyArtistStatsViewModel.LoadArtist(SelectedArtist.Id);
        await  LoadWikiOfArtist();

        _ = Task.Run(async () =>
        {
            //await MyStatsViewModel.LoadArtistStatsAsync(SelectedArtist);
        });

    }
    private async Task LoadWikiOfArtist()
    {
      await  MyViewModel.LoadLastFMArtist(MyViewModel.SelectedArtist);

        if (MyViewModel.SelectedArtist is null ) return;
        if (MyViewModel.SelectedArtist.Bio is null ) return;
        var html = MyViewModel.SelectedArtist.Bio;
     
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

        //BioBlock.Blocks.Add(p);
    }

    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
           

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

    private void ArtistNameInArtistPage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
      
    }



    // 1. A flag to prevent double clicks while processing
    private bool _isTogglingFavorite = false;

    // Helper method to draw the button content
    private void UpdateFavoriteButtonVisuals(Button btn)
    {
        if (_isTogglingFavorite) return;
        _isTogglingFavorite = true;

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

        btn.Content = favStackPanel; _isTogglingFavorite = false;
    }

    //private void ArtistDataTable_Loaded(object sender, RoutedEventArgs e)
    //{
    //    ArtistDataTable.ItemsSource = MyViewModel.SelectedArtist!.SongsByArtist;



    //}

    //private void AlbumsIR_Loaded(object sender, RoutedEventArgs e)
    //{
    //    if (MyViewModel.SelectedArtist?.AlbumsByArtist is null) return;


    //    AlbumsIR.ItemsSource = MyViewModel.SelectedArtist.AlbumsByArtist;
    //}

    Button prevAlbBtn;
    private async void Album_Click(object sender, RoutedEventArgs e)
    {

        var send = (Button)sender;
                var album = (AlbumModelView)send.DataContext;

        AlbumModelView? prevAlb ;
        if (prevAlbBtn is not null)
        {
            prevAlbBtn.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Transparent);
             prevAlb = prevAlbBtn.DataContext as AlbumModelView;

        }
        prevAlbBtn = send;

        send.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.DarkSlateBlue);
        send.BorderThickness = new Microsoft.UI.Xaml.Thickness(2);

        if (album.SongsInAlbum is null || album.SongsInAlbum.Count < 1)
        {
            MyViewModel.SetSelectedAlbum(album);
        }
        //ArtistDataTable.ItemsSource = album.SongsInAlbum;
        return;
      

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
        MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.ByArtist(MyViewModel.SelectedArtist.Name));
    }

    private async void TitleSection_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        
    }


    private async void LastFM_Click(object sender, RoutedEventArgs e)
    {
       await MyViewModel.LoadLastFMArtist(MyViewModel.SelectedArtist);
    }

    private void CollectionOfArtistTopSongs_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private async void AlbumsByArtistGridView_Loaded(object sender, RoutedEventArgs e)
    {
        var selArtistAlbums = MyViewModel.SelectedArtist?.AlbumsByArtist;
        if (selArtistAlbums is null) return;
        for (int i = 0; i < selArtistAlbums.Count; i++)
        {
            
            var alb = selArtistAlbums.ElementAtOrDefault(i);
        

            if(string.IsNullOrEmpty(alb?.ImagePath))
            {
               alb= await MyViewModel.LoadAndSaveAlbumImageFromLastFMAsync(SelectedArtist, alb);
                
            }
        }

        MyViewModel.SelectedArtist?.AlbumsByArtist = selArtistAlbums;
    }

    private void StatisticsSection_Loaded(object sender, RoutedEventArgs e)
    {
        Grid send = (Grid)sender;
        send.DataContext = MyArtistStatsViewModel;
        
    }

    private void ArtistImg_Loaded(object sender, RoutedEventArgs e)
    {
        var artImg = (Image)sender;
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.SelectedArtist), art => MyViewModel.SelectedArtist)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(selArt =>
            {
                if (selArt is null) return;
                if(!string.IsNullOrEmpty(selArt.ImagePath))
                {
                    artImg.Source = new BitmapImage(new Uri(selArt.ImagePath));
                }
            });
    }


    private void SongStats_Loaded(object sender, RoutedEventArgs e)
    {
        StackPanel send = (StackPanel)sender;
        if (MyArtistStatsViewModel is null) return;
        send.DataContext = MyArtistStatsViewModel;

    }

    private void ListEraPreference_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListEraPreference), v => MyArtistStatsViewModel?.ListEraPreference)
            .Subscribe(insight =>
            {
                ListEraPreference.ItemsSource = insight;
            });
    }

    private void ListTopSongs_Loaded(object sender, RoutedEventArgs e)
    {
        
    }

    private void ListDeepCuts_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListDeepCuts), v => MyArtistStatsViewModel?.ListDeepCuts)
            .Subscribe(insight =>
            {
                ListDeepCuts.ItemsSource = insight;
            });
    }

    private void ListMonthlyTrend_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListMonthlyTrend), v => MyArtistStatsViewModel?.ListMonthlyTrend)
            .Subscribe(insight =>
            {
                ListMonthlyTrend.ItemsSource = insight;
            });
    }

    private void ListTopAlbums_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListTopAlbums), v => MyArtistStatsViewModel?.ListTopAlbums)
            .Subscribe(insight =>
            {
                ListTopAlbums.ItemsSource = insight;
            });
    }

    private void ArtistSongsDG_Loaded(object sender, RoutedEventArgs e)
    {
        ArtistSongsDG.ItemsSource = MyViewModel.SelectedArtist?.SongsByArtist;
    }

    private void StoreCarouselTopSongs_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListTopSongs), v => MyArtistStatsViewModel?.ListTopSongs)
            .Subscribe(insight =>
            {
                StoreCarouselTopSongs.ItemsSource = insight?.Where(x =>File.Exists(x.ImagePath)).ToList();
            });
    }

    private void StoreCarouselTopSongs_ItemClick(object sender, StoreCarouselEventArgs e)
    {

    }

    private void ArtistSongsDG_CellDoubleTapped(object sender,TableViewCellDoubleTappedEventArgs e)
    {

    }

    private void ListPlaySkipRatio_Loaded(object sender, RoutedEventArgs e)
    {
        MyArtistStatsViewModel?.WhenPropertyChanged(nameof(MyArtistStatsViewModel.ListPlaySkipRatio), v => MyArtistStatsViewModel?.ListPlaySkipRatio)
            .Subscribe(insight =>
            {
                ListPlaySkipRatio.ItemsSource = insight;
            });
    }
}