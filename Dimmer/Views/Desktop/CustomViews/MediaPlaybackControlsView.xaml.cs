namespace Dimmer_MAUI.Views.CustomViews;

public partial class MediaPlaybackControlsView : ContentView
{
	HomePageVM vm { get; set; }
	public MediaPlaybackControlsView()
	{
		InitializeComponent();
		vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
		this.BindingContext = vm;
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



    private void PointerGestureRecognizer_PointerReleased(object sender, PointerEventArgs e)
    {

    }
}