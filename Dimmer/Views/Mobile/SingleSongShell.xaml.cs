

namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : ContentPage
{
	public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;

    }

    private TabItem InitialTab;
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
        HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);

        DeviceDisplay.Current.KeepScreenOn = true;
    }

    private void TabV_SelectedTabChanged(object sender, TabItem e)
    {
        
        if (e != null && e.Title == "Stats")
        {
            HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);
        }
        
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        DeviceDisplay.Current.KeepScreenOn = false;
    }

}