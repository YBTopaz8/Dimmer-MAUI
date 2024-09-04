﻿
namespace Dimmer_MAUI;

public partial class AppShell : Shell
{

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(HomeD), typeof(HomeD));
        Routing.RegisterRoute(nameof(NowPlayingD), typeof(NowPlayingD));
        Routing.RegisterRoute(nameof(PlaylistsPageD), typeof(PlaylistsPageD));
        Routing.RegisterRoute(nameof(ArtistsPageD), typeof (ArtistsPageD));
        Routing.RegisterRoute(nameof(FullStatsD), typeof (FullStatsD));
        Routing.RegisterRoute(nameof(SingleSongStatsPageD), typeof (SingleSongStatsPageD));
    }


    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        //if (args.Current.Location.OriginalString.Contains("MainPageD")) USE THIS TO DO SOMETHING WHEN USER CLICKS BTN
        //{
        //    HandleHomeButtonClicked();
        //}
    }

    
}

public enum PageEnum
{
    MainPage,
    NowPlayingPage,
    PlaylistsPage,
    FullStatsPage
}