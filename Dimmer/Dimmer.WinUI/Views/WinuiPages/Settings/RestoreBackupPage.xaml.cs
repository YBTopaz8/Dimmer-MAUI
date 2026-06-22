// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RestoreBackupPage : Page
{
    public RestoreBackupPage()
    {
        InitializeComponent();

     MyViewModel=   IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
    }
    BaseViewModelWin MyViewModel { get; set; }



    private async void PickBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        PickedUpBackupExpander.IsExpanded = true;
        await MyViewModel.PickFolderToRestoreAppDataAsync();
    }

    public static event EventHandler? IsPopupDismissedRequested;
    private async void ConfirmRestore_Click(object sender, RoutedEventArgs e)
    {
       await MyViewModel.RestoreCompleteDataAsync();
        if(MyViewModel.IsRestoreDone)
        {
            ClosePopup_Click(sender, e);
        }
    }

    private void ClosePopup_Click(object sender, RoutedEventArgs e)
    {
        IsPopupDismissedRequested?.Invoke(this, EventArgs.Empty);
    }
}
