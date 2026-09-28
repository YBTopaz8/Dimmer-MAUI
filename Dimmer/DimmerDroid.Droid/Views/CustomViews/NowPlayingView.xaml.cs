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



    private void myPage_Unloaded(object sender, EventArgs e)
    {
        _debounceTimer?.Dispose();
    }

    private async void CoverImgInNowPlayingPage_Tapped(object sender, TappedEventArgs e)
    {



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
       
    }
    private void ListPerfectPairings_Loaded(object sender, EventArgs e)
    {
       
    }

    CancellationTokenSource? cancellationTokenSource; 
    private async void ListPerfectPairings_Tap(object sender, CollectionViewGestureEventArgs e)
    {
      
    }

    private async void AudioControlCenterSheet_StateChanged(object sender, ValueChangedEventArgs<BottomSheetState> e)
    {
        switch (e.NewValue)
        {
            case BottomSheetState.FullExpanded:
            case BottomSheetState.HalfExpanded:
                await MyViewModel.AudioService.InitializeEngineAsync();
                break;
            case BottomSheetState.Hidden:
                break;
            default:
                break;
        }
    }



}