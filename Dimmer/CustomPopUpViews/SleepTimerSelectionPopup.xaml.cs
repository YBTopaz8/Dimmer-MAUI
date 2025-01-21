

namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SleepTimerSelectionPopup : Popup
{
    public SleepTimerSelectionPopup(HomePageVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
        MyViewModel = vm;
    }

    public HomePageVM MyViewModel { get; }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        MyViewModel.StartSleepTimerCommand.Execute(sleepSlider.Value);
        await this.CloseAsync();
    }
}