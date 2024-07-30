namespace Dimmer_MAUI.Views.Mobile;

public partial class SinglePlaylistPageM : ContentPage
{
	public SinglePlaylistPageM(PlaylistsPageVM playlistsPageVM, HomePageVM homePageVM)
	{
		InitializeComponent();
        PlaylistsPageVM = playlistsPageVM;
        HomePageVM = homePageVM;
        BindingContext = playlistsPageVM;
    }

    public PlaylistsPageVM PlaylistsPageVM { get; }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.Title = PlaylistsPageVM.SelectedPlaylistPageTitle;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.SearchSongCommand.Execute(null);
    }
}