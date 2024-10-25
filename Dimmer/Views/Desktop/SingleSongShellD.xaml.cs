using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongShellD : ContentPage
{
    public SingleSongShellD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        //MediaPlayBackCW.BindingContext = homePageVM;

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
        //TabV.SelectedTab = TabV.Items[0];

        Task.Delay(3000);

        //FocusCaro.ItemsSource = HomePageVM.BackEndQ;
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
            vm.SwitchViewNowPlayingPageCommand.Execute(1);

        }


        if (e != null && e.Title == "Fetch Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);
        }

    }

    protected override void OnDisappearing()
    {
        //TabV.SelectedTab = TabV.Items[0];

        base.OnDisappearing();
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    //private void FocusCaro_Swiped(object sender, Utilities.OtherUtils.CustomControl.SwipeCardsView.Core.SwipedCardEventArgs e)
    //{
        
    //    var ee = FocusCaro.TopItem as SongsModelView;
    //    Debug.WriteLine(ee.Title + " tess");
    //    var send = (View)sender;
    //    var song = send.BindingContext as SongsModelView;
    //    Debug.WriteLine(song.Title);
    //}
}