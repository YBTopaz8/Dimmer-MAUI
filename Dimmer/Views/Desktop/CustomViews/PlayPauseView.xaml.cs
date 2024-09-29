namespace Dimmer_MAUI.Views.CustomViews;

public partial class PlayPauseView : ContentView
{
    public HomePageVM HomePageVM { get; }
    public PlayPauseView()
	{
		InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
    }


    private async void PlayPauseImgBtn_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.PauseResumeSong();
    }

}