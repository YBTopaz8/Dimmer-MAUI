using AndroidX.Lifecycle;

using Dimmer.ViewModel;
using Dimmer.Views.Stats;

using Syncfusion.Maui.Toolkit.Chips;

using System.Threading.Tasks;

using Button = Microsoft.Maui.Controls.Button;
using ImageButton = Microsoft.Maui.Controls.ImageButton;

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

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel  = IPlatformApplication.Current.Services.GetService<BaseViewModelAnd>();
        if ((MyViewModel is null))
        {
            return;
        }
        this.BindingContext = MyViewModel;
        await MyViewModel.BaseVM.InitializeAllVMCoreComponentsAsync();
        
    }

    public BaseViewModelAnd MyViewModel { get; internal set; }
    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {

        var send = (SfChip)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "Artists":

                break;

            default:
                break;
        }

    }

   

    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.BaseVM.LoadUserLastFMInfo();
        }
    }

    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {
        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        //await  MyViewModel.BaseVM.AddMusicFolderAsync(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.BaseVM.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

    private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "0":
                break;
            case "1":
                break;
            default:

                break;
        }

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;


    private async void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {
        this.IsBusy=true;
        await Shell.Current.GoToAsync(nameof(SettingsPage));
        this.IsBusy=false;
    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.BaseVM.LoginToLastfm();
    }
    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
        this.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }

    private void FindDupes_Clicked(object sender, EventArgs e)
    {
        this.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }
}