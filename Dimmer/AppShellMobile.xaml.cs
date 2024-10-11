
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
        this.Navigating += OnNavigating;
    }

    private void OnNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        //throw new NotImplementedException();
    }

    protected override bool OnBackButtonPressed()
    {
        var currentPage = Current.CurrentPage;

        var targetPages = new[] { typeof(PlaylistsPageM), typeof(AlbumsM), typeof(TopStatsPageM) };

        if (targetPages.Contains(currentPage.GetType()))
        {
            
            shelltabbar.CurrentItem = homeTab;
            return true;
        }
        if (currentPage.GetType() == typeof(HomePageM))
        {
            return true;
        }
        return base.OnBackButtonPressed();

    }
}