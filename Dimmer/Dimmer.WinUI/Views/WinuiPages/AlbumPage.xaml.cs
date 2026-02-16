// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using CommunityToolkit.Maui.Core.Extensions;

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


        var ss = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(DetailedSong.Id)!.Album ;

        SelectedAlbum = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(DetailedSong.Id)!.Album.ToAlbumModelView(withArtist: true, withSongs: true)!; ;

        MyViewModel.SetSelectedAlbum (SelectedAlbum);
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
           
        }

    }

    private void CoordinatedPanel2_Click(object sender, RoutedEventArgs e)
    {
       
    }

    private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var prop = e.GetCurrentPoint((UIElement)sender).Properties;
        if (prop != null && prop.IsXButton1Pressed)
        {
            
            if (Frame.CanGoBack)
            {
                AnimationHelper.Prepare(AnimationHelper.Key_DetailToListFromAlbum,
               DestinationElement, AnimationHelper.ConnectedAnimationStyle.ScaleDown);
                Frame.GoBack();
            }
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
                    AnimationHelper.Prepare(AnimationHelper.Key_DetailToListFromAlbum,
                DestinationElement, AnimationHelper.ConnectedAnimationStyle.ScaleDown);
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
            AnimationHelper.Prepare(AnimationHelper.Key_DetailToListFromAlbum,
               DestinationElement, AnimationHelper.ConnectedAnimationStyle.ScaleDown);
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

    private void ArtistDataTable_Loaded(object sender, RoutedEventArgs e)
    {

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

            songV.CoverImagePath = song.CoverImagePath;

            await MyViewModel.AssignImageToSong(songV);
        }
    }


    private void ArtistSongTitle_Click(object sender, RoutedEventArgs e)
    {
        var songFrameworkElement = (FrameworkElement)sender;
        var selectedSong = songFrameworkElement.DataContext as SongModelView;

        var row = ArtistDataTable.ContainerFromItem(selectedSong) as FrameworkElement;
        var image = PlatUtils.FindVisualChild<Image>(row, "coverArtImage");
        if (image == null) return;

        ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("ArtistToSongDetailsAnim", image);



        MyViewModel.SelectedSong = selectedSong;
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(SongDetailPage));
        
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

            Debug.WriteLine($"Double-tapped on song: {song.Title}");
            await MyViewModel.PlaySongWithActionAsync(song,PlaybackAction.PlayNow, SongsEnumerable);
        }
    }

    private void DestinationElement_Loaded(object sender, RoutedEventArgs e)
    {

        AnimationHelper.TryStart(DestinationElement,null, AnimationHelper.Key_ToAlbumPage);
    }

    private void ArtistsInAlbumIR_Loaded(object sender, RoutedEventArgs e)
    {
        ArtistsInAlbumIR.ItemsSource = MyViewModel.RealmFactory.GetRealmInstance()
            .Find<AlbumModel>(MyViewModel.SelectedAlbum.Id)
            .Artists.DistinctBy(x => x.Name).ToObservableCollection();
    }

    private void ArtistBtn_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Prepare(AnimationHelper.Key_AlbumToArtist,
            (FrameworkElement)sender, AnimationHelper.ConnectedAnimationStyle.GravitySwing);
    }
}
