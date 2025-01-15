namespace Dimmer_MAUI.Views.Mobile;

public partial class EachPageNPFAB_Mobile : ContentView
{
	public EachPageNPFAB_Mobile()
	{
		InitializeComponent();
		this.HomePageVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;

        this.BindingContext = this.HomePageVM;
    }
    public HomePageVM HomePageVM { get; }

    private void DXButton_Clicked(object sender, EventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            HomePageVM.PauseSong();
        }
        else
        {
            HomePageVM.ResumeSong();
        }
    }

    private void NowPlayingBtn_Clicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        NowPlayingMiniControl.Commands.ToggleExpandState.Execute(null);
        

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        NowPlayingMiniControl.Commands.ToggleExpandState.Execute(null);
    }
}