namespace Dimmer.WinUI.Views.CustomViews;

public partial class MediaControlBtmBar : ContentView
{
    public HomeViewModel MyViewModel { get; internal set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MediaControlBtmBar()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
		InitializeComponent();
        
        MyViewModel = IPlatformApplication.Current!.Services.GetService<HomeViewModel>()!;
        BindingContext = MyViewModel;
    }

    private void MediaControlBtmBar_Loaded(object? sender, EventArgs e)
    {
        MyViewModel = IPlatformApplication.Current!.Services.GetService<HomeViewModel>()!;
        BindingContext = MyViewModel;

    }


    private bool _isThrottling;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;
        Slider send = (Slider)sender;
        double s = send.Value;
        await MyViewModel.SeekSongPosition(currPosPer: s);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private async void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {

        await this.AnimateFocusModePointerEnter();
    }

    private async void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        await this.AnimateFocusModePointerExited(endOpacity: 0.4, endScale: 1);
    }

    private static void NavToSingleSongShell_Tapped(object sender, TappedEventArgs e)
    {
        Shell.Current.GoToAsync(nameof(SingleSongPage));
    }



    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }
    private void ShowCntxtMenuBtn_Clicked(object sender, EventArgs e)
    {
    }

    private void SfEffectsView_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged += MainLayout_PointerWheelChanged;
#endif
    }

#if WINDOWS
    private void MainLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(null);
        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        if (mouseWheelDelta != 0)
        {
            if (mouseWheelDelta > 0)
            {
                if (MyViewModel.VolumeLevel >=1)
                {
                    return;
                }
                MyViewModel.IncredeVolume();
                // Handle scroll up
            }
            else
            {
                MyViewModel.DecredeVolume();
                // Handle scroll down
            }
        }

        e.Handled = true;
    }

#endif

    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        Microsoft.UI.Xaml.UIElement? mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged -= MainLayout_PointerWheelChanged;
#endif

    }

    private async void PlayPrevious_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.PlayPrevious();
    }

    private async void PlayNext_Clicked(object sender, EventArgs e)
    {
       await MyViewModel.PlayNext();
    }

    private async void PlayPauseSong_Tapped(object sender, TappedEventArgs e)
    {
        await MyViewModel.PlayPauseSong();
    }

    private void ShuffleBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.ToggleRepeatMode();
    }
}