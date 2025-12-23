using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

using Dimmer.ViewModel;
using Dimmer.ViewModel.DimmerLiveVM;
using Dimmer.WinUI.ViewModel.DimmerLiveWin;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using NavigationEventArgs = Microsoft.UI.Xaml.Navigation.NavigationEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LoginPage : Page
{
    public LoginViewModelWin ViewModel { get; set; }
    public LoginPage()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = IPlatformApplication.Current!.Services.GetService<LoginViewModelWin>()!;
        this.DataContext = ViewModel; // Set DataContext for binding within DataTemplates
        this.Name = "RootPage"; // Helper for ElementName binding

    }

    private async void LogIn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoginAsync();
        if (ViewModel.CurrentUserOnline is not null && ViewModel.CurrentUserOnline.IsAuthenticated)
        {
            ViewModel.NavigateToProfilePage();
        }
    }
}
