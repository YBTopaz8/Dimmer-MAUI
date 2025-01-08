

using Syncfusion.Maui.Toolkit.EffectsView;

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
        if (HomePageVM.SelectedAlbumOnArtistPage is not null)
        {
            HomePageVM.SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        }
        if (HomePageVM.SelectedArtistOnArtistPage is not null)
        {
            HomePageVM.SelectedArtistOnArtistPage.IsCurrentlySelected = false;
        }
       

        //AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;

        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
        AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;

        if (HomePageVM.SelectedSongToOpenBtmSheet is null)
        {

            if (HomePageVM.TemporarilyPickedSong is not null)
            {
                HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
                HomePageVM.GetAllArtistsAlbum(song: HomePageVM.TemporarilyPickedSong, isFromSong: true);
                
            }
        }
        else
        {
            HomePageVM.GetAllArtistsAlbum();
        }
        if (HomePageVM.SelectedArtistOnArtistPage is not null)
        {
            AllArtistsColView.ScrollTo(HomePageVM.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);
            AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;
        }
        
        
        
    }
    private async void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongModelView;
        await HomePageVM.PlaySong(song);
    }

    private async void SetSongCoverAsAlbumCover_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        await HomePageVM.SetSongCoverAsAlbumCover(song!);
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        HomePageVM.SearchArtistCommand.Execute(SearchArtistBar.Text);
    }
    private async void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        await HomePageVM.GetSongsFromAlbumId(curSel!.LocalDeviceId);
        //await HomePageVM.GetAllAlbumInfos(curSel);
        //await HomePageVM.ShowSpecificArtistsSongsWithAlbum(curSel);
    }

    private void AlbumSongsCV_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (AlbumSongsCV.IsLoaded)
        //{
        //    AlbumSongsCV.ScrollTo(HomePageVM.PickedSong, ScrollToPosition.MakeVisible);
        //}
    }

    

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongModelView;
        HomePageVM.SetContextMenuSong(song!);
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        HomePageVM.LoadSongsFromArtistId(HomePageVM.SelectedArtistOnArtistPage.LocalDeviceId);
    }


    ArtistModelView currentlySelectedArtist;
    private void ArtistView_TouchDown(object sender, EventArgs e)
    {
        SfEffectsView view = (SfEffectsView)sender;
        ArtistModelView artist = (view.BindingContext as ArtistModelView)!;

        //artist.IsCurrentlySelected = true;
        HomePageVM.GetAllArtistAlbumFromArtist(artist);
        
        //await HomePageVM.GetAllArtistAlbumFromArtist(artist);


        //var AlbumArtist = HomePageVM.AllLinks!.FirstOrDefault(x => x.ArtistId == artist.LocalDeviceId)!.AlbumId;
        //var album = HomePageVM.AllAlbums.FirstOrDefault(x => x.LocalDeviceId == AlbumArtist);
        //HomePageVM.GetAllArtistsAlbum(album: album, isFromSong: false);
    }
}