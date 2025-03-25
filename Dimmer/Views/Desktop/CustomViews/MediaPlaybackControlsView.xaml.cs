#if WINDOWS
using Microsoft.UI.Xaml.Input;
#endif
using Syncfusion.Maui.Toolkit.EffectsView;

namespace Dimmer_MAUI.Views.CustomViews;

public partial class MediaPlaybackControlsView : ContentView
{
	HomePageVM MyViewModel { get; set; }
	public MediaPlaybackControlsView() 
    {
		InitializeComponent();
        HomePageVM VM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        MyViewModel = VM;
        //BindingContext = VM;

    }


    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        
    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;
        Slider send = (Slider)sender;
        double s = send.Value;
        MyViewModel.SeekSongPosition(currPosPer:s);

        
        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private async void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        
        await this.AnimateFocusModePointerEnter();
    }

    private async void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        await this.AnimateFocusModePointerExited(endOpacity:0.4, endScale:1);
    }

    private async void NavToSingleSongShell_Tapped(object sender, TappedEventArgs e)
    {
        //var send = (View)sender;
        //var song = send.BindingContext as SongModelView;
        MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong!;
        await MyViewModel.NavToSingleSongShell();
    }


    private void PlayPauseBtn_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSong();
        }
        else
        {
            MyViewModel.ResumeSong();
        }
    }

    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatModeCommand.Execute(true);
    }
    private void ShowCntxtMenuBtn_Clicked(object sender, EventArgs e)
    {
        View thiss = this as View;
        View? par = thiss.Parent as View;
        MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong;
        MyViewModel.ToggleFlyout();


        //await MyViewModel.ShowHideContextMenuFromBtmBar(thiss);
    }
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        View send = (View)sender;
        SongModelView? song = (SongModelView)send.BindingContext;

        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
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
        // Retrieve the mouse wheel delta value
        var pointerPoint = e.GetCurrentPoint(null);
        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        // Check if the event is from a mouse wheel
        if (mouseWheelDelta != 0)
        {
            // Positive delta indicates wheel scrolled up
            // Negative delta indicates wheel scrolled down
            if (mouseWheelDelta > 0)
            {
                if (MyViewModel.VolumeSliderValue >=1)
                {
                    return;
                }
                MyViewModel.IncreaseVolumeCommand.Execute(true);
                // Handle scroll up
            }
            else
            {
                MyViewModel.DecreaseVolumeCommand.Execute(true);
                // Handle scroll down
            }
        }

        // Mark the event as handled
        e.Handled = true;
    }

#endif

    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {

    }

    private void PlayPrevious_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayPreviousSong();
    }

    private void PlayNext_Clicked(object sender, EventArgs e)
    {
        MyViewModel.PlayNextSong();
    }
}