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
        TabV.SelectedTab = TabV.Items[0];
        if (HomePageVM.CurrentViewIndex == 1)
        {
            TabV.SelectedTab = TabV.Items[1];
        }

        Shell.SetTabBarIsVisible(this, false);
        Shell.SetNavBarIsVisible(this, false);
    }

    private void TabV_SelectedTabChanged(object sender, TabItem e)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        if (e!= null &&  e.Title == "Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(0);
        }

        if (e != null && e.Title == "Edit Tags")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(1);
        }

        if (e!= null && e.Title == "Stats")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);
        }
        
        
        if (e!= null && e.Title == "Fetch Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);
        }
        
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TabV.SelectedTab = TabV.Items[0];
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

}