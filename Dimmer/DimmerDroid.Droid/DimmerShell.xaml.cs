global using Dimmer.Views;
global using Dimmer.Views.SingleSong;

namespace Dimmer;

public partial class DimmerShell : Shell
{
	public DimmerShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
		Routing.RegisterRoute(nameof(DetailsOverview), typeof(DetailsOverview));
    }
}