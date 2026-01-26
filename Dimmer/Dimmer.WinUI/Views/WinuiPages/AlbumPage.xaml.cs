// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AlbumPage : Page
{
    public AlbumPage()
    {
        InitializeComponent();

        this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

        _rootVisual = ElementCompositionPreview.GetElementVisual(this);
    }

    private readonly Microsoft.UI.Composition.Visual _rootVisual;
    private readonly Microsoft.UI.Composition.Compositor _compositor;

    BaseViewModelWin MyViewModel { get; set; }

    public SongModelView DetailedSong { get; set; }
    public AlbumModelView SelectedAlbum { get; private set; }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is BaseViewModelWin args)
        {
            MyViewModel = args; 
            DetailedSong = args.SelectedSong!;
        }
        this.DataContext = MyViewModel;

        MyViewModel.IsBackButtonVisible = WinUIVisibility.Visible;


        var s = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(DetailedSong.Id);
        var ee = s!.Album;
        SelectedAlbum = ee.ToAlbumModelView(withArtist: true, withSongs: true)!;

        MyViewModel.SelectedAlbum  =    SelectedAlbum;
        MyViewModel.SelectedLastFMAlbum = await MyViewModel.LastFMService.GetAlbumInfoAsync(DetailedSong.ArtistName, SelectedAlbum!.Name);
        AnimationHelper.TryStart(
      DestinationElement,
      null,
      AnimationHelper.Key_Forward
  );

        try
        {

            
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);


        if (e.NavigationMode == Microsoft.UI.Xaml.Navigation.NavigationMode.Back)
        {
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(DestinationElement) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", DestinationElement);
            //}
            //if (CoordinatedPanel != null && VisualTreeHelper.GetParent(CoordinatedPanel) != null)
            //{
            //    ConnectedAnimationService.GetForCurrentView()
            //        .PrepareToAnimate("BackConnectedAnimation", CoordinatedPanel);
            //}
        }

    }

    private void CoordinatedPanel2_Click(object sender, RoutedEventArgs e)
    {
       
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            //var image = detailedImage;
            //ConnectedAnimationService.GetForCurrentView()
            //    .PrepareToAnimate("BackwardConnectedAnimation", image);
            Frame.GoBack();
        }
    }

    private void CoverImageAlbumPage_Loaded(object sender, RoutedEventArgs e)
    {
        var UriSource = DetailedSong?.Album?.ImagePath;
        if (!string.IsNullOrEmpty(UriSource))
        {
            CoverImageAlbumPage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(UriSource));
        }

    }

    private void Page_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props != null)
        {
            if (props.IsXButton1Pressed)
            {
 
                if (Frame.CanGoBack)
                {
                    var image = DestinationElement;
                    ConnectedAnimationService.GetForCurrentView()
                        .PrepareToAnimate("BackwardConnectedAnimation", image);
                    Frame.GoBack();
                }
            }
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
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

    private void MyPageGrid_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {

    }

    private void DurationFormatted_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }

    private void AlbumBtn_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

    private void ArtistBtnStackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void TitleColumn_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void SongTitle_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ButtonHover_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    private void ButtonHover_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

    }

    private void ViewOtherBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ExtraPanel_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void coverArtImage_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

    private void coverArtImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CardBorder_PointerReleased(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CardBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CardBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CardBorder_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void CardBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {

    }
    private void AlbumBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ViewSongBtn_Click(object sender, RoutedEventArgs e)
    {

    }
}
