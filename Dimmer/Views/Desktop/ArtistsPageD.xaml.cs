

using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.Desktop;

public partial class ArtistsPageD : ContentPage
{
    public ArtistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        this.BindingContext = homePageVM;
        MyViewModel.GetAllArtistsCommand.Execute(null);
    }

  
    public HomePageVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (MyViewModel.SelectedAlbumOnArtistPage is not null)
        {
            MyViewModel.SelectedAlbumOnArtistPage.IsCurrentlySelected = false;
        }
        if (MyViewModel.SelectedArtistOnArtistPage is not null)
        {
            MyViewModel.SelectedArtistOnArtistPage.IsCurrentlySelected = false;
        }
       

        //AllAlbumsColView.SelectedItem = MyViewModel.SelectedAlbumOnArtistPage;

        MyViewModel.CurrentPage = PageEnum.AllArtistsPage;
        AllArtistsColView.SelectedItem = MyViewModel.SelectedArtistOnArtistPage;

        if (MyViewModel.MySelectedSong is null)
        {

            if (MyViewModel.TemporarilyPickedSong is not null)
            {
                MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;
                MyViewModel.GetAllArtistsAlbum(song: MyViewModel.TemporarilyPickedSong, isFromSong: true);
                
            }
        }
        else
        {
            MyViewModel.GetAllArtistsAlbum();
        }
        if (MyViewModel.SelectedArtistOnArtistPage is not null)
        {
            AllArtistsColView.ScrollTo(MyViewModel.SelectedArtistOnArtistPage, null, ScrollToPosition.Center, false);
            AllArtistsColView.SelectedItem = MyViewModel.SelectedArtistOnArtistPage;
        }
        
        
        
    }

    private async void SetSongCoverAsAlbumCover_Clicked(object sender, EventArgs e)
    {
        var send = (MenuFlyoutItem)sender;
        var song = send.BindingContext as SongModelView;
        await MyViewModel.SetSongCoverAsAlbumCover(song!);
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        MyViewModel.SearchArtistCommand.Execute(SearchArtistBar.Text);
    }
    private async void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
        //await MyViewModel.GetAllAlbumInfos(curSel);
        //await MyViewModel.ShowSpecificArtistsSongsWithAlbum(curSel);
    }

    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext! as SongModelView;
        MyViewModel.SetContextMenuSong(song!);
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        MyViewModel.GetAllSongsFromArtistID(MyViewModel.SelectedArtistOnArtistPage.LocalDeviceId);
    }


    ArtistModelView currentlySelectedArtist;
    private void ArtistView_TouchDown(object sender, EventArgs e)
    {
        SfEffectsView view = (SfEffectsView)sender;
        ArtistModelView artist = (view.BindingContext as ArtistModelView)!;

        //artist.IsCurrentlySelected = true;
        MyViewModel.GetAllArtistAlbumFromArtistModel(artist);
        
        //await MyViewModel.GetAllArtistAlbumFromArtist(artist);


        //var AlbumArtist = MyViewModel.AllLinks!.FirstOrDefault(x => x.ArtistId == artist.LocalDeviceId)!.AlbumId;
        //var album = MyViewModel.AllAlbums.FirstOrDefault(x => x.LocalDeviceId == AlbumArtist);
        //MyViewModel.GetAllArtistsAlbum(album: album, isFromSong: false);
    }
}