using DevExpress.Maui.Core;
using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.Mobile;

public partial class TopStatsPageM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public TopStatsPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        MyViewModel = homePageVM;

        btmSheet = IPlatformApplication.Current!.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);

    }
    public HomePageVM MyViewModel { get; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPage = PageEnum.FullStatsPage;
        int itemHandle = UserChatColView.GetItemHandle(MyViewModel.ChatMessages.Count);
        //UserChatColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.End);


        //MyViewModel.LoadDailyData();
    }

    private void ShowSongStats_Tapped(object sender, TappedEventArgs e)
    {
        FlexLayout send = (FlexLayout)sender;
        SingleSongStatistics? song = send.BindingContext as SingleSongStatistics;
        if (song is null)
        {
            return;
        }
        MyViewModel.ShowSingleSongStatsCommand.Execute(song.Song);

    }

    //private async void ShareStatBtn_Clicked(object sender, EventArgs e)
    //{
    //    ShareStatBtn.IsVisible = false;

    //    string shareCapture = "viewToShare.png";
    //    string filePath = Path.Combine(FileSystem.CacheDirectory, shareCapture);

    //    await SongStatView.CaptureCurrentViewAsync(OverViewSection, filePath);

    //    ShareStatBtn.IsVisible = true;

    //}

  
    int SelectedGeneralView;
    private async void AddReaction_Clicked(object sender, EventArgs e)
    {
        ImageButton send = (ImageButton)sender;
        UserActivity? uAct = send.BindingContext as UserActivity;
        await OGSenderView.DimmInCompletely();
        OGSenderUserName.Text = uAct.Sender.Username;
        OGSenderLabel.Text = uAct.ChatMessage.Content;
        //var Msg = (UserActivity)send.BindingContext
    }
 

    private void EditRemoveReaction_Clicked(object sender, EventArgs e)
    {

    }

    private void MsgBorderPointerRecog_PointerEntered(object sender, PointerEventArgs e)
    {

    }

    private void MsgBorderPointerRecog_PointerExited(object sender, PointerEventArgs e)
    {

    }

    private async void SendTextMsgBtn_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(ChatMsgViewText.Text) || MyViewModel is null)
        {
            return;
        }
        await MyViewModel.SendMessageAsync(ChatMsgViewText.Text);
        ChatMsgViewText.Text = string.Empty;
        await OGSenderView.DimmOutCompletely();
    }

    private void SepecificUserVew_TouchDown(object sender, EventArgs e)
    {

    }

    private async void CloseReplyWindow_Clicked(object sender, EventArgs e)
    {

        await OGSenderView.DimmOutCompletely();
    }
    private void HomeTabView_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {

    }
    private void BtmBar_Loaded(object sender, EventArgs e)
    {

    }


    private async void BtmBarTapGest_Tapped(object sender, TappedEventArgs e)
    {
        DXBorder send = (DXBorder)sender;

        if (MyViewModel.IsPlaying)
        {

            MyViewModel.PauseSong();
            RunFocusModeAnimation(send, Color.FromArgb("#8B0000")); // DarkRed for pause

            await send.MyBackgroundColorTo(Color.FromArgb("#252526"), length: 300);
        }
        else
        {
            await send.MyBackgroundColorTo(Color.FromArgb("#483D8B"), length: 300);
            //RunFocusModeAnimation(send, Color.FromArgb("#483D8B")); // DarkSlateBlue for resume
            if (MyViewModel.CurrentPositionInSeconds.IsZeroOrNaN())
            {
                MyViewModel.PlaySong(MyViewModel.TemporarilyPickedSong);
            }
            else
            {
                MyViewModel.ResumeSong();
            }
        }

    }
    public void RunFocusModeAnimation(DXBorder bView, Color strokeColor)
    {
        if (bView == null)
            return;

        // Set the stroke color based on pause/resume state
        bView.BorderColor= strokeColor;

        // Define a single animation to embiggen the stroke
        Animation expandAnimation = new Animation(v => bView.BorderThickness = v, // Only animating BorderThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        Animation shrinkAnimation = new Animation(
            v => bView.BorderThickness = v,
            5,                                   // Start at 10 thickness
            0,                                    // Reduce to 0 thickness
            Easing.CubicInOut
        );

        // Combine expand and shrink animations into one sequence
        Animation animationSequence = new Animation
        {
            { 0, 0.5, expandAnimation },   // Embiggen in the first half
            { 0.5, 1, shrinkAnimation }    // Shrink back in the second half
        };

        // Run the full animation sequence
        animationSequence.Commit(bView, "FocusModeAnimation", length: 300, easing: Easing.Linear);
    }

    protected override bool OnBackButtonPressed()
    {

        switch (HomeTabView.SelectedItemIndex)
        {
            case 0:
                break;
            case 1:
                HomeTabView.SelectedItemIndex = 0;
                break;
            default:
                break;
        }
        return true;
    }


    private double _startX;
    private double _startY;
    private bool _isPanning;
    private CancellationTokenSource _debounceTokenSource = new CancellationTokenSource();
    private int _lastFullyVisibleHandle = -1; // Track the last *fully visible* item handle.

    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        DXBorder send = (DXBorder)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _isPanning = true;
                _startX = BtmBar.TranslationX;
                _startY = BtmBar.TranslationY;
                break;

            case GestureStatus.Running:
                if (!_isPanning)
                    return; // Safety check

                BtmBar.TranslationX = _startX + e.TotalX;
                BtmBar.TranslationY = _startY + e.TotalY;
                break;

            case GestureStatus.Completed:
                _isPanning = false;

                double deltaX = BtmBar.TranslationX - _startX;
                double deltaY = BtmBar.TranslationY - _startY;
                double absDeltaX = Math.Abs(deltaX);
                double absDeltaY = Math.Abs(deltaY);

                // Haptic feedback based on direction
                if (absDeltaX > absDeltaY) // Horizontal swipe
                {
                    if (absDeltaX > absDeltaY) // Horizontal swipe
                    {
                        try
                        {
                            if (deltaX > 0) // Right
                            {
                                HapticFeedback.Perform(HapticFeedbackType.LongPress);
                                Debug.WriteLine("Swiped Right");

                                MyViewModel.PlayPreviousSong();

                                Task colorTask = AnimateColor(send, Colors.SlateBlue);
                                Task<bool> bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(colorTask, bounceTask);
                            }
                            else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                MyViewModel.PlayPreviousSong();

                                Task colorTask = AnimateColor(send, Colors.MediumPurple);
                                Task<bool> bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(colorTask, bounceTask);
                            }
                        }
                        catch { }
                    }

                    else // Left
                    {
                        try
                        {
                            Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                            MyViewModel.PlayPreviousSong();
                            Debug.WriteLine("Swiped left");
                            Task t1 = send.MyBackgroundColorTo(Colors.MediumPurple, length: 300);
                            Task t2 = Task.Delay(500);
                            Task t3 = send.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
                            await Task.WhenAll(t1, t2, t3);
                        }
                        catch { }
                    }
                }
                else  //Vertical swipe
                {
                    if (deltaY > 0) // Down
                    {
                        try
                        {
                            if (HomeTabView.SelectedItemIndex != 0)
                            {
                                HomeTabView.SelectedItemIndex=0;
                            }
                            //var itemHandle = SongsColView.FindItemHandle(MyViewModel.TemporarilyPickedSong);
                            //SongsColView.ScrollTo(itemHandle, DevExpress.Maui.Core.DXScrollToPosition.Start);

                            HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        }
                        catch { }
                    }
                    else  //Up
                    {
                        try
                        {
                            if (HomeTabView.SelectedItemIndex != 1)
                            {
                                HomeTabView.SelectedItemIndex = 1;
                                //await MyViewModel.AssignSyncLyricsCV(LyricsColView);
                            }
                            else
                            {
                                MyViewModel.LoadArtistSongs();
                                //ContextBtmSheet.State = BottomSheetState.HalfExpanded;
                                //ContextBtmSheet.HalfExpandedRatio = 0.8;
                                //await NowPlayingQueueView.DimmOutCompletely();
                                //NowPlayingQueueView.IsVisible=false;
                                //await ArtistSongsView.DimmInCompletely();
                                //ArtistSongsView.IsVisible=true;
                                //HomeTabView.SelectedItemIndex = prevViewIndex;
                            }
                        }
                        catch { }
                    }

                }

                await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);
                break;


            case GestureStatus.Canceled:
                _isPanning = false;
                await BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut); // Return to original position
                break;

        }
    }
    int prevViewIndex = 0;
    // Extracted color animation method for reusability
    async Task AnimateColor(VisualElement element, Color color)
    {
        await element.MyBackgroundColorTo(color, length: 300);
        await Task.Delay(300); // Reduce freeze by using a lower delay
        await element.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
    }
    private void Chip_Tap(object sender, HandledEventArgs e)
    {
        Chip send = (Chip)sender;
        string? param = send.TapCommandParameter.ToString();
        switch (param)
        {
            case "repeat":

                MyViewModel.ToggleRepeatModeCommand.Execute(true);

                break;
            case "shuffle":
                MyViewModel.CurrentQueue = 1;
                break;
            case "Lyrics":
                MyViewModel.CurrentQueue = 2;
                break;
            default:
                break;
        }

    }
    private void ViewNowPlayPage_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.UpdateContextMenuData(MyViewModel.MySelectedSong);
        //ContextBtmSheet.State = BottomSheetState.HalfExpanded;
    }
    private void ContextIcon_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.LoadArtistSongs();
        //ContextBtmSheet.State = BottomSheetState.HalfExpanded;
        //ContextBtmSheet.HalfExpandedRatio = 0.8;
    }


    private void UserChatColView_SelectionChanged(object sender, DevExpress.Maui.CollectionView.CollectionViewSelectionChangedEventArgs e)
    {

    }

    private void UserChatColView_Loaded(object sender, EventArgs e)
    {
        MyViewModel.userChatColViewDX= UserChatColView;
    }

    private void UserChatColView_Unloaded(object sender, EventArgs e)
    {
        MyViewModel.userChatColViewDX= null;
    }


    private bool _isAnimating;
    private double _containerWidth;
    private async void MarqueeLabel_SizeChanged(object sender, EventArgs e)
    {
        if (BtmBar.Width <= 0)
            return;

        _containerWidth = BtmBar.Width;
        // Start animation if text width is larger than container
        if (!_isAnimating && GetTextWidth() > _containerWidth)
            await StartAnimation();
    }

    private double GetTextWidth()
    {
        // Measures the text width based on available height
        Size size = Measure(double.PositiveInfinity, Height);
        return size.Width;
    }

    public async Task StartAnimation()
    {
        _isAnimating = true;
        double textWidth = GetTextWidth();

        // Calculate extra distance to scroll completely off screen
        double scrollDistance = textWidth + _containerWidth;

        // Reset starting position (text starts at container's right edge)
        TranslationX = _containerWidth;

        while (true)
        {

            // Animate translation: move from right to left completely
            await this.TranslateTo(-textWidth, 0, 5000, Easing.Linear);
            // Optional pause at the end
            await Task.Delay(1000);
            // Reset instantly to starting position
            TranslationX = _containerWidth;
        }
    }
}