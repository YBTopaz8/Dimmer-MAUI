namespace Dimmer_MAUI.Views.CustomViews;

public partial class PlayPauseView : ContentView
{
    public HomePageVM HomePageVM { get; }
    public PlayPauseView()
	{
		InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        this.BindingContext = HomePageVM;
    }

    private async void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.PauseSong();
        
    }

    private async void playImgBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.ResumeSong();
    }

}