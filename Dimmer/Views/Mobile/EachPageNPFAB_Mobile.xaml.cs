namespace Dimmer_MAUI.Views.Mobile;

public partial class EachPageNPFAB_Mobile : ContentView
{
	public EachPageNPFAB_Mobile()
	{
		InitializeComponent();
		this.HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();

        this.BindingContext = this.HomePageVM;
    }
    public HomePageVM HomePageVM { get; }

    private async void DXButton_Clicked(object sender, EventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            await HomePageVM.PauseSong();
        }
        else
        {
            await HomePageVM.ResumeSong();
        }
    }

    private void NowPlayingBtn_Clicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        
    }
}