

namespace Dimmer_MAUI.Views.Desktop;

public partial class ArtistsPageD : ContentPage
{
    public ArtistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
        HomePageVM.GetAllArtistsCommand.Execute(null);
        AllArtistsColView.Loaded += AllArtistsColView_Loaded;
    }


    private void AllArtistsColView_Loaded(object? sender, EventArgs e)
    {
        if (AllArtistsColView.IsLoaded)
        {
            if (HomePageVM.SelectedArtistOnArtistPage is null)
            {
                HomePageVM.GetAllArtistsAlbum(HomePageVM.TemporarilyPickedSong.Id, HomePageVM.TemporarilyPickedSong);
            }
            AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;
            AllArtistsColView.ScrollTo(HomePageVM.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);
        }
    }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
        //HomePageVM.GetAllArtistsAlbum(HomePageVM.TemporarilyPickedSong.Id, HomePageVM.TemporarilyPickedSong);
        AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;
        AllArtistsColView.ScrollTo(HomePageVM.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);

    }
    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private async void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.SetSongCoverAsAlbumCover();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        HomePageVM.SearchArtistCommand.Execute(SearchArtistBar.Text);
    }
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var t = (Border)sender;
        var album = t.BindingContext as AlbumModelView;
        HomePageVM.ShowSpecificArtistsSongsWithAlbumIdCommand.Execute(album.Id);
    }

    private void ArtistFromArtistPage_Tapped(object sender, TappedEventArgs e)
    {
        Border view = (Border)sender;
        ArtistModelView artist = view.BindingContext as ArtistModelView;
        HomePageVM.GetAllArtistsAlbum(artist.Id);
    }

    private void AlbumSongsCV_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AlbumSongsCV.IsLoaded)
        {
            AlbumSongsCV.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.MakeVisible);
        }
    }

    private void AllArtistsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (AllAlbumsColView.IsLoaded)
        {
            AllAlbumsColView.ScrollTo(HomePageVM.SelectedArtistOnArtistPage, position: ScrollToPosition.Center, animate: false);
        }
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongsModelView;
        HomePageVM.SetContextMenuSong(song);
    }
}