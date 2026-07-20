namespace Dimmer.Views.Album;

public partial class AllAlbumsPage : ContentPage
{
	public AllAlbumsPage(BaseViewModelAnd myViewModel)
    {
        InitializeComponent();
        MyViewModel = myViewModel;
        BindingContext = myViewModel;
    }
    public BaseViewModelAnd MyViewModel { get; }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await Task.Delay(250);
        MyViewModel.SetupAlbumPipeline();
    }



    private async void NavigateToAlbumDetailsButton_Tapped(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

        DXButton send = (DXButton)sender;
        var album = send.CommandParameter as AlbumModelView;

        MyViewModel.SetSelectedAlbum(album);
        await Shell.Current.GoToAsync(nameof(AlbumPage));


    }

    private void AlbumSongsCountChip_Tap(object sender, HandledEventArgs e)
    {
        var album = ((Chip)sender).BindingContext as AlbumModelView;
        if (album != null)
        {
            MyViewModel.SetSelectedAlbum(album);
            AlbumSongsCV.ItemsSource = album.SongsInAlbum;
            AlbumSongsBtmSheet.Show();
        }
    }

    private async void AlbumSongsCV_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {

        var song = e.Item as SongModelView;
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage, MyViewModel.SearchResults);

    }

    private void AddSongToNextInPlaylist_Tap(object sender, DXTapEventArgs e)
    {
        DXButton send = (DXButton)sender;
        var song = send.CommandParameter as SongModelView;
        if (song is null)
            return;
        MyViewModel.AddToNext(new List<SongModelView> { song });
    }

   


}