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
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage.Stats;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages.SingleSongPage;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SongStatsContainerPage : Page
{
    public SongStatsContainerPage()
    {
        InitializeComponent();
    }
    private SongModelView _currentSong;
    private void Page_Loaded(object sender, RoutedEventArgs e)
    {

    }
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Pass the Song Model or ID from the previous page
        if (e.Parameter is SongModelView song)
        {
            _currentSong = song;
            // Default to Overview
            StatsNavView.SelectedItem = StatsNavView.MenuItems[0];
        }
    }

    private void StatsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag.ToString())
            {
                case "Overview":
                    StatsContentFrame.Navigate(typeof(SongOverviewStatsPage), _currentSong);
                    break;
                case "Album":
                    StatsContentFrame.Navigate(typeof(SongAlbumStatsPage), _currentSong);
                    break;
                case "Artist":
                    StatsContentFrame.Navigate(typeof(SongArtistStatsPage), _currentSong);
                    break;
                case "Journey":
                    StatsContentFrame.Navigate(typeof(SongJourneyPage), _currentSong);
                    break;
            }
        }
    }
}
