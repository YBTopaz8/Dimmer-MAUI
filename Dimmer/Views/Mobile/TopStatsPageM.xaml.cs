using Dimmer_MAUI.Views.Mobile.CustomViewsM;
using Microsoft.Maui.Storage;

namespace Dimmer_MAUI.Views.Mobile;

public partial class TopStatsPageM : UraniumContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public TopStatsPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        HomePageVM = homePageVM;

        btmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(btmSheet);

    }
    public HomePageVM HomePageVM { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.FullStatsPage;
        HomePageVM.ShowGeneralTopXSongsCommand.Execute(null);
    }

    private void ShowSongStats_Tapped(object sender, TappedEventArgs e)
    {
        var send = (FlexLayout)sender;
        var song = send.BindingContext as SingleSongStatistics;
        if (song is null)
        {
            return;
        }
        HomePageVM.ShowSingleSongStatsCommand.Execute(song.Song);

    }

    private async void ShareStatBtn_Clicked(object sender, EventArgs e)
    {
        ShareStatBtn.IsVisible = false;
        
        string shareCapture = "viewToShare.png";
        string filePath = Path.Combine(FileSystem.CacheDirectory, shareCapture);

        await SongStatView.CaptureCurrentViewAsync(OverViewSection, filePath);

        ShareStatBtn.IsVisible = true;
        
    }

    protected override bool OnBackButtonPressed()
    {
      
        return base.OnBackButtonPressed();
    }
}