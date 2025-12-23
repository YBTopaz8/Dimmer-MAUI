using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CloudDataPage : Page
{
    public SessionManagementViewModel ViewModel { get; set; }

    public CloudDataPage()
    {
        this.InitializeComponent();

        // Resolve the ViewModel from your DI Container / App.Services
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = IPlatformApplication.Current!.Services.GetService<SessionManagementViewModel>()!;
        ViewModel.OnPageNavigatedTo();
        this.DataContext = ViewModel; // Set DataContext for binding within DataTemplates
        this.Name = "RootPage"; // Helper for ElementName binding
        await ViewModel.LoadBackupsAsync();
    }

    private async void RestorebackBtn_Click(object sender, RoutedEventArgs e)
    {
        var send = (Button)sender;
        var objId = send.CommandParameter as string;
        await ViewModel.RestoreBackupAsync(objId);
    }
}