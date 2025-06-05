using Dimmer.ViewModel;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(MusicPlayerPage), typeof(MusicPlayerPage));
        Routing.RegisterRoute(nameof(DimmerSettings), typeof(DimmerSettings));
        Routing.RegisterRoute(nameof(SearchSongPage), typeof(SearchSongPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        BaseViewModel vm = IPlatformApplication.Current!.Services.GetService<BaseViewModel>()!;

    }
}