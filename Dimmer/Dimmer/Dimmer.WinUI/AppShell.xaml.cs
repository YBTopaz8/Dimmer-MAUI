using Dimmer.WinUI.Views.AlbumsPage;
using Dimmer.WinUI.Views.ArtistsSpace;
using Dimmer.WinUI.Views.ArtistsSpace.MAUI;
using Dimmer.WinUI.Views.SettingsSpace;

namespace Dimmer.WinUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();


        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(OnlinePageManagement), typeof(OnlinePageManagement));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AllAlbumsPage), typeof(AllAlbumsPage));
        Routing.RegisterRoute(nameof(SpecificArtistPage), typeof(SpecificArtistPage));

        MyViewModel= IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
    }


    public BaseViewModelWin MyViewModel { get; internal set; }
    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "Artists":
                MyViewModel.OpenArtistsWindow();
                break;


                break;
            default:
                break;
        }

    }

    private void SettingsChip_Clicked(object sender, EventArgs e)
    {
        MyViewModel.OpenSettingsWindow();
    }

}