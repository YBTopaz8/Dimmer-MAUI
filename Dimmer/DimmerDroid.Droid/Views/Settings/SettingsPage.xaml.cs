global using DevExpress.Android.Editors;
using DevExpress.Maui.Core;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace Dimmer.Views.Settings;

public partial class SettingsPage : ContentPage
{
	public SettingsPage(BaseViewModelAnd viewModelAnd)
    {
        InitializeComponent();
        BindingContext = viewModelAnd;
        MyViewModel = viewModelAnd;
    }

    BaseViewModelAnd MyViewModel { get; }

    private void RemoveFolderBtn_Clicked(object sender, HandledEventArgs e)
    {
        var chip = (Chip)sender;
        var folderPath = (string)chip.Text;
        MyViewModel.DeleteFolderPath(folderPath);
    }

    private async void BackupDeviceBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.BackUpAppDataAsync();
    }

    private async void RestoreBackupDeviceBtn_Clicked(object sender, EventArgs e)
    {
        EventsExpander.IsExpanded = true;
        await  MyViewModel.PickFolderToRestoreAppDataAsync();

    }

    private void LastFMHeaderChip_Tap(object sender, HandledEventArgs e)
    {
        LastFmExpander.IsExpanded = !LastFmExpander.IsExpanded;
    }

    private void ConnectToLastFM_Clicked(object sender, EventArgs e)
    {
        var send = (DXButton)sender;
        if (send is null) return;
        if (MyViewModel is null) return;


            BaseViewModel.LastFMName = LastFMUname.Text;
            //LoginLastFMBtn.IsEnabled = false;
            MyViewModel?.LoginToLastfmCommand.Execute(null);

    }

    private async void ConfirmRestoreBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.RestoreCompleteDataAsync();
    }

    private async void GoBackBtn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }


    private void FetchLyricsData_Click(object sender, EventArgs e)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        _ = Task.Run(async () => await MyViewModel.LoadAllSongsLyricsFromOnlineAsync(cts));
    }


    private async void OpenFolderScannerBtn_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }
}