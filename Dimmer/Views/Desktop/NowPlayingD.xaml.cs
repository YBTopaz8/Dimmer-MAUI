namespace Dimmer_MAUI.Views.Desktop;

public partial class NowPlayingD : ContentPage
{
	public NowPlayingD(HomePageVM homePageVM)
    {
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;

        EditableSongsTagsV.homePageVM = homePageVM;
        EditableSongsTagsV.BindingContext = HomePageVM;
    }
    public HomePageVM HomePageVM { get; }
    
}