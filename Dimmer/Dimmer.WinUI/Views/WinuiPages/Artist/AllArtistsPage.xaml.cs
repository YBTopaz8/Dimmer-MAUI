using DynamicData.Binding;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Artist;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllArtistsPage : Page
{
    public AllArtistsPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;

        DataContext = MyViewModel;
        MyViewModel.CurrentPageEnum = CurrentPage.AllArtistsPage;
        await Task.Delay(250);
        MyViewModel.SetupArtistPipeline();
    }
    public BaseViewModelWin MyViewModel { get; set; }



    //FrameworkElement? artistClicked;

    private void ArtistsItemsRepeater_Tapped(object sender, TappedRoutedEventArgs e)
    {
        
    }

    private void ArtistImg_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var propr = e.GetCurrentPoint((UIElement)sender).Properties;
        if (propr != null)
        {
            if (propr.IsLeftButtonPressed)
            {
                FrameworkElement? artistClicked = (FrameworkElement)e.OriginalSource;

                var artist = artistClicked.DataContext as ArtistModelView;

                if (artist != null)
                {

                    MyViewModel.NavigateToArtistPageWithArtistId(artist.Id);
                }
            }

        }
    }

    private void SortRadioBtns_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        if (sender is RadioButtons rb && MyViewModel != null)
        {
            int colorName = rb.SelectedIndex;
            switch (colorName)
            {
                case 0: // Name Asc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Ascending(x => x.Name));
                    break;
                case 1: // Name Desc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Descending(x => x.Name));
                    break;
                case 2: // Total Play Count Asc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Ascending(x => x.TotalCompletedPlays));
                    break;
                case 3: // Total Play Count Desc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Descending(x => x.TotalCompletedPlays));
                    break;
                case 4: // Total Albums Asc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Ascending(x => x.TotalAlbumsByArtist));
                    break;
                case 5: // Total Albums Desc
                    MyViewModel.ArtistSortSubject.OnNext(SortExpressionComparer<ArtistModelView>.Descending(x => x.TotalAlbumsByArtist));
                    break;
            }
        }
    }

    private void ArtistBtnView_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;

        var artist = send.DataContext as ArtistModelView;
    }

    private async void AllArtistsTableView_CellDoubleTapped(object sender, TableViewCellDoubleTappedEventArgs e)
    {
        FrameworkElement element = (e.Cell as FrameworkElement)!;
        ArtistModelView? artist = null;
        if (element == null)
            return;

        if (e.Item is ArtistModelView currentArtist)
        {
            artist = currentArtist;
        }
        if (artist == null)
            return;



        MyViewModel.NavigateToArtistPageWithArtistId(artist.Id);
    }
    

    private void AllArtistsTableView_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void AllArtistsTableView_ItemClick(object sender, ItemClickEventArgs e)
    {

    }
}
