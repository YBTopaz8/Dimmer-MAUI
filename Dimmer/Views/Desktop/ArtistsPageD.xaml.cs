

namespace Dimmer_MAUI.Views.Desktop;

public partial class ArtistsPageD : ContentPage
{
    public ArtistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
        HomePageVM.GetAllArtistsCommand.Execute(null);
    }

  
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }

        AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
        AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;

        if (HomePageVM.SelectedSongToOpenBtmSheet is null)
        {
            HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        }
        HomePageVM.GetAllArtistsAlbum(song: HomePageVM.TemporarilyPickedSong, isFromSong: true);
        AllArtistsColView.ScrollTo(HomePageVM.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);

        AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;
     
    }
    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }

    private async void SetSongCoverAsAlbumCover_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongsModelView;
        await HomePageVM.SetSongCoverAsAlbumCover(song);
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        HomePageVM.SearchArtistCommand.Execute(SearchArtistBar.Text);
    }
    private async void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        
        await HomePageVM.ShowSpecificArtistsSongsWithAlbum(curSel);
    }

    private async void ArtistFromArtistPage_Tapped(object sender, TappedEventArgs e)
    {
        Border view = (Border)sender;
        ArtistModelView artist = view.BindingContext as ArtistModelView;
        await HomePageVM.GetAllArtistAlbumFromArtist(artist);
        
        var AlbumArtist = HomePageVM.AllLinks.FirstOrDefault(x => x.ArtistId == artist.Id).AlbumId;
        var album = HomePageVM.AllAlbums.FirstOrDefault(x => x.Id == AlbumArtist);
        HomePageVM.GetAllArtistsAlbum(album:album,isFromSong:false);
    }

    private void AlbumSongsCV_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AlbumSongsCV.IsLoaded)
        {
            AlbumSongsCV.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.MakeVisible);
        }
    }

    

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongsModelView;
        HomePageVM.SetContextMenuSong(song);
    }

    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var album = send.BindingContext as AlbumModelView;
        await HomePageVM.ShowSpecificArtistsSongsWithAlbum(album);
    }
}