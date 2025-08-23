using Java.Net;

using Syncfusion.Maui.Toolkit.Chips;

using ImageButton = Microsoft.Maui.Controls.ImageButton;

namespace Dimmer.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(BaseViewModelAnd vm)
    {
        InitializeComponent();
        this.MyViewModel = vm;
        BindingContext = vm;
    }
    BaseViewModelAnd MyViewModel { get; }


    private static async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        string reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.BaseVM.DeleteFolderPath(param);
    }

    //private async void ViewUserAccountOnline(object sender, EventArgs e)
    //{


    //    var selectedFolder = (string)((DXButton)sender).CommandParameter;
    //    await MyViewModel.AddMusicFolderViaPickerAsync(selectedFolder);
    //}
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        //await MyViewModel.InitializeDimmerLiveData();
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }
 
    private void SwitchDeviceRecipient_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void SendMsgBtn_Clicked(object sender, EventArgs e)
    {

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


    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.BaseVM.LoadUserLastFMInfo();
        }
    }

    private async void ViewUserAccountOnline(object sender, EventArgs e)
    {


        await Launcher.Default.OpenAsync(new Uri(MyViewModel.BaseVM.UserLocal.LastFMAccountInfo.Url));
    }
    private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Microsoft.Maui.Controls.Button)sender;
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

        await MyViewModel.BaseVM.LoginToLastfm();
    }

    private void AcceptBtn_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.DimmerLiveViewModel.AcceptFriendRequestCommand.Execute(null);
    }

    private void RejectBtn_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.DimmerLiveViewModel.RejectFriendRequestCommand.Execute(null);

    }

    private async void RescanLyrics_Clicked(object sender, EventArgs e)
    {
       await MyViewModel.LoadSongDataAsync(null, _lyricsCts);
    }
}