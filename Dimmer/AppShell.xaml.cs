namespace Dimmer_MAUI;

public partial class AppShell : Shell
{
    
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(HomeD), typeof(HomeD));
    }
}
