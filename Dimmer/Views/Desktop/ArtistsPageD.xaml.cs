

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
            AllArtistsColView.ScrollTo(AllArtistsColView.SelectedItem, null, ScrollToPosition.Center, false);
        }
    }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;
        AllArtistsColView.SelectedItem = HomePageVM.SelectedArtistOnArtistPage;
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
        base.OnAppearing();
    }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {

        base.OnNavigatedTo(args);
        
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
        var view = (MenuFlyoutItem)sender;
        SongsModelView song = view.BindingContext as SongsModelView;
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
}