using Dimmer_MAUI.Utilities.OtherUtils;

namespace Dimmer_MAUI.Views.Desktop;

public partial class SingleSongShellD : ContentPage
{
    public SingleSongShellD(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        //MediaPlayBackCW.BindingContext = homePageVM;

    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.NowPlayingPage;
        if (HomePageVM.AllSyncLyrics is not null)
        {
            Array.Clear(HomePageVM.AllSyncLyrics);
        }
        //TabV.SelectedTab = TabV.Items[0];

        Task.Delay(3000);

        //FocusCaro.ItemsSource = HomePageVM.BackEndQ;
    }

    private void TabV_SelectedTabChanged(object sender, TabItem e)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        if (e != null && e.Title == "Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(0);
        }

        if (e != null && e.Title == "Stats")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(1);

        }


        if (e != null && e.Title == "Fetch Lyrics")
        {
            vm.SwitchViewNowPlayingPageCommand.Execute(2);
        }

    }

    protected override void OnDisappearing()
    {
        //TabV.SelectedTab = TabV.Items[0];

        base.OnDisappearing();
    }

    private void tabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private void SongsPlayed_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
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


}