namespace Dimmer_MAUI;

public partial class AppShellMobile : Shell
{
	public AppShellMobile()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(HomePageM), typeof(HomePageM));
		Routing.RegisterRoute(nameof(SingleSongShell), typeof(SingleSongShell));
		Routing.RegisterRoute(nameof(PlaylistsPageM), typeof(PlaylistsPageM));
		Routing.RegisterRoute(nameof(SinglePlaylistPageM), typeof(SinglePlaylistPageM));
		Routing.RegisterRoute(nameof(FullStatsPageM), typeof(FullStatsPageM));
		Routing.RegisterRoute(nameof(SingleSongStatsPageM), typeof(SingleSongStatsPageM));
		Routing.RegisterRoute(nameof(AlbumPageM), typeof(AlbumPageM));
		Routing.RegisterRoute(nameof(ArtistsPageM), typeof(ArtistsPageM));		
    }
}