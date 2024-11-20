namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public PlaylistsPageM(HomePageVM homePageVM)
    {
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);

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

    protected override bool OnBackButtonPressed()
    {
        
        return base.OnBackButtonPressed();
    }

}