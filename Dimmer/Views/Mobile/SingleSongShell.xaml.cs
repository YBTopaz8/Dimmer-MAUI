using Plugin.Maui.SegmentedControl;

namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : ContentPage
{
	public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        SegControl.ValueChanged += SegControl_ValueChanged;
    }

    private void SegControl_ValueChanged(object? sender, Plugin.Maui.SegmentedControl.ValueChangedEventArgs e)
    {
        int currentView = ((SegmentedControl)sender).SelectedSegment;
        HomePageVM.SwitchViewNowPlayingPageCommand.Execute(currentView);
        //throw new NotImplementedException();
    }

    public HomePageVM HomePageVM { get; }
}