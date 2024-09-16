namespace Dimmer_MAUI.Views.Mobile.CustomViewsM;

public partial class SongStatView : ContentView
{
	public SongStatView()
    {
        InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        BindingContext = HomePageVM;
    }
    public HomePageVM HomePageVM { get; }

    private void ShareStatBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void FavSong_Clicked(object sender, EventArgs e)
    {

    }
    
}