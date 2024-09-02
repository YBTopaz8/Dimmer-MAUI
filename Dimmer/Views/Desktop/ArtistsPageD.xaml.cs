namespace Dimmer_MAUI.Views.Desktop;

public partial class ArtistsPageD : ContentPage
{
    public ArtistsPageD(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
    }

    public HomePageVM HomePageVM { get; }

    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var t = (Border)sender;
        var album= t.BindingContext as AlbumModelView;
        HomePageVM.ShowSpecificArtistsSongsCommand.Execute(album.Id);
    }

    private void ShowArtistAlbumsSongs_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongsModelView;

        HomePageVM.PlaySongCommand.Execute(song);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.GetAllArtistsCommand.Execute(null);
    }

    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {
        var view = (MenuFlyoutItem)sender;
        SongsModelView song = view.BindingContext as SongsModelView;
        HomePageVM.SetSongCoverAsAlbumCover(song);
    }
}