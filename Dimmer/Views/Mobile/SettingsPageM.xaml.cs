

using DevExpress.Maui.Core;

namespace Dimmer_MAUI.Views.Mobile;

public partial class SettingsPageM : ContentPage
{
	public SettingsPageM(HomePageVM vm)
    {
        InitializeComponent();
        this.MyViewModel = vm;
        BindingContext = vm;
    }
    public HomePageVM MyViewModel { get; }

    private async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        var reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void LoginSignUpToggle_Click(object sender, EventArgs e)
    {
        LoginUI.IsVisible = !LoginUI.IsVisible;
        SignUpUI.IsVisible = !SignUpUI.IsVisible;
    }
    protected async override void OnAppearing()
    {
        base.OnAppearing();
        //await MyViewModel.SetChatRoom(ChatRoomOptions.PersonalRoom);

        //LoginBtn_Clicked(null, null); //review this.
    }
    private async void SignUpBtn_Clicked(object sender, EventArgs e)
    {
        SignUpBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(SignUpUname.Text) ||
            string.IsNullOrWhiteSpace(SignUpPass.Text) ||
            string.IsNullOrWhiteSpace(SignUpEmail.Text))
        {
            await Shell.Current.DisplayAlert("Error", "All fields are required.", "OK");
            return;
        }

        ParseUser user = new ParseUser()
        {
            Username = SignUpUname.Text.Trim(),
            Password = SignUpPass.Text.Trim(),
            Email = SignUpEmail.Text.Trim()
        };

        try
        {
            await ParseClient.Instance.SignUpWithAsync(user);
            await Shell.Current.DisplayAlert("Success", "Account created successfully!", "OK");

            // Navigate to a different page or reset fields
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Sign-up failed: {ex.Message}", "OK");

        }
    }

    private async void LoginBtn_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.CurrentUserOnline is not null)
        {
            if (await MyViewModel.CurrentUserOnline.IsAuthenticatedAsync())
            {
                return;
            }
        }
        //LoginBtn.IsEnabled = false;
        if (string.IsNullOrWhiteSpace(LoginUname.Text) || string.IsNullOrWhiteSpace(LoginPass.Text))
        {
            await Shell.Current.DisplayAlert("Error", "Username and Password are required.", "OK");
            return;
        }

        try
        {
            var uname= LoginUname.Text.Trim();
            var pass = LoginPass.Text.Trim();
            var oUser = await ParseClient.Instance.LogInWithAsync(uname,pass);
            MyViewModel.SongsMgtService.UpdateUserLoginDetails(oUser);
            MyViewModel.SongsMgtService.CurrentOfflineUser.UserPassword = LoginPass.Text;
            MyViewModel.CurrentUserOnline = oUser;
            MyViewModel.CurrentUser.IsAuthenticated = true;
            await Shell.Current.DisplayAlert("Success !", $"Welcome Back ! {oUser.Username}", "OK");

            LoginUname.Text = string.Empty;
            LoginPass.Text = string.Empty;
            // Navigate to a different page or perform post-login actions
            //MyViewModel.SongsMgtService.GetUserAccount(oUser);
        }
        catch (Exception ex)
        {
            MyViewModel.CurrentUser.IsAuthenticated = false;
            await Shell.Current.DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");

        }
    }

    private void FullSyncBtn_Clicked(object sender, EventArgs e)
    {
       _=  MyViewModel.FullSync();
    }

    private void DXButton_Clicked(object sender, EventArgs e)
    {

    }

    private async void ScanAllBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.LoadSongsFromFolders();
    }

    private async void PickFolder_Clicked(object sender, EventArgs e)
    {
         await MyViewModel.SelectSongFromFolder();
    }

    private async void SyncPDaCS_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SongsMgtService.SyncPlayDataAndCompletionData();
    }

    private async void AddReaction_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var uAct = send.BindingContext as UserActivity;
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
        var send = (DXBorder)sender;

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
        var expandAnimation = new Animation(v => bView.BorderThickness = v, // Only animating BorderThickness now
            0,                                   // Start with 0 thickness
            5,                                  // Expand to 10 thickness
            Easing.CubicInOut                    // Smooth easing
        );

        // Shrink the stroke back to zero after embiggen
        var shrinkAnimation = new Animation(
            v => bView.BorderThickness = v,
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
        animationSequence.Commit(bView, "FocusModeAnimation", length: 300, easing: Easing.Linear);
    }


    private double _startX;
    private double _startY;
    private bool _isPanning;
    private CancellationTokenSource _debounceTokenSource = new CancellationTokenSource();
    private int _lastFullyVisibleHandle = -1; // Track the last *fully visible* item handle.

    private async void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var send = (DXBorder)sender;

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

                                MyViewModel.PlayNextSongCommand.Execute(null);

                                var colorTask = AnimateColor(send, Colors.SlateBlue);
                                var bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

                                await Task.WhenAll(colorTask, bounceTask);
                            }
                            else // Left
                            {
                                Vibration.Vibrate(TimeSpan.FromMilliseconds(50)); // Short vibration
                                MyViewModel.PlayPreviousSongCommand.Execute(null);

                                var colorTask = AnimateColor(send, Colors.MediumPurple);
                                var bounceTask = BtmBar.TranslateTo(0, 0, 250, Easing.BounceOut);

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
                            MyViewModel.PlayPreviousSongCommand.Execute(null);
                            Debug.WriteLine("Swiped left");
                            var t1 = send.MyBackgroundColorTo(Colors.MediumPurple, length: 300);
                            var t2 = Task.Delay(500);
                            var t3 = send.MyBackgroundColorTo(Colors.DarkSlateBlue, length: 300);
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
                                ContextBtmSheet.State = BottomSheetState.HalfExpanded;
                                ContextBtmSheet.HalfExpandedRatio = 0.8;
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
        var send = (Chip)sender;
        var param = send.TapCommandParameter.ToString();
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
        ContextBtmSheet.State = BottomSheetState.HalfExpanded;
    }
    private void ContextIcon_Tap(object sender, HandledEventArgs e)
    {
        MyViewModel.LoadArtistSongs();
        ContextBtmSheet.State = BottomSheetState.HalfExpanded;
        ContextBtmSheet.HalfExpandedRatio = 0.8;        
    }

    private void DXButton_Clicked_1(object sender, EventArgs e)
    {
        MyViewModel.SongsMgtService.GetUserAccountOnline();
    }
}