// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using DevWinUI;

namespace Dimmer.WinUI.Views.WinuiPages.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class BackupPage : Page
{
    public BackupPage()
    {
        InitializeComponent();

     MyViewModel=   IPlatformApplication.Current!.Services.GetService<SettingsViewModelWin>()!;
    }
    SettingsViewModelWin MyViewModel { get; set; }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        this.DataContext = MyViewModel;
        MyViewModel.LoadApplicationDataForBackUpPage();
    }

    private async void BackUpDataBtn_Click(object sender, RoutedEventArgs e)
    {
       
        await MyViewModel.BackUpAppDataAsync();
        MyViewModel.WhenPropertyChanged(nameof(MyViewModel.MyBackupResult),v=>MyViewModel.MyBackupResult)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe (async res =>
            {
                if (res is not null)
                {
                    if (res.IsBackUpComplete)
                    {
                        StatusInfoBar.Visibility= Microsoft.UI.Xaml.Visibility.Visible;
                        await Task.Delay(3000);

                        StatusInfoBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    }
                    else
                    {
                        StatusInfoBar.Content = "BackUp Failed. Please check the logs for more information.";
                        StatusInfoBar.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        await Task.Delay(3000);
                        StatusInfoBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

                    }
                    res = null;
                }
            });
    }

    private void StatusInfoBar_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }
}
