using System.Threading.Tasks;

using Dimmer.WinUI.Views.ArtistsSpace.MAUI;

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

        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel= IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        await MyViewModel.Initialize();

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

    private async void SettingsChip_Clicked(object sender, EventArgs e)
    {
        
        MyViewModel.OpenSettingsWindow();
    }

}