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
    private async void PickFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SelectSongFromFolderAndroid();
    }

    private static async void ReportIssueBtn_Clicked(object sender, EventArgs e)
    {
        string reportingLink = $"https://github.com/YBTopaz8/Dimmer-MAUI/issues/new";

        await Browser.Default.OpenAsync(reportingLink, BrowserLaunchMode.SystemPreferred);
    }

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }

    private async void ChangeFolder_Clicked(object sender, EventArgs e)
    {


        var selectedFolder = (string)((ImageButton)sender).CommandParameter;
        await MyViewModel.SelectSongFromFolderAndroid(selectedFolder);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.SelectSongFromFolderAndroid();
    }
    private async void ViewDevices_Clicked(object sender, EventArgs e)
    {

        await MyDevicesPopUp.ShowAsync();

    }

    private async void SelectDeviceChip_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {
        MainTabView.SelectedItemIndex = 4;

        await MyViewModel.SendMessage($"Pinged on {DeviceInfo.Current.Idiom} {DeviceInfo.Current.Platform}");
        MyDevicesPopUp.Close();
    }

    private void SwitchDeviceRecipient_Tap(object sender, System.ComponentModel.HandledEventArgs e)
    {

    }

    private void SendMsgBtn_Clicked(object sender, EventArgs e)
    {

    }
}