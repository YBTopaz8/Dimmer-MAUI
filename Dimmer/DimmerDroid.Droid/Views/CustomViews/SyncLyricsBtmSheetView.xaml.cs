namespace Dimmer.Views.CustomViews;

public partial class SyncLyricsBtmSheetView : BottomSheet
{
	public SyncLyricsBtmSheetView()
	{
		InitializeComponent();
	}
    BaseViewModelAnd MyViewModel { get; }
    private async void CoverImgInNowPlayingPage_Tapped(object sender, TappedEventArgs e)
    {

        await this.CloseAsync();

    }

    private void AllLyricsColView_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    {


        var currentList = e.AddedItems as IReadOnlyList<object>;
        var current = currentList?.FirstOrDefault() as Dimmer.Data.ModelView.LyricPhraseModelView;
        if (current != null)
        {
            var pastList = e.RemovedItems as IReadOnlyList<object>;
            if (pastList.Count > 0 && pastList?[0] is Dimmer.Data.ModelView.LyricPhraseModelView past)
            {
                past?.NowPlayingLyricsFontSize = 25;
                past?.HighlightColor = Microsoft.Maui.Graphics.Colors.White;
                past?.IsHighlighted = false;
            }
            current?.NowPlayingLyricsFontSize = 40;
            current?.IsHighlighted = true;
            current?.HighlightColor = Microsoft.Maui.Graphics.Colors.SlateBlue;

            var itemHandle = AllLyricsColView.FindItemHandle(current);
            RxSchedulers.UI.ScheduleTo(() =>
            {
                AllLyricsColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);
            });

        }
    }

    private void AllLyricsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {

        LyricPhraseModelView? lyricTapped = e.Item as LyricPhraseModelView;
        var lyricTappedHandle = e.ItemHandle;
        if (lyricTapped is null)
            return;
        var timeInSec = TimeSpan.FromMilliseconds(lyricTapped.EndTimeMs).Seconds;
        MyViewModel.SeekTrackPosition(timeInSec);
        AllLyricsColView.ScrollTo(lyricTappedHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);

    }
}