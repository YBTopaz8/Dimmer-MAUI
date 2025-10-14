using Dimmer.DimmerLive;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.DimmerLiveUI;

using System.Threading.Tasks;

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



        Progress<LyricsProcessingProgress>? progressReporter = new Progress<LyricsProcessingProgress>(progress =>
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

    protected override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel.ReloadFolderPathsCommand.Execute(null);
        //await MyViewModel.CheckForAppUpdatesAsync();
#if Release
ViewAdminUpdate.IsVisible = false;
#endif
        MyViewModel.LoadLastFMSession();

    }
    private async void OpenDimmerLiveSettingsChip_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DimmerLivePage));
    }
    private void SettingsChip_Clicked(object sender, EventArgs e)
    {

        var winMgr = IPlatformApplication.Current!.Services.GetService<IMauiWindowManagerService>()!;

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

    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        if (string.IsNullOrEmpty(param)) return;
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

    private void RescanFolder_Clicked(object sender, EventArgs e)
    {
        var send = (SfChip)sender;
        var comParam = send.CommandParameter as string;

        if (comParam is null)
            return;

       MyViewModel.ReScanMusicFolderByPassingToServiceCommand.Execute(comParam);
    }
}