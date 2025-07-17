using AndroidX.Lifecycle;

using Dimmer.ViewModel;
using Dimmer.Views.Stats;

namespace Dimmer;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(DimmerSettings), typeof(DimmerSettings));
        Routing.RegisterRoute(nameof(SearchSongPage), typeof(SearchSongPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(PlayHistoryPage), typeof(PlayHistoryPage));
    }

    protected  override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel  = IPlatformApplication.Current.Services.GetService<BaseViewModel>();
        if ((MyViewModel is null))
        {
            return;
        }
        MyViewModel.Initialize();
    }

    public BaseViewModel MyViewModel { get; internal set; }
}