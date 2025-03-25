namespace Dimmer_MAUI.Views.Mobile;

public partial class SingleSongShell : ContentPage
{
    NowPlayingBtmSheet? btmSheet { get; set; }
    public SingleSongShell(HomePageVM homePageVM)
	{
		InitializeComponent();
        MyViewModel = homePageVM;
        BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>());
        //dailyDateFilter.Date = DateTime.Now;
     
    }


    public HomePageVM MyViewModel { get; }
   
    /*
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;
        await MyViewModel.AfterSingleSongShellAppeared();
        
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        DeviceDisplay.Current.KeepScreenOn = false;
        
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
                if (MyViewModel.AllSyncLyrics is not null)
                {
                    MyViewModel.AllSyncLyrics = new();
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
            MyViewModel.ShowSingleSongStatsCommand.Execute(MyViewModel.MySelectedSong);
        }
    }
    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MyViewModel.MySelectedSong != MyViewModel.TemporarilyPickedSong)
        {
            return;
        }
        try
        {
            if (MyViewModel.SynchronizedLyrics?.Count < 1 || MyViewModel.SynchronizedLyrics is null)
            {
                return;
            }
            if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
            {
                LyricsColView.ScrollTo(LyricsColView.GetItemHandle(MyViewModel.SynchronizedLyrics!.IndexOf(MyViewModel.CurrentLyricPhrase!)), DXScrollToPosition.Start);
            }

        }
        catch (Exception)
        {
            return;
        }
    }
    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            var bor = (View)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            MyViewModel.SeekSongPosition(lyr);
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
        await MyViewModel.FetchLyrics(true);
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
        var thisContent = send.BindingContext as Dimmer_MAUI.Utilities.Models.Content;
        if (title == "Synced Lyrics")
        {

            await MyViewModel.SaveLyricToFile(thisContent, false);
        }
        else
        if (title == "Plain Lyrics")
        {

            await MyViewModel.SaveLyricToFile(thisContent, true);
        }
    }
    private void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        NowPlayingBtmSheet.Show();
    }
    private void RevealNPBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        NowPlayingBtmSheet.Show();
    }
    private void DXButton_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSong();
        }
        else
        {
            MyViewModel.ResumeSong();
        }
    }
    */
}