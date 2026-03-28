global using Animation = Microsoft.Maui.Controls.Animation;

namespace Dimmer.Views.CustomViews;

public partial class NowPlayingBottomSheet : BottomSheet
{
	public NowPlayingBottomSheet()
	{
		InitializeComponent();
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelAnd>()!;
        BindingContext = MyViewModel;
	}
    BaseViewModelAnd MyViewModel { get;}

    SongModelView? songForLyrics;
    private async void LyricsChip_Tap(object sender, HandledEventArgs e)
    {
        if(MyViewModel != null)
        {
            songForLyrics = MyViewModel.SelectedSong;
            MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
            await Shell.Current.GoToAsync(nameof(DetailsOverview), true);
            _= MyViewModel.SearchLyricsAsync();
        }
    }

  

    private void SongTitleLabel_SizeChanged(object sender, EventArgs e)
    {
        double startX = SongTitleLabel.Width;
        double endX = -SongTitleLabel.Width;

        //now marquee the text
        var animation = new Animation(v => SongTitleLabel.TranslationX = v, startX, endX);
        animation.Commit(this, "MarqueeAnimation", 16, 10000, Easing.Linear, (v, c) => SongTitleLabel.TranslationX = startX, () => true);
    }

    private void PlaybackChip_Tap(object sender, HandledEventArgs e)
    {

    }

    private void SongTitleLabel_Loaded(object sender, EventArgs e)
    {

    }

    private void NowPlayingHighlightBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        if(PlayBackQueueExp.IsExpanded)
        {
            PlayBackQueueExp.IsExpanded = false;
            NowPlayingExp.IsExpanded = true;
            MainBgImg.Opacity = 0.5;
        }
        else
        {
            NowPlayingExp.IsExpanded = false;
            PlayBackQueueExp.IsExpanded = true;
            MainBgImg.Opacity = 0.1;
        }

    }

    public void ShowAndOpenPlaybackQueue()
    {
        this.Show();
        PlayBackQueueExp.IsExpanded = true;
        NowPlayingExp.IsExpanded = false;
        MainBgImg.Opacity = 0.1;
    }
    private async void RemoveSongFromQueueBtn_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    { 
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;

        await MyViewModel.RemoveFromQueue(song);
    }

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
                AllLyricsColView.ScrollTo(itemHandle,   DevExpress.Maui.Core.DXScrollToPosition.Start);
            });

        }
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
}