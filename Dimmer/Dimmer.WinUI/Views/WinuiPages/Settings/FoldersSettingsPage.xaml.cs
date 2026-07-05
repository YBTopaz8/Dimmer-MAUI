// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.Settings;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FoldersSettingsPage : Page
{
    public FoldersSettingsPage()
    {
        InitializeComponent();
    }
    BaseViewModelWin MyViewModel;

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var vm = e.Parameter as BaseViewModelWin ?? IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        if(vm is null)
        { 
            return;
        }
        
        MyViewModel = vm;

        MyViewModel.CurrentPageEnum = CurrentPage.FolderSettingsPage;
        
        this.DataContext = MyViewModel;

        MyViewModel.LoadFolderPaths();
    }
    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {

        var button = (Button)sender;
        var path = button.DataContext as string;
        MyViewModel.DeleteFolderPath(path);
    }

    private async void UpdateFolder_Click(object sender, RoutedEventArgs e)
    {

        var button = (Button)sender;
        var path = button.DataContext as string;
        //await MyViewModel.UpdateFolderPath(path);
    }


    private async void ReScanButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var path = button.DataContext as string;
        if (path is null) return;
        if (MyViewModel is null) return;

        await MyViewModel.ReScanMusicFolderByPassingToService(path);
    }


    private void MusicFoldersGrid_Loaded(object sender, RoutedEventArgs e)
    {

       

    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        AddFolder.IsEnabled = false;
       await MyViewModel?.AddMusicFolderViaPickerAsync();
        AddFolder.IsEnabled = true;
    }
}
