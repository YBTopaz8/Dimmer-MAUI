using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;
using Dimmer_MAUI.Views.Mobile.CustomViewsM;
using System.Diagnostics;
using UraniumUI.Extensions;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmSheet : DevExpress.Maui.Controls.BottomSheet
{
	public NowPlayingBtmSheet()
	{
        
		InitializeComponent();
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        this.BindingContext = HomePageVM;

        //Shell.SetTabBarIsVisible(this, false);
        AllowedState = BottomSheetAllowedState.FullExpanded;

        //this.StateChanged += NowPlayingBtmSheet_StateChanged;
    }

    private void NowPlayingBtmSheet_StateChanged(object? sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        //if (e.NewValue == BottomSheetState.FullExpanded)
        //{
        //    if(NPSlider.ItemsSource is null)
        //    {
        //        NPSlider.ItemsSource = HomePageVM.SongsMgtService.AllSongs;
        //    }
        //}
    }

    HomePageVM HomePageVM { get; set; }

    private async void ShowLyricsPage_Clicked(object sender, EventArgs e)
    {
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        
        await HomePageVM.NavToNowPlayingPage();
        await Task.Delay(800);
        this.State = BottomSheetState.Hidden;

    }


    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.DisplayedSongs.Count < 1)
        {
            return;
        }
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong;
        await HomePageVM.NavigateToArtistsPage(0);
        this.State = BottomSheetState.Hidden;

    }


    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        HomePageVM.SeekSongPosition();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ShowLyricsPage_Clicked(sender, e);
    }

    private async void PlayPauseBtn_Tapped(object sender, TappedEventArgs e)
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

    void CloseSheet()
    {

    }
}