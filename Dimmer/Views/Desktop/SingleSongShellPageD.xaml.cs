using Syncfusion.Maui.Toolkit.Chips;
using System.Diagnostics;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongShellPageD : ContentPage
{
	public SingleSongShellPageD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (HomePageVM.TemporarilyPickedSong is null)
        {
            return;
        }
        HomePageVM.CurrentPage = PageEnum.NowPlayingPage;
        DeviceDisplay.Current.KeepScreenOn = true;
        HomePageVM.AssignSyncLyricsCV(LyricsColView);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        HomePageVM.CurrentViewIndex = 0;
        HomePageVM.IsViewingDifferentSong = false;
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                break;
            case 1:
                emptyV.IsVisible = false;
                if (HomePageVM.AllSyncLyrics is not null)
                {
                    HomePageVM.AllSyncLyrics = Array.Empty<Content>();
                }
                break;
            case 2:
                break;
            default:
                
                break;
        }
        if (e.NewIndex == 2)
        {
            HomePageVM.ShowSingleSongStatsCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);
        }
    }

    private void SongsPlayed_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {

    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;

        HomePageVM.SeekSongPosition();


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }
    bool isOnFocusMode = false;
    private async void FocusModePointerRec_PointerEntered(object sender, PointerEventArgs e)
    {
        if (isOnFocusMode)
        {
            if (HomePageVM.IsSleek)
            {
                return;
            }
            await FocusModeUI.AnimateFocusModePointerEnter();
            leftImgBtn.IsVisible = true;
            rightImgBtn.IsVisible = true;
        }
    }

    private async void FocusModePointerRec_PointerExited(object sender, PointerEventArgs e)
    {
        if (isOnFocusMode)
        {
            await FocusModeUI.AnimateFocusModePointerExited();
            leftImgBtn.IsVisible = false;
            rightImgBtn.IsVisible = false;
        }
    }
    private void ToggleSleekModeClicked(object sender, EventArgs e)
    {

        //await FocusModeUI.AnimateFocusModePointerExited();
        leftImgBtn.IsVisible = false;
        rightImgBtn.IsVisible = false;
        HomePageVM.ToggleSleekModeCommand.Execute(true);
    }
    private async void ToggleFocusModeClicked(object sender, EventArgs e)
    {
        if (FocusModeUI.IsVisible)
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeOutBack(),
            NormalNowPlayingUI.AnimateFadeInFront()

            );

            isOnFocusMode = false;
        }
        else
        {
            await Task.WhenAll(
            FocusModeUI.AnimateFadeInFront(),
            NormalNowPlayingUI.AnimateFadeOutBack());
            isOnFocusMode = true;
        }
    }



    private async void PointerGestureRecognizer_PointerPressed(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerPressed();
    }

    private async void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {
        var send = (View)sender;
        await send.AnimateHighlightPointerReleased();
    }

    private void FocusModePlayResume_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            HomePageVM.PauseSongCommand.Execute(null);
            RunFocusModeAnimation(sender as AvatarView, Color.FromArgb("#8B0000")); // DarkRed for pause
        }
        else
        {
            HomePageVM.ResumeSongCommand.Execute(null);
            RunFocusModeAnimation(sender as AvatarView, Color.FromArgb("#483D8B")); // DarkSlateBlue for resume
        }
    }

    private void RunFocusModeAnimation(AvatarView avatarView, Color strokeColor)
    {
        if (avatarView == null)
            return;

        // Set the stroke color based on pause/resume state
        avatarView.Stroke = strokeColor;

        // Define a single animation to embiggen the stroke
        var expandAnimation = new Animation(v => avatarView.StrokeThickness = v, // Only animating StrokeThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        var shrinkAnimation = new Animation(
            v => avatarView.StrokeThickness = v,
            5,                                   // Start at 10 thickness
            0,                                    // Reduce to 0 thickness
            Easing.CubicInOut
        );

        // Combine expand and shrink animations into one sequence
        var animationSequence = new Animation
        {
            { 0, 0.5, expandAnimation },   // Embiggen in the first half
            { 0.5, 1, shrinkAnimation }    // Shrink back in the second half
        };

        // Run the full animation sequence
        animationSequence.Commit(avatarView, "FocusModeAnimation", length: 500, easing: Easing.Linear);
    }


    private void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
            {
                LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
            }
         
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void SeekSongPosFromLyric_Tapped(object sender, TappedEventArgs e)
    {
        if (HomePageVM.IsPlaying)
        {
            var bor = (Border)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            HomePageVM.SeekSongPosition(lyr);
        }
    }

    bool CanScroll = true;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        CanScroll = false;
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        CanScroll = true;
    }


    private async void ViewLyricsBtn_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var title = send.Text;
        var thisContent = send.BindingContext as Dimmer_MAUI.Utilities.Models.Content;
        if (title == "Synced Lyrics")
        {

            await HomePageVM.ShowSingleLyricsPreviewPopup(thisContent!, false);
        }
        else
        if (title == "Plain Lyrics")
        {

            await HomePageVM.ShowSingleLyricsPreviewPopup(thisContent!, true);
        }
    }

    //private async void SfSegmentedControl_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.SegmentedControl.SelectionChangedEventArgs e)
    //{
    //    var newSelection = e.NewIndex;
    //    switch (newSelection)
    //    {
    //        case 0:
    //            await Task.WhenAll(
    //            WeeklyStats.AnimateFadeOutBack(),
    //            MonthlyStats.AnimateFadeOutBack(),
    //            YearlyStats.AnimateFadeOutBack(),
    //            DailyStats.AnimateFadeInFront());
    //            HomePageVM.LoadDailyStats(HomePageVM.SelectedSongToOpenBtmSheet);
    //            break;
    //        case 1:
    //            await Task.WhenAll(
    //           DailyStats.AnimateFadeOutBack(),
    //           YearlyStats.AnimateFadeOutBack(),
    //           MonthlyStats.AnimateFadeOutBack(),
    //           WeeklyStats.AnimateFadeInFront()   // Fade in WeeklyStats
    //       );
    //            HomePageVM.LoadWeeklyStats(HomePageVM.SelectedSongToOpenBtmSheet);

    //            break;
    //        case 2:
    //            await Task.WhenAll(
    //            DailyStats.AnimateFadeOutBack(),
    //            WeeklyStats.AnimateFadeOutBack(),
    //            YearlyStats.AnimateFadeOutBack(),
    //            MonthlyStats.AnimateFadeInFront()  // Fade in MonthlyStats
    //        );
    //            HomePageVM.LoadMonthlyStats(HomePageVM.SelectedSongToOpenBtmSheet);
    //            break;
    //        case 3:
    //            await Task.WhenAll(
    //            DailyStats.AnimateFadeOutBack(),
    //            WeeklyStats.AnimateFadeOutBack(),
    //            MonthlyStats.AnimateFadeOutBack(),
    //            YearlyStats.AnimateFadeInFront()    // Fade in YearlyStats
    //        );
    //            HomePageVM.LoadYearlyStats(HomePageVM.SelectedSongToOpenBtmSheet);
    //            break;
    //        default:
    //            break;
    //    }
    //}


    private void ShowHideChart_CheckChanged(object sender, EventArgs e)
    {
        
    }

    private async void SearchLyricsOnLyrLib_Clicked(object sender, EventArgs e)
    {
        emptyV.IsVisible = true;
        await Task.WhenAll(Lookgif.AnimateFadeInFront(), fetchFailed.AnimateFadeOutBack(),
NoLyricsFoundMsg.AnimateFadeOutBack());

        Lookgif.IsVisible = true;

        await HomePageVM.FetchLyrics(true);

        await Task.WhenAll(Lookgif.AnimateFadeOutBack(), fetchFailed.AnimateFadeInFront(),
NoLyricsFoundMsg.AnimateFadeInFront());
        fetchFailed.IsAnimationPlaying = false;
        
        await Task.Delay(3000);
        fetchFailed.IsAnimationPlaying = false;
        fetchFailed.IsVisible = false;
        emptyV.IsVisible = false;
    }


    private async void SfChipGroup_ChipClicked(object sender, EventArgs e)
    {
        var send = sender as SfChip;
        var newSelection = int.Parse(send.CommandParameter.ToString()!);
        switch (newSelection)
        {
            case 0:
                await Task.WhenAll(
                    SyncedLyricGrid.AnimateFadeInFront(),
                    PlainLyricsGrid.AnimateFadeOutBack(),
                    SearchLyricsGrid.AnimateFadeOutBack());
                
                break;
            case 1:
                await Task.WhenAll(
                    SyncedLyricGrid.AnimateFadeOutBack(),
                    PlainLyricsGrid.AnimateFadeInFront(),
                    SearchLyricsGrid.AnimateFadeOutBack());

                break;
            case 2:
                await Task.WhenAll(
                SyncedLyricGrid.AnimateFadeOutBack(),
                PlainLyricsGrid.AnimateFadeOutBack(),
                SearchLyricsGrid.AnimateFadeInFront());

                break;
                default:
                break;
        }
    }

    private void RatingChipCtrl_ChipClicked(object sender, EventArgs e)
    {
        var ee = RatingChipCtrl.SelectedItem;

        Debug.WriteLine(ee.GetType());
    }

    private void SearchLastFM_Clicked(object sender, EventArgs e)
    {

    }
}
