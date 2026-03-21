global using Dimmer.Views;
global using Dimmer.Views.SingleSong;
global using Dimmer.Views.Album;
global using Dimmer.Views.Artist;
global using Dimmer.Views.Settings;

namespace Dimmer;

public partial class DimmerShell : Shell
{
	public DimmerShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
		Routing.RegisterRoute(nameof(DetailsOverview), typeof(DetailsOverview));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(ArtistPage), typeof(ArtistPage));
		Routing.RegisterRoute(nameof(AlbumPage), typeof(AlbumPage));
    }
}