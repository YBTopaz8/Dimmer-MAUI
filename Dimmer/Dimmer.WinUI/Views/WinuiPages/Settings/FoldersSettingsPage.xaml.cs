using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        this.DataContext = MyViewModel;
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

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel?.AddMusicFolderViaPickerAsync();

    }
}
