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
        //this.Attachments.Add(IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>());
        dailyDateFilter.Date = DateTime.Now;
     
    }


    public HomePageVM HomePageVM { get; }
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;
        HomePageVM.AfterSingleSongShellAppeared();
        
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        DeviceDisplay.Current.KeepScreenOn = false;
        HomePageVM.IsViewingDifferentSong = false;
    }

    protected override bool OnBackButtonPressed()
    {           
        return base.OnBackButtonPressed();
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                break;
            case 1:
                emptyV.IsVisible = false;
                if (HomePageVM.AllSyncLyrics is not null)
                {
                    HomePageVM.AllSyncLyrics = Array.Empty<Content>();
                }
                break;
            case 2:
                break;
            default:
                Lookgif.IsVisible = false;
                break;
        }
        if (e.NewIndex == 2)
        {
            HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);
        }
    }


    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            
            if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
            {                
                LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
            }
            

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            var bor = (View)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            HomePageVM.SeekSongPosition(lyr);
        }
    }

    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {
        emptyV.IsVisible = true;
        await Task.WhenAll(Lookgif.AnimateFadeInFront(), fetchFailed.AnimateFadeOutBack(),
NoLyricsFoundMsg.AnimateFadeOutBack());

        Lookgif.HeightRequest = 100;
        Lookgif.WidthRequest = 100;
        Lookgif.IsAnimationPlaying = true;
        await HomePageVM.FetchLyrics(true);
        Lookgif.HeightRequest = 0;
        Lookgif.WidthRequest = 0;
        await Task.WhenAll(Lookgif.AnimateFadeOutBack(), fetchFailed.AnimateFadeInFront(),
NoLyricsFoundMsg.AnimateFadeInFront());
        fetchFailed.IsAnimationPlaying = true;
        Lookgif.IsAnimationPlaying = false;
        await Task.Delay(3000);
        fetchFailed.IsAnimationPlaying = false;
        fetchFailed.IsVisible = false;
        emptyV.IsVisible = false;
    }


    private async void ViewLyricsBtn_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var title = send.Text;
        var thisContent = send.BindingContext as Dimmer_MAUI.Utilities.Services.Models.Content;
        if (title == "Synced Lyrics")
        {

            await HomePageVM.ShowSingleLyricsPreviewPopup(thisContent, false);
        }
        else
        if (title == "Plain Lyrics")
        {

            await HomePageVM.ShowSingleLyricsPreviewPopup(thisContent, true);
        }
    }

    private async void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    {
        var newSelection = e.NewIndex;
        switch (newSelection)
        {
            case 0:
                await Task.WhenAll(
                DailyStats.AnimateFadeInFront(),
                WeeklyStats.AnimateFadeOutBack(),


                MonthlyStats.AnimateFadeOutBack(),
                YearlyStats.AnimateFadeOutBack());
                HomePageVM.LoadDailyStats(HomePageVM.SelectedSongToOpenBtmSheet);
                break;
            case 1:
                await Task.WhenAll(
               DailyStats.AnimateFadeOutBack(),
               WeeklyStats.AnimateFadeInFront(),   // Fade in WeeklyStats
               MonthlyStats.AnimateFadeOutBack(),
               YearlyStats.AnimateFadeOutBack()
           );
                HomePageVM.LoadWeeklyStats(HomePageVM.SelectedSongToOpenBtmSheet);

                break;
            case 2:
                await Task.WhenAll(
                DailyStats.AnimateFadeOutBack(),
                WeeklyStats.AnimateFadeOutBack(),
                MonthlyStats.AnimateFadeInFront(),  // Fade in MonthlyStats
                YearlyStats.AnimateFadeOutBack()
            );
                HomePageVM.LoadMonthlyStats(HomePageVM.SelectedSongToOpenBtmSheet);
                break;
            case 3:
                await Task.WhenAll(
                DailyStats.AnimateFadeOutBack(),
                WeeklyStats.AnimateFadeOutBack(),
                MonthlyStats.AnimateFadeOutBack(),
                YearlyStats.AnimateFadeInFront()    // Fade in YearlyStats
            );
                HomePageVM.LoadYearlyStats(HomePageVM.SelectedSongToOpenBtmSheet);
                break;
            default:
                break;
        }
    }

    private async void ShowHideChart_CheckChanged(object sender, EventArgs e)
    {
        var send = (UraniumUI.Material.Controls.CheckBox)sender;

        var checkState = send.IsChecked;
        switch (checkState)
        {
            case true:
                await Task.WhenAll(DailyStats.AnimateFadeOutBack(),
                dailyStatChart.AnimateFadeInFront());
                break;
            case false:
                await Task.WhenAll(DailyStats.AnimateFadeInFront(),
                dailyStatChart.AnimateFadeOutBack());
                break;
            default:
                break;
        }

    }

    private void NowPlayingBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

    }

    private void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        NowPlayingBtmSheet.Show();
    }

    private void RevealNPBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        NowPlayingBtmSheet.Show();
    }

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
}