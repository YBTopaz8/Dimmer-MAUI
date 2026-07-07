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
public sealed partial class AppUtilsPage : Page
{
    public AppUtilsPage()
    {
        InitializeComponent();
    }
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        MyViewModel = IPlatformApplication.Current.Services.GetService<BaseViewModelWin>();
        DataContext = MyViewModel;
    }
    BaseViewModelWin MyViewModel;
    CancellationTokenSource? cts;
    private async void SyncLyrics_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
        cts = new();
        await MyViewModel.LoadAllSongsLyricsFromOnlineAsync(cts);
    }

    private async void SyncCovers_Click(object sender, RoutedEventArgs e)
    {
        await MyViewModel.EnsureAllCoverArtCachedForSongsAsync();
    }
}
