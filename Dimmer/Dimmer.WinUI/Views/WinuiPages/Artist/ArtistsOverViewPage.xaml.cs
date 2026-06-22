// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Artist;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ArtistsOverViewPage : Page
{
    public ArtistsOverViewPage()
    {
        InitializeComponent();
        _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is BaseViewModelWin baseVM)
        {
            MyViewModel = baseVM;
            DataContext = baseVM;
        }
    }
    public BaseViewModelWin MyViewModel { get; set; }

    private void StyledGrid_ItemClick(object sender, ItemClickEventArgs e)
    {

    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void AllArtistsIR_Tapped(object sender, TappedRoutedEventArgs e)
    {


    }

    private void AllArtistsIR_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

    private void ArtistName_Click(object sender, RoutedEventArgs e)
    {
        var send = (UIElement)sender;
        var sendFE = (FrameworkElement)sender;

        Debug.WriteLine(sendFE.DataContext is null);
    }

    private void ArtistMenuFlyout_Opening(object sender, object e)
    {

    }
    private void ArtistBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

        UIControlsAnims.AnimateBtnPointerEntered((Button)sender, _compositor);
    }

    private void ArtistBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UIControlsAnims.AnimateBtnPointerExited((Button)sender, _compositor);

    }
    private readonly Microsoft.UI.Composition.Compositor _compositor;
    private int previousSelectedIndex;

    private void ExpandableCard_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void detailedImage_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void PlaySongBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void PlaySongBtn_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void ArtistToSong_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void ArtistNameBtn_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void Artist_Click(object sender, RoutedEventArgs e)
    {

    }

    private void CloseArtistDetail_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ArtistNameBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void ArtistSongsPreviewIR_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void PlaySongNowFromArtistListOfSongs_Click(object sender, RoutedEventArgs e)
    {

    }

    private void PlaySongNowFromArtistListOfSongs_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {

    }

    private void AddNextInQueue_Click(object sender, RoutedEventArgs e)
    {

    }


    private void ArtistSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        System.Type pageType;

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(AllArtistsPage);
                break;
            case 1:
                pageType = typeof(ArtistPage);

                break;
            case 2:
                pageType = typeof(ArtistPage);

                break;
            case 3:
                pageType = typeof(ArtistPage);

                break;
            default:
                pageType = typeof(ArtistPage);

                break;
        }

        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ArtistContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;
    }

    private void FetchAllInfos_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<ArtistModelView> artists = MyViewModel.ArtistsCollection.ToList();
            foreach (var art in artists)
            {
                _ = MyViewModel.LoadLastFMArtist(art);

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }
}