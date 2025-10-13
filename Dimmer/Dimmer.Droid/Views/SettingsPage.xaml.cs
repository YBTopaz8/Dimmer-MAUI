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
        MyViewModel.DeleteFolderPath(param);
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
            await MyViewModel.LoadUserLastFMInfo();
        }
    }

    private async void ViewUserAccountOnline(object sender, EventArgs e)
    {


        await Launcher.Default.OpenAsync(new Uri(MyViewModel.UserLocal.LastFMAccountInfo.Url));
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

        await MyViewModel.LoginToLastfm();
    }

    private void AcceptBtn_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.DimmerLiveViewModel.AcceptFriendRequestCommand.Execute(null);
    }

    private void RejectBtn_Clicked(object sender, EventArgs e)
    {
        //MyViewModel.DimmerLiveViewModel.RejectFriendRequestCommand.Execute(null);

    }
    private async void RefreshLyrics_Clicked(object sender, EventArgs e)
    {
        var res = await DisplayAlert("Refresh Lyrics", "This will process all songs in the library to update lyrics. Do you want to continue?", "Yes", "No");

        if (!res)
        {
            return; // User cancelled the operation
        }


        if (_isLyricsProcessing)
        {
            bool cancel = await DisplayAlert("Processing...", "Lyrics are already being processed. Cancel the current operation?", "Yes, Cancel", "No");
            if (cancel)
            {
                _lyricsCts?.Cancel();
            }
            return;
        }

        _isLyricsProcessing = true;
        MyProgressBar.IsVisible = true; // Show a progress bar
        MyProgressLabel.IsVisible = true; // Show a label



        _lyricsCts = new CancellationTokenSource();



        var progressReporter = new Progress<LyricsProcessingProgress>(progress =>
        {
            MyProgressBar.Progress = (double)progress.ProcessedCount / progress.TotalCount;
            MyProgressLabel.Text = $"Processing: {progress.CurrentFile} {Environment.NewLine}" +
            $"File {progress.ProcessedCount}/{progress.TotalCount}";
        });

        try
        {

            await MyViewModel.LoadSongDataAsync(progressReporter, _lyricsCts);
            await DisplayAlert("Complete", "Lyrics processing finished!", "OK");
        }
        catch (OperationCanceledException)
        {
            await DisplayAlert("Cancelled", "The operation was cancelled.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
        finally
        {
            _isLyricsProcessing = false;
            MyProgressBar.IsVisible = false;
            MyProgressLabel.IsVisible = false;
        }
    }

    private void RescanFolder_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var comParam = send.CommandParameter as string;

        if (comParam is null)
            return;

        MyViewModel.ReScanMusicFolderByPassingToServiceCommand.Execute(comParam);
    }

    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }
}