namespace Dimmer_MAUI.Views.CustomViews;

public partial class MediaPlaybackControlsView : ContentView
{	
	public MediaPlaybackControlsView()
	{
		InitializeComponent();
		var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
		this.BindingContext = vm;
	}
}