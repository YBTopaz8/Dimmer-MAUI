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
using Button = Microsoft.UI.Xaml.Controls.Button;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ControlPanelWindow : Microsoft.UI.Xaml.Window
{
    DimmerMultiWindowCoordinator _coordinator;
    BaseViewModelWin MyViewModel;
    public ControlPanelWindow( BaseViewModelWin myViewModel)
    {
        InitializeComponent();
        _coordinator = myViewModel.DimmerMultiWindowCoordinator;
        MyViewModel = myViewModel;
        MyViewModel.AllWindows= _coordinator.Windows.ToObservableCollection();

    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        _coordinator.SaveAll();
    }

    private void Focus_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WindowEntry entry)
            _coordinator.BringToFront(entry.Window);
    }

    private void Snap_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WindowEntry entry)
        {
            var home = _coordinator.Windows.FirstOrDefault()?.Window;
            if (home != null)
                WindowDockManager.SnapHomeWindow(home, _coordinator.Windows.Select(x => x.Window));
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WindowEntry entry)
            entry.Window.Close();
    }
}
