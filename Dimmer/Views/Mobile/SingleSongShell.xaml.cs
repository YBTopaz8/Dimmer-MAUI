namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : UraniumContentPage
{
    NowPlayingBtmSheet? btmSheet { get; set; }
    public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        this.Attachments.Add(IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>());

    }

    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
        HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);

        DeviceDisplay.Current.KeepScreenOn = true;
        
        if (HomePageVM.AllSyncLyrics?.Length > 0)
        {
            Array.Clear(HomePageVM.AllSyncLyrics);
        }

        TabV.SelectedIndex = 0;
        if (HomePageVM.CurrentViewIndex == 1)
        {
            TabV.SelectedIndex = 1;
        }

        Shell.SetTabBarIsVisible(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }


    protected override void OnDisappearing()
    {
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, true);
        base.OnDisappearing();
        
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    protected override bool OnBackButtonPressed()
    {
        
        if(btmSheet is not null)
        {
            if (btmSheet.IsPresented)
            {
                btmSheet.IsPresented = false;
                return true;
            }
        }        
        return base.OnBackButtonPressed();
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }
}