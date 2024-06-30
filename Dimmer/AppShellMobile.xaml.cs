namespace Dimmer_MAUI;

public partial class AppShellMobile : Shell
{
	public AppShellMobile()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(HomePageM), typeof(HomePageM));
		Routing.RegisterRoute(nameof(NowPlayingPageM), typeof(NowPlayingPageM));
		Routing.RegisterRoute(nameof(PlaylistsPageM), typeof(PlaylistsPageM));
    }
}