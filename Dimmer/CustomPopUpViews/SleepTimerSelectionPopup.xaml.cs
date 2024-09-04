

namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SleepTimerSelectionPopup : Popup
{
    public SleepTimerSelectionPopup(HomePageVM vm)
	{
		InitializeComponent();
		BindingContext = vm;
        Vm = vm;
    }

    public HomePageVM Vm { get; }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        Vm.StartSleepTimerCommand.Execute(sleepSlider.Value);
        await this.CloseAsync();
    }
}