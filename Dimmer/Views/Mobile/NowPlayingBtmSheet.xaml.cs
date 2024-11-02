using Dimmer_MAUI.Views.Mobile.CustomViewsM;
using System.Diagnostics;
using UraniumUI.Extensions;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmSheet : Border
{
	public NowPlayingBtmSheet()
	{
        
		InitializeComponent();
        homePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        this.BindingContext = homePageVM;

        //Shell.SetTabBarIsVisible(this, false);
      
    }



    HomePageVM homePageVM { get; set; }

    private async void ShowLyricsPage_Clicked(object sender, EventArgs e)
    {
        await Task.Delay(800);
        //this.IsPresented = false;
        homePageVM.SelectedSongToOpenBtmSheet = homePageVM.TemporarilyPickedSong;
        
        await homePageVM.NavToNowPlayingPage();

    }


    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (homePageVM.DisplayedSongs.Count < 1)
        {
            return;
        }
        homePageVM.SelectedSongToOpenBtmSheet = homePageVM.TemporarilyPickedSong;
        await homePageVM.NavigateToArtistsPage(0);
        //this.IsPresented = false;

    }


    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        homePageVM.SeekSongPosition();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ShowLyricsPage_Clicked(sender, e);
    }

    private async void PlayPauseBtn_Tapped(object sender, TappedEventArgs e)
    {
        if (homePageVM.IsPlaying)
        {
            await homePageVM.PauseSong();
        }
        else
        {
            await homePageVM.ResumeSong();
        }
    }

}