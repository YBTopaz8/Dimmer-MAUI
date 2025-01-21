using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingBtmSheet : DevExpress.Maui.Controls.BottomSheet
{
    public NowPlayingBtmSheet()
    {

        InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        this.BindingContext = MyViewModel;

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
        //        NPSlider.ItemsSource = MyViewModel.SongsMgtService.AllSongs;
        //    }
        //}
    }

    HomePageVM MyViewModel { get; set; }

    private async void ShowLyricsPage_Clicked(object sender, EventArgs e)
    {
        MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;

        await MyViewModel.NavToSingleSongShell();
        await Task.Delay(500);
        this.State = BottomSheetState.Hidden;

    }


    private async void ShowSongAlbum_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.SongsMgtService.AllSongs.Count < 1)
        {
            return;
        }
        MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong!;
        await MyViewModel.NavigateToArtistsPage(0);
        this.State = BottomSheetState.Hidden;

    }


    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        ShowLyricsPage_Clicked(sender, e);
    }


    private void NowPlayingBtn_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSong();
        }
        else
        {
            MyViewModel.ResumeSong();
        }

        return;
    }

    private async void NavToSingleSongShell_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.NavToSingleSongShell();
    }

    private void lyricsCover_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.SynchronizedLyrics?.Count < 1 || MyViewModel.SynchronizedLyrics is null)
        {
            return;
        }
        //NormalNowPlayingUI.IsExpanded = !NormalNowPlayingUI.IsExpanded;
        //LyricsUI.IsExpanded = !LyricsUI.IsExpanded;
    }

    private void SyncLyricsColView_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    {
        if (MyViewModel.SynchronizedLyrics?.Count < 1 || MyViewModel.SynchronizedLyrics is null)
        {
            return;
        }
        SyncLyricsColView.ScrollTo(SyncLyricsColView.GetItemHandle(MyViewModel.SynchronizedLyrics!.IndexOf(MyViewModel.CurrentLyricPhrase!)), DXScrollToPosition.Start);
    }

    private void ProgressSlider_TapReleased(object sender, DXTapEventArgs e)
    {
        MyViewModel.CurrentPositionInSeconds = ProgressSlider.Value;
        MyViewModel.SeekSongPosition();
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

            //myPage.AllowDismiss = false;
        }
        else
        {
            myPage.AllowDismiss = true;
        }
        if (MyViewModel.MySelectedSong is null)
        {
            return;
        }
        SongsColView.ScrollTo(SongsColView.FindItemHandle(MyViewModel.MySelectedSong), DevExpress.Maui.Core.DXScrollToPosition.MakeVisible);

    }
    private void SongsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        if (e.Item is not SongModelView song)
        {
            return;
        }
        MyViewModel.MySelectedSong = song;
        MyViewModel.PlaySong(song);
    }

    private void QuickPlaySong_Clicked(object sender, EventArgs e)
    {
        var send = sender as DXButton;
        var song = send.BindingContext  as SongModelView;

        MyViewModel.MySelectedSong = song;
        MyViewModel.PlaySong(song);
    }

    private void ToggleRepeat_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleRepeatModeCommand.Execute(true);

    }
}