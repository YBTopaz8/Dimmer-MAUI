
using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class ArtistsPageM : ContentPage
{
	public ArtistsPageM(HomePageVM homePageVM)
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

        //AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;

        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;

        if (HomePageVM.SelectedSongToOpenBtmSheet is null)
        {
            HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        }

        HomePageVM.GetAllArtistsAlbum(song: HomePageVM.SelectedSongToOpenBtmSheet, isFromSong: true);
    }

    private void ResetSongs_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        HomePageVM.GetAllArtistAlbumFromArtist(HomePageVM.SelectedArtistOnArtistPage);
    }

    private async void SingleAlbum_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        await HomePageVM.GetSongsFromAlbumId(curSel!.LocalDeviceId);
    }

    private void SingleSongBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (View)sender;
        var song = s.BindingContext as SongModelView;
        HomePageVM.PlaySongCommand.Execute(song);

    }

    private async void ShowArtistAlbums_Tapped(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var curSel = send.BindingContext as AlbumModelView;



        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        send.PressedBackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        await HomePageVM.GetSongsFromAlbumId(curSel!.LocalDeviceId);
    }
}