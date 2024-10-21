namespace Dimmer_MAUI.Views.Desktop;

public partial class NowPlayingD : ContentPage
{
    public NowPlayingD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.NowPlayingPage;
        if (HomePageVM.AllSyncLyrics is not null)
        {
            Array.Clear(HomePageVM.AllSyncLyrics);
        }
        TabV.SelectedTab = TabV.Items[0];
    }

    private void TabV_SelectedTabChanged(object sender, TabItem e)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        if (e != null && e.Title == "Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(0);
        }

        if (e != null && e.Title == "Stats")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);

        }


        if (e != null && e.Title == "Fetch Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);
        }

    }

    protected override void OnDisappearing()
    {
        TabV.SelectedTab = TabV.Items[0];

        base.OnDisappearing();
    }

}