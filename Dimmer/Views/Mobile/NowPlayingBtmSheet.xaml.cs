using Dimmer_MAUI.Views.Mobile.CustomViewsM;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UraniumUI.Material.Attachments;

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

  
    //private void CoverFlowView_ItemSwiped(CardsView view, PanCardView.EventArgs.ItemSwipedEventArgs args)
    //{
    //    if (args.Direction == PanCardView.Enums.ItemSwipeDirection.Right)
    //    {
    //        HomePageVM.PlayPreviousSongCommand.Execute(null);
    //    }
    //    else
    //    {
    //        HomePageVM.PlayNextSongCommand.Execute(null);
    //    }
    //}

    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (homePageVM.DisplayedSongs.Count < 1)
        {
            return;
        }
        homePageVM.SelectedSongToOpenBtmSheet = homePageVM.TemporarilyPickedSong;
        await homePageVM.NavigateToArtistsPage(homePageVM.SelectedSongToOpenBtmSheet);
        this.IsPresented = false;

    }

    private async void PlayPauseImgBtn_Clicked(object sender, EventArgs e)
    {
        await homePageVM.PauseResumeSong();
    }

    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        homePageVM.SeekSongPosition();
    }
}