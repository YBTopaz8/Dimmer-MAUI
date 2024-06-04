namespace Dimmer_MAUI.Views.Desktop;

public partial class HomePageD : ContentPage
{
	public HomePageD(IPlayBackService songsManagerService, HomePageVM homePageVM)
	{
		InitializeComponent();
        SongsManagerService = songsManagerService;
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
    }

    public IPlayBackService SongsManagerService { get; }
    public HomePageVM HomePageVM { get; }

    private void PauseBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void PlayBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void StopBtn_Clicked(object sender, EventArgs e)
    {

    }

    
}