using System.ComponentModel;

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

        // Call your method
        vm.SeekSongPosition();

        // Add a delay to prevent firing again in a short time window
        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        myPage.Opacity = 1;
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        myPage.Opacity = 0.5;
    }
}