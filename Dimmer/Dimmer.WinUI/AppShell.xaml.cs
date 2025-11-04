using System.Diagnostics;

using CommunityToolkit.Maui.Behaviors;

using Dimmer.DimmerSearch;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.AlbumsPage;
using Dimmer.WinUI.Views.DimmerLiveUI;
using Dimmer.WinUI.Views.PlaylistPages;
using Dimmer.WinUI.Views.TQLCentric;
using Dimmer.WinUI.Views.WinUIPages;

using Microsoft.UI.Xaml.Media.Animation;

using Vanara.PInvoke;


namespace Dimmer.WinUI;

public partial class AppShell : Shell
{
    public BaseViewModelWin MyViewModel { get; }

    public AppShell(BaseViewModelWin baseViewModel)
    {
        InitializeComponent();
        MyViewModel = baseViewModel;

        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(OnlinePageManagement), typeof(OnlinePageManagement));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(DimmerLivePage), typeof(DimmerLivePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(LibSanityPage), typeof(LibSanityPage));
        Routing.RegisterRoute(nameof(ExperimentsPage), typeof(ExperimentsPage));
        Routing.RegisterRoute(nameof(SocialView), typeof(SocialView));
        Routing.RegisterRoute(nameof(AllArtistsPage), typeof(AllArtistsPage));
        Routing.RegisterRoute(nameof(AllPlaylists), typeof(AllPlaylists));

        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        Routing.RegisterRoute(nameof(TqlTutorialPage), typeof(TqlTutorialPage));
        Routing.RegisterRoute(nameof(SessionTransferView), typeof(SessionTransferView));
        Routing.RegisterRoute(nameof(SingleAlbumPage), typeof(SingleAlbumPage));
        Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));

        Routing.RegisterRoute(nameof(DuplicatesMgtWindow), typeof(DuplicatesMgtWindow));
    }


    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        //args.
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
    }
    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
    }


}