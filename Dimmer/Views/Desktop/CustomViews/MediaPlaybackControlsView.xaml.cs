

namespace Dimmer_MAUI.Views.CustomViews;

public partial class MediaPlaybackControlsView : ContentView
{
	HomePageVM MyViewModel { get; set; }
	public MediaPlaybackControlsView() 
    {
		InitializeComponent();
        var VM = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        MyViewModel = VM;
        BindingContext = VM;
        
        this.Loaded += MediaPlaybackControlsView_Loaded;
    }

    private void MediaPlaybackControlsView_Loaded(object? sender, EventArgs e)
    {
        MyViewModel = IPlatformApplication.Current.Services.GetService<HomePageVM>()!;
        BindingContext = MyViewModel;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName==nameof(MyViewModel.IsPlaying))
        {
            bool isPlaying = MyViewModel.IsPlaying;
            if (isPlaying)
            {
                
            }
        }
    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekSongPosition();

        
        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private async void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        await this.AnimateFocusModePointerEnter();
    }

    private async void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        await this.AnimateFocusModePointerExited(endScale:1);
    }

    private async void NavToSingleSongShell_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext as SongModelView;
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
}