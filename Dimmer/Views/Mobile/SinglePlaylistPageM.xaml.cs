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
}