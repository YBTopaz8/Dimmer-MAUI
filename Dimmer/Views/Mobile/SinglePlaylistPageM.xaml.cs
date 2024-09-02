namespace Dimmer_MAUI.Views.Mobile;

public partial class SinglePlaylistPageM : ContentPage
{
	public SinglePlaylistPageM(HomePageVM playlistsPageVM, HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = playlistsPageVM;
    }

    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.Title = HomePageVM.SelectedPlaylistPageTitle;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.SearchSongCommand.Execute(null);
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;

        var view = (FlexLayout)sender;
        var song = view.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }
}