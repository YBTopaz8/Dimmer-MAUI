

namespace Dimmer_MAUI.Views.CustomViews;

public partial class MediaPlaybackControlsView : ContentView
{
	HomePageVM vm { get; set; }
	public MediaPlaybackControlsView()
	{
		InitializeComponent();
        //vm.PropertyChanged += OnPropertyChanged;
        this.Loaded += MediaPlaybackControlsView_Loaded;
    }

    private void MediaPlaybackControlsView_Loaded(object? sender, EventArgs e)
    {
        vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        BindingContext = vm;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName==nameof(vm.IsPlaying))
        {
            bool isPlaying = vm.IsPlaying;
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

        vm.SeekSongPosition();

        
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
        vm.SelectedSongToOpenBtmSheet = vm.TemporarilyPickedSong!;
        await vm.NavToSingleSongShell();
    }


    private async void PlayPauseBtn_Tapped(object sender, TappedEventArgs e)
    {
        if (vm.IsPlaying)
        {
            await vm.PauseSong();
        }
        else
        {
            await vm.ResumeSong();
        }
    }

}