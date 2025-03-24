namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public PlaylistsPageM(PlaylistVM homePageVM)
    {
		InitializeComponent();
        MyViewModel = homePageVM;
        BindingContext = homePageVM;
        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);

    }
    public PlaylistVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPage = PageEnum.PlaylistsPage;
        MyViewModel.RefreshPlaylists();
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