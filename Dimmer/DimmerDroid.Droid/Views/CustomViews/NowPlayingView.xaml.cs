using AndroidX.Navigation;
using CommunityToolkit.Maui.Alerts;
using DevExpress.Maui.CollectionView;
using Dimmer.Charts;
using Dimmer.ViewModel.StatsVMs;
using MongoDB.Bson;

namespace Dimmer.Views.CustomViews;

public partial class NowPlayingView : ContentView
{
	public NowPlayingView()
	{
		InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()!;
        StatsViewModel = IPlatformApplication.Current!.Services.GetService<SongStatsViewModel>()!;
        BindingContext = MyViewModel;
	}
    BaseViewModelAnd MyViewModel { get;}
    SongStatsViewModel StatsViewModel { get;}

    SongModelView? songForLyrics;
    private async void LyricsChip_Tap(object sender, HandledEventArgs e)
    {
        if(MyViewModel.CurrentPlayingSongView.HasSyncedLyrics)
        {

            NowPlayingViewExpander.SetIsExpanded(false, true);
            SyncLyricsView.SetIsExpanded(true, true);
            return;
        }
        if(songForLyrics is null)
        {
            songForLyrics = MyViewModel.CurrentPlayingSongView;
        }
        SongLyricsDownloadPopup popup = new SongLyricsDownloadPopup(MyViewModel, MyViewModel.CurrentPlayingSongView);

        await popup.ShowAsync();
    }

  



    private void PlaybackChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void SongTitleLabel_Loaded(object sender, EventArgs e)
    {

    }

    private void NowPlayingHighlightBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        
        NowPlayingViewExpander.SetIsExpanded(true, true);
        SyncLyricsView.SetIsExpanded(false, true);
    }
    public event EventHandler? SwitchToPlayBackQueue;
   

   
    private async void PlaySongInQueue_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await MyViewModel.PlaySongWithActionAsync(song, PlaybackAction.JumpInQueue);

    }

    private void PlaybackChip_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        NowPlayingHighlightBtn_TapPressed(sender, e);
    }

    private void PlaybackChip_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        NowPlayingHighlightBtn_TapPressed(sender, e);

    }

  
    private void DXCollectionView_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {

    }

    private void PlaybackQueueCV_Scrolled(object sender, DevExpress.Maui.CollectionView.DXCollectionViewScrolledEventArgs e)
    {
        //get scroll direction,
        double scrollDirection = e.Delta;
        double ViewportSize = e.ViewportSize;
        int FirstVisibleItemIndex = e.FirstVisibleItemIndex;
        int FirstVisibleItemHandle = e.FirstVisibleItemHandle;
        int LastVisibleItemIndex = e.LastVisibleItemIndex;
        int LastVisibleItemHandle = e.LastVisibleItemHandle;
        double Offset = e.Offset;
        double ExtentSize = e.ExtentSize;
    }

    private async void PlaySongInQueue_Tap(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await MyViewModel.PlaySongWithActionAsync(song, PlaybackAction.JumpInQueue);

    }




    
    private void CurrentLyricLineTapGestRec_Tapped(object sender, TappedEventArgs e)
    {



    }


    private void BackBtn_Tap(object sender, DXTapEventArgs e)
    {
       

    }



    private void LyricsChip_Tap_1(object sender, HandledEventArgs e)
    {

    }


    private bool _isUserDragging = false;
    private double _pendingSeekValue;
    private Timer _debounceTimer;
    private void OnSliderDragStarted(object sender, EventArgs e)
    {
        _isUserDragging = true;
        _pendingSeekValue = TrackProgressSlider.Value;

        // Optional: Show preview label
        //PreviewTimeLabel.IsVisible = true;
        //UpdatePreviewLabel(_pendingSeekValue);
    }

    private void OnSliderDragCompleted(object sender, EventArgs e)
    {
        _isUserDragging = false;
        _pendingSeekValue = TrackProgressSlider.Value;

        // Debounce to prevent rapid seeks
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(ExecuteSeek, null, 150, Timeout.Infinite);
    }
    private void ExecuteSeek(object? state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Critical: Don't let timer cause multiple seeks
            var value = _pendingSeekValue;
            _pendingSeekValue = -1;

            // Update ViewModel's property to keep binding in sync
            MyViewModel.CurrentTrackPositionSeconds = value;
            MyViewModel.SeekTrackPosition(value);

            //PreviewTimeLabel.IsVisible = false;
        });
    }

    private void myPage_Unloaded(object sender, EventArgs e)
    {
        _debounceTimer?.Dispose();
    }

    private async void CoverImgInNowPlayingPage_Tapped(object sender, TappedEventArgs e)
    {

        await Shell.Current.GoToAsync("..");

    }

    private void AllLyricsCV_SelectionChanged(object sender, CollectionViewSelectionChangedEventArgs e)
    {
        var selItemHandle = AllLyricsCV.FindItemHandle(AllLyricsCV.SelectedItem);
        AllLyricsCV.ScrollTo(selItemHandle,DXScrollToPosition.Start);
    }

    private void CurrentLyricLine_Clicked(object sender, EventArgs e)
    {
        NowPlayingViewExpander.SetIsExpanded(false, true);
        SyncLyricsView.SetIsExpanded(true, true);
    }

    private void AllLyricsCV_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        var lineObj = e.Item as LyricPhraseModelView;
        var lineHandle = e.ItemHandle;

        AllLyricsCV.ScrollTo(lineHandle,DXScrollToPosition.Start);
    }

    private async void LyricsChip_LongPress(object sender, HandledEventArgs e)
    {
        SongLyricsDownloadPopup popup = new SongLyricsDownloadPopup(MyViewModel, MyViewModel.CurrentPlayingSongView);

        await popup.ShowAsync();
    }

    private void SwipedUp_Swiped(object sender, SwipedEventArgs e)
    {
        
    }

    private void AudioMgtButton_TapPressed(object sender, DXTapEventArgs e)
    {
//do a btm sheet having a tabview tab 1 being app volume management, tab 2 being app speaker choice
    }


    private void ShowFrequentlyPlayedExpanderChkBtn_CheckedChanging(object sender, ValueChangingEventArgs<bool> e)
    {
        if (e.NewValue)
        {
            StatsViewModel.LoadSong(MyViewModel.CurrentPlayingSongView.Id);
            return;
        }
        if (FrequentlyPlayedExpander.IsExpanded)
        {
            StatsViewModel.LoadSong(MyViewModel.CurrentPlayingSongView.Id);
        }
    }
    private void ListPerfectPairings_Loaded(object sender, EventArgs e)
    {
        if(ListPerfectPairings.IsLoaded)
        {
            StatsViewModel?.WhenPropertyChanged(nameof(StatsViewModel.ListPerfectPairings), v => StatsViewModel?.ListPerfectPairings)
            .Subscribe(insight =>
            {
                ListPerfectPairings.ItemsSource = insight;
            });
        } 
    }

    CancellationTokenSource? cancellationTokenSource; 
    private async void ListPerfectPairings_Tap(object sender, CollectionViewGestureEventArgs e)
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new();
        var tappedItemHandle = e.ItemHandle;
        var tappedItem = ListPerfectPairings.GetItem(tappedItemHandle) as SongPairing;

        if (tappedItem != null && tappedItem.songId != null && tappedItem.isPresentOnDevice)
        {
            MyViewModel.SelectedSong = MyViewModel.RealmFactory.GetRealmInstance().Find<SongModel>(tappedItem.songId).ToSongModelView();
            this.SingleSongStatView.State = BottomSheetState.HalfExpanded;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        else
        {

            var songNotOnDeviceToast = new Snackbar();
            var songNotOnDeviceToastText = "Song Not On Device";

            CommunityToolkit.Maui.Alerts.Toast msgToast = new CommunityToolkit.Maui.Alerts.Toast() { Text = songNotOnDeviceToastText, Duration = CommunityToolkit.Maui.Core.ToastDuration.Short, TextSize=21};
            msgToast?.Show(cancellationTokenSource.Token);

            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
    }


    //private void AllLyricsColView_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    //{


    //    var currentList = e.AddedItems as IReadOnlyList<object>;
    //    var current = currentList?.FirstOrDefault() as Dimmer.Data.ModelView.LyricPhraseModelView;
    //    if (current != null)
    //    {
    //        var pastList = e.RemovedItems as IReadOnlyList<object>;
    //        if (pastList.Count > 0 && pastList?[0] is Dimmer.Data.ModelView.LyricPhraseModelView past)
    //        {
    //            past?.NowPlayingLyricsFontSize = 25;
    //            past?.HighlightColor = Microsoft.Maui.Graphics.Colors.White;
    //            past?.IsHighlighted = false;
    //        }
    //        current?.NowPlayingLyricsFontSize = 40;
    //        current?.IsHighlighted = true;
    //        current?.HighlightColor = Microsoft.Maui.Graphics.Colors.SlateBlue;

    //        var itemHandle = AllLyricsColView.FindItemHandle(current);
    //        RxSchedulers.UI.ScheduleTo(() =>
    //        {
    //            AllLyricsColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
    //        });

    //    }
    //}

    //private void AllLyricsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    //{

    //    LyricPhraseModelView? lyricTapped = e.Item as LyricPhraseModelView;
    //    var lyricTappedHandle = e.ItemHandle;
    //    if (lyricTapped is null)
    //        return;
    //    var timeInSec = TimeSpan.FromMilliseconds(lyricTapped.TimestampStart).Seconds;
    //    MyViewModel.SeekTrackPosition(timeInSec);
    //    AllLyricsColView.ScrollTo(lyricTappedHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);

    //}

}