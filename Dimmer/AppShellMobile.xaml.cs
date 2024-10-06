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
		Routing.RegisterRoute(nameof(TopStatsPageM), typeof(TopStatsPageM));
		Routing.RegisterRoute(nameof(SingleSongStatsPageM), typeof(SingleSongStatsPageM));
		Routing.RegisterRoute(nameof(AlbumPageM), typeof(AlbumPageM));
		Routing.RegisterRoute(nameof(ArtistsPageM), typeof(ArtistsPageM));		
		Routing.RegisterRoute(nameof(SpecificAlbumPage), typeof(SpecificAlbumPage));
		Routing.RegisterRoute(nameof(AlbumPageM), typeof(AlbumPageM));
    }

    protected override bool OnBackButtonPressed()
    {
        var bmtSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        if (bmtSheet.IsPresented)
        {
            bmtSheet.IsPresented = false;
			return true;
        }
        return base.OnBackButtonPressed();

    }
}