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

    public SongModelView? DetailedSong { get; set; }
    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        //DetailedSong = DetailedSong is null ? MyViewModel.SelectedSong : DetailedSong;

        if (e.Parameter is SongDetailNavArgs args)
        {
            MyViewModel = (args.ExtraParam as BaseViewModelWin)!;       // reference, not copy
            DetailedSong = args.Song;
        }
        this.DataContext = MyViewModel;

        MyViewModel.IsBackButtonVisible = WinUIVisibility.Visible;
        
    

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
        // Standard navigation back
        if (Frame.CanGoBack)
        {
            //var image = detailedImage;
            //ConnectedAnimationService.GetForCurrentView()
            //    .PrepareToAnimate("BackwardConnectedAnimation", image);
            Frame.GoBack();
        }
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
}
