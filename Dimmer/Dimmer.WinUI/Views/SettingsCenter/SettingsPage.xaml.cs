using Dimmer.DimmerLive;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.DimmerLiveUI;

namespace Dimmer.WinUI.Views.SettingsCenter;

public partial class SettingsPage : ContentPage
{
    public BaseViewModelWin MyViewModel { get; internal set; }
    public SettingsPage(BaseViewModelWin vm)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel=vm;

    }

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

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await MyViewModel.CheckForAppUpdatesAsync();
#if Release
ViewAdminUpdate.IsVisible = false;
#endif

    }
    private async void OpenDimmerLiveSettingsChip_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DimmerLivePage));
    }
    private void SettingsChip_Clicked(object sender, EventArgs e)
    {

        var winMgr = IPlatformApplication.Current!.Services.GetService<IWindowManagerService>()!;

        winMgr.GetOrCreateUniqueWindow(() => new SettingWin(MyViewModel));
        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.LoadUserLastFMInfo();
        }
    }

    private void RescanFolderPath_Clicked(object sender, EventArgs e)
    {
        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        MyViewModel.RescanFolderPath(selectedFolder);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
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


    private void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {

    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.LoginToLastfm();
    }

    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
        this.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }

    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }

    private async void FindDupes_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LibSanityPage), true);
    }

    private async void PostUpdate_Clicked(object sender, EventArgs e)
    { 
        if(string.IsNullOrWhiteSpace(RelTitle.Text) || string.IsNullOrWhiteSpace(RelNotes.Text) || string.IsNullOrWhiteSpace(RelLink.Text))
        {
            await Shell.Current.DisplayAlert("Error", "Please fill in all fields before posting an update.", "OK");
            return;
        }

        await MyViewModel.PostAppUpdateAsync(RelTitle.Text, RelNotes.Text, RelLink.Text);

    }

    private void DownloadAndInstall_Clicked(object sender, EventArgs e)
    {
        
    }

    private async void RelLinkss_OnHyperLinkClicked(object sender, Indiko.Maui.Controls.Markdown.LinkEventArgs e)
    {
        var urll = e.Url;

        await Browser.Default.OpenAsync(urll, BrowserLaunchMode.SystemPreferred);
    }

}