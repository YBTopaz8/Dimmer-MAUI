#if ANDROID
using Android.Views;
using AndroidX.RecyclerView.Widget;
#endif
using DevExpress.Maui.Core;
using View = Microsoft.Maui.Controls.View;

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
        
    }


    private void NowPlayingBtmSheet_StateChanged(object? sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        
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
        MyViewModel.SeekSongPosition(currPosPer:ProgressSlider.Value);
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

            myPageView.ScrollTo(0, 180, true);


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
        SongsColView.ScrollTo(SongsColView.FindItemHandle(MyViewModel.MySelectedSong), DevExpress.Maui.Core.DXScrollToPosition.End);

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

    private void MyPage_StateChanged(object sender, ValueChangedEventArgs<DBtmState> e)
    {
        if (e.NewValue != BottomSheetState.Hidden)
        {
            DeviceDisplay.Current.KeepScreenOn = true;
        }
        else
        {
            DeviceDisplay.Current.KeepScreenOn = false;
        }
    }

    private void Chip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void SyncLyricsColView_Loaded(object sender, EventArgs e)
    {
#if ANDROID
        var nativeView = (global::Android.Views.View )SyncLyricsColView.Handler?.PlatformView;
        if (nativeView is not null)
        {
            if (nativeView is RecyclerView recyclerView)
            {
                // Disable item animator
                recyclerView.SetItemAnimator(null);

                // Remove all item decorations in reverse order to avoid shifting indexes.
                for (int i = recyclerView.ItemDecorationCount - 1; i >= 0; i--)
                {
                    recyclerView.RemoveItemDecorationAt(i);
                }

                // Remove background
                recyclerView.Background = null;
            }
            // If the native view is a ViewGroup, ensure transparency.
            if (nativeView is ViewGroup viewGroup)
            {
                viewGroup.SetBackgroundColor(Android.Graphics.Color.Transparent);
            }
        }
#endif
    }

    private void SyncLyricsColView_Unloaded(object sender, EventArgs e)
    {

    }

    private void SyncLyricsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            
            var bor = (View)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            MyViewModel.SeekSongPosition(lyr);
        }
    }
}