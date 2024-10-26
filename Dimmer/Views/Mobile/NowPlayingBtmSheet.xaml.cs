using Dimmer_MAUI.Views.Mobile.CustomViewsM;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmSheet : NowPlayingBtmSheetContainer
{
	public NowPlayingBtmSheet()
	{
        
		InitializeComponent();		
		homePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
		this.BindingContext = homePageVM;
        
        //Shell.SetTabBarIsVisible(this, false);
        //this.PropertyChanged += NowPlayingBtmSheet_PropertyChanged;
        
    }

    

    HomePageVM homePageVM { get; set; }

    private async void ShowLyricsPage_Clicked(object sender, EventArgs e)
    {
        homePageVM.SelectedSongToOpenBtmSheet = homePageVM.TemporarilyPickedSong;
        homePageVM.NavToNowPlayingPageCommand.Execute(null);
        await Task.Delay(500);
        this.IsPresented = false;
    }


    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (homePageVM.DisplayedSongs.Count < 1)
        {
            return;
        }
        homePageVM.SelectedSongToOpenBtmSheet = homePageVM.TemporarilyPickedSong;
        await homePageVM.NavigateToArtistsPage(0);
        this.IsPresented = false;

    }

    private async void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        await homePageVM.PauseSong();
        
    }

    private async void playImgBtn_Clicked(object sender, EventArgs e)
    {
        await homePageVM.ResumeSong();
        
    }

    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        homePageVM.SeekSongPosition();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ShowLyricsPage_Clicked(sender, e);
    }
}