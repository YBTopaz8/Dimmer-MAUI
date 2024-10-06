namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
	public PlaylistsPageM(HomePageVM homePageVM)
    {
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
    }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.PlaylistsPage;
        HomePageVM.RefreshPlaylists();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();        
    }
   
  

}