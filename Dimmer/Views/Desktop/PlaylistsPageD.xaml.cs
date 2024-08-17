namespace Dimmer_MAUI.Views.Desktop;

public partial class PlaylistsPageD : ContentPage
{
    public HomePageVM HomePageVM { get; }
    public PlaylistsPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        BindingContext = homePageVM;
        HomePageVM = homePageVM;
        MediaPlayBackCV.BindingContext = homePageVM;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();        
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.DisplayedSongsFromPlaylist.Clear();
        
    }
}