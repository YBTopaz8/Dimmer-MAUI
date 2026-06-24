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

namespace Dimmer.WinUI.Views.WinuiPages.PlaylistSection;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AllPlaylistsPage : Page
{
    public AllPlaylistsPage()
    {
        InitializeComponent();
    }

    private BaseViewModelWin MyViewModel { get; set; }

    protected async override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;

        DataContext = MyViewModel;

        await Task.Delay(1000);

        MyViewModel.SetupPlaylistPipeline();
    }

    private void PlaylistsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        var pl = e.ClickedItem as PlaylistModelView;
        MyViewModel.SelectedPlaylist = pl;

        MyViewModel.NavigateToAnyPageOfGivenType(typeof(SinglePlaylistPage));

    }
}
