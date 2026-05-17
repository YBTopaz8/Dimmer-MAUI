global using Dimmer.Views;
global using Dimmer.Views.Album;
global using Dimmer.Views.Artist;
global using Dimmer.Views.Settings;
global using Dimmer.Views.SingleSong;
using Dimmer.Views.CustomViews;
using Dimmer.Views.DimmerCloud;
using Dimmer.Views.DimmerStats;
using Dimmer.Views.LastFM;
using Dimmer.Views.Toolkit;

namespace Dimmer;

public partial class DimmerShell : Shell
{
	public DimmerShell()
	{
		InitializeComponent();

        Routing.RegisterRoute(nameof(DetailsOverview), typeof(DetailsOverview));
        Routing.RegisterRoute(nameof(ArtistPage), typeof(ArtistPage));
        Routing.RegisterRoute(nameof(AlbumPage), typeof(AlbumPage));
        Routing.RegisterRoute(nameof(DimmerHomeCenter), typeof(DimmerHomeCenter));
        Routing.RegisterRoute(nameof(DuplicateFinder), typeof(DuplicateFinder));
                //Routing.RegisterRoute(nameof(LastFMLogin), typeof(LastFMLogin));


    }
}