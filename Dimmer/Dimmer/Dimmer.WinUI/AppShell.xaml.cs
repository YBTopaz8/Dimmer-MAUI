using Dimmer.WinUI.Views;

namespace Dimmer.WinUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();


        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
    }
}