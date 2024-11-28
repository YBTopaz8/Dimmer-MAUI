using DevExpress.Maui.Controls;
using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmSheet : DevExpress.Maui.Controls.BottomSheet
{
	public NowPlayingBtmSheet()
	{
        
		InitializeComponent();
        HomePageVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
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
        
        await HomePageVM.NavToSingleSongShell();
        await Task.Delay(500);
        this.State = BottomSheetState.Hidden;

    }

    
    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.SongsMgtService.AllSongs.Count < 1)
        {
            return;
        }
        HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong!;
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


    private async void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            await HomePageVM.PauseSong();
        }
        else
        {
            await HomePageVM.ResumeSong();
        }
    
        return;
    }

    private void ProgressSlider_ValueChanged(object sender, EventArgs e)
    {
        HomePageVM.SeekSongPosition();
    }

    private async void NavToSingleSongShell_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.NavToSingleSongShell();
    }

    private void lyricsCover_Clicked(object sender, EventArgs e)
    {
        if (HomePageVM.SynchronizedLyrics?.Count < 1 || HomePageVM.SynchronizedLyrics is null)
        {
            return;
        }
        //NormalNowPlayingUI.IsExpanded = !NormalNowPlayingUI.IsExpanded;
        //LyricsUI.IsExpanded = !LyricsUI.IsExpanded;
    }

    private void SyncLyricsColView_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    {
        if (HomePageVM.SynchronizedLyrics?.Count <1 || HomePageVM.SynchronizedLyrics is null)
        {
            return;
        }
        SyncLyricsColView.ScrollTo(SyncLyricsColView.GetItemHandle(HomePageVM.SynchronizedLyrics!.IndexOf(HomePageVM.CurrentLyricPhrase!)), DXScrollToPosition.Start);
    }
}