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


    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ShowLyricsPage_Clicked(sender, e);
    }


    private void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            HomePageVM.PauseSong();
        }
        else
        {
            HomePageVM.ResumeSong();
        }

        return;
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
        if (HomePageVM.SynchronizedLyrics?.Count < 1 || HomePageVM.SynchronizedLyrics is null)
        {
            return;
        }
        SyncLyricsColView.ScrollTo(SyncLyricsColView.GetItemHandle(HomePageVM.SynchronizedLyrics!.IndexOf(HomePageVM.CurrentLyricPhrase!)), DXScrollToPosition.Start);
    }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        HomePageVM.CurrentPositionInSeconds = ProgressSlider.Value;
        HomePageVM.SeekSongPosition();
    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {
        if (!myPage.AllowDismiss)
        {
            this.Close();
            return;
        }
        this.Close();
    }

    private async void ShowMoreActionsContextMenuBtn_Clicked(object sender, EventArgs e)
    {
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        UpcomingSongsExp.Commands.ToggleExpandState.Execute(null);
        if (UpcomingSongsExp.IsExpanded)
        {
#if ANDROID
            myPageView.ScrollTo(0, 180, true);
#endif

            myPage.AllowDismiss = false;
        }
        else
        {
            myPage.AllowDismiss = true;
        }
        if (HomePageVM.SelectedSongToOpenBtmSheet is null)
        {
            return;
        }
        SongsColView.ScrollTo(SongsColView.FindItemHandle(HomePageVM.SelectedSongToOpenBtmSheet), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

    }
    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        if (e.Item is not SongModelView song)
        {
            return;
        }
        HomePageVM.SelectedSongToOpenBtmSheet = song;
        HomePageVM.PlaySong(song);
    }

    private void QuickPlaySong_Clicked(object sender, EventArgs e)
    {
        var send = sender as DXButton;
        var song = send.BindingContext  as SongModelView;

        HomePageVM.SelectedSongToOpenBtmSheet = song;
        HomePageVM.PlaySong(song);
    }
}