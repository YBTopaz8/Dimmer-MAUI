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
using Microsoft.UI.Xaml.Controls.Primitives;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerLivePage : Page
{
    LoginViewModel MyViewModel;
    BaseViewModelWin uiViewModel;
    public DimmerLivePage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current?.Services.GetService<LoginViewModel>();
        uiViewModel= IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel != null)
        {
            //MyViewModel.CurrentWinUIPage = this;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = uiViewModel;
        }

    }
}
