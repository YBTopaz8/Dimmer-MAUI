using Dimmer.DimmerLive.Models;
using LiveChartsCore.Kernel;
using Microsoft.Maui.Platform;
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

namespace Dimmer.WinUI.Views.WinuiPages.DimmerLive;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class RemoteControlPage : Page
{
    private UserDeviceSession? device;

    public RemoteControlPage()
    {
        InitializeComponent();
    }

    public LoginViewModelWin MyViewModel { get; private set; }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var param = e.Parameter as Dictionary<string, object>;
        var vm = param?.GetValueOrDefault("ViewModel") as LoginViewModelWin;
        var deviceID = param?.GetValueOrDefault("deviceID") as string;

        if (vm is null)
            return;
        MyViewModel = vm;
        
        DataContext = MyViewModel.SessionMgtVM;

      
    }

    private async void OnSendCommandClicked(object sender, RoutedEventArgs e)
    {
        Button send = (Button)sender;
        var commandParameter = send.CommandParameter as string;

        switch (commandParameter)
        {
            case "Play":
               await MyViewModel.SessionMgtVM.SendDeviceCommand(commandParameter);
                break;
            default:
                break;
        }
    }

    private async void GetFavs_Click(object sender, RoutedEventArgs e)
    {
        Button send = (Button)sender;
        var commandParameter = send.Name as string;

        await MyViewModel.SessionMgtVM.SendDeviceCommand(commandParameter);

    }

    private void GetAllSongs_Click(object sender, RoutedEventArgs e)
    {

    }

    private void GetPlayBackQueue_Click(object sender, RoutedEventArgs e)
    {

    }
}
