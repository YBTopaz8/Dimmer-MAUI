
using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class ArtistsPageM : ContentPage
{
	public ArtistsPageM(HomePageVM homePageVM)
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

        if (MyViewModel.TemporarilyPickedSong is null)
        {
            return;
        }

        //AllAlbumsColView.SelectedItem = MyViewModel.SelectedAlbumOnArtistPage;

        MyViewModel.CurrentPage = PageEnum.AllArtistsPage;

        if (MyViewModel.MySelectedSong is null)
        {
            MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;
        }

        MyViewModel.GetAllArtistsAlbum(song: MyViewModel.MySelectedSong, isFromSong: true);
    }

    private void ResetSongs_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        MyViewModel.LoadArtistAlbumsAndSongs(MyViewModel.SelectedArtistOnArtistPage);
    }

    private void SingleAlbum_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        var send = (View)sender;

        var curSel = send.BindingContext as AlbumModelView;
        MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
    }

    private void SingleSongBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (View)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);

    }

    private void ShowArtistAlbums_Tapped(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        var curSel = send.BindingContext as AlbumModelView;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        send.PressedBackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        MyViewModel.GetAllSongsFromAlbumID(curSel!.LocalDeviceId);
    }
}