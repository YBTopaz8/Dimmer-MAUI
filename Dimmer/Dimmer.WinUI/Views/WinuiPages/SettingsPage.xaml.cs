using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using CommunityToolkit.WinUI;

using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using Button = Microsoft.UI.Xaml.Controls.Button;
using Colors = Microsoft.UI.Colors;
using Grid = Microsoft.UI.Xaml.Controls.Grid;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views.WinuiPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }


    private readonly Microsoft.UI.Composition.Compositor _compositor;
    private readonly SongTransitionAnimation _userPrefAnim;
    private SongModelView? _storedSong;

    SettingsViewModel? MyViewModel { get; set; }

    string CurrentPageTQL = string.Empty;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current?.Services.GetService<SettingsViewModel>();
        // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel != null)
        {
            //MyViewModel.CurrentWinUIPage = this;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = MyViewModel;
        }

    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (WizardFlipView.SelectedIndex < WizardFlipView.Items.Count - 1)
        {
            WizardFlipView.SelectedIndex += 1;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (WizardFlipView.SelectedIndex > 0)
        {
            WizardFlipView.SelectedIndex -= 1;
        }
    }

    private void WizardFlipView_SelectionChanged_1(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {

    }

    private void MusicFoldersView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 0;
    }

    private void LastFMView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 1;
        var send = (ToggleButton)sender;

    }


    private void UtilsView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 2;
    }

    private void WizardFlipView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var addedItems = e.AddedItems;
        var removedItems = e.RemovedItems;
        Grid addedGrid = (Grid)addedItems[0];
        Grid? removedGrid = removedItems.Count > 0 ? (Grid)removedItems[0]:null;
        string addedName = addedGrid.Name;
        //string? removedName = removedGrid?.Name;
        switch (addedName)
        {
            case "MusicFoldersBtn":
                MusicFoldersBtn.IsEnabled = false;
                MusicFoldersBtn.IsChecked = true;
                LastFMBtn.IsEnabled = true;
                UtilsBtn.IsEnabled = true;
                break;
            case "LastFMBtn":
                MusicFoldersBtn.IsEnabled = true;
                LastFMBtn.IsEnabled = false;
                LastFMBtn.IsChecked = true;
                UtilsBtn.IsEnabled = true;
                break;
            case "UtilsBtn":
                MusicFoldersBtn.IsEnabled = true;
                LastFMBtn.IsEnabled = true;
                UtilsBtn.IsEnabled = false;
                UtilsBtn.IsChecked = true;
                break;
            default:
                break;
        }

    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.AddMusicFolderViaPickerAsync();
    }

    private void DeletePathBtn_Click(object sender, RoutedEventArgs e)
    {
        Button btn = (Button)sender;
        var folderPath = ((Button)sender).DataContext as string;
        if (folderPath is null) return;

        MyViewModel?.DeleteFolderPathCommand.Execute(folderPath);
    }

    private void ChangePathBtn_Click(object sender, RoutedEventArgs e)
    {
        
    }

    private void FolderPathView_Loaded(object sender, RoutedEventArgs e)
    {
        
        if (MyViewModel?.FolderPaths.Count==0)
        {
            FolderPathView.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        
    }

    private void LogoutLastFM_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.LogoutFromLastfmCommand.Execute(null);
    }

    private void LoginLastFM_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.LoginToLastfmCommand.Execute(null);
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ToggleButton send = (ToggleButton)sender;
        MyViewModel?.ToggleAppTheme();
        send.IsEnabled = MyViewModel?.IsDarkModeOn ?? false;
        var currentWinUITheme = Microsoft.UI.Xaml.Application.Current.RequestedTheme;
        // set new theme if different
        Microsoft.UI.Xaml.Application.Current.RequestedTheme = currentWinUITheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        
    }

    private void BackNavM4_Click(object sender, RoutedEventArgs e)
    {

        MyViewModel?.AllowBackNavigationWithMouseFour(BackNavM4.IsChecked);
    }

    private void EnableMiniLyricsView_Checked(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Controls.CheckBox send = (Microsoft.UI.Xaml.Controls.CheckBox)sender;

        MiniLyricsExpander.IsExpanded = !MiniLyricsExpander.IsExpanded;
        MyViewModel?.ToggleIsMiniLyricsViewEnableCommand.Execute(send.IsChecked);
    }
    private void SetPreferredMiniLyricsViewPosition_Click(object sender, RoutedEventArgs e)
    {
       
    }

    private void PreferredPosition_TopRight_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;

    }

    private void TopLeftPosition_Click(object sender, RoutedEventArgs e)
    {

    }

    private void PositionChange_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem send = (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem)sender;
        string? position = send.Text?.ToString();
        if (!string.IsNullOrEmpty(position))
        {
            MyViewModel?.SetPreferredMiniLyricsViewPosition(position);
            PreferredPosition.Content = send.Text;
        }
    }

    private void EnableLyricsBtn_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var urlLink = "https://lrclib.net/";
                _ = Windows.System.Launcher.LaunchUriAsync(new Uri(urlLink));
    }

    private void EnableLyricsBtn_Checked(object sender, RoutedEventArgs e)
    {
        LyricsExpander.IsExpanded = EnableLyricsBtn.IsChecked ?? false;
        
    }

    private void LrcLibSource_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("LrcLib");
        PreferredLyricsSource.Content= "LrcLib";
    }

    private void SongFileOnly_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("SongFileOnly");
        PreferredLyricsSource.Content = "SongFileOnly";

    }

    private void AllFormats_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("AllFormats");
        PreferredLyricsSource.Content = "AllFormats";
    }

    private void AllowLyricsContribution_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetAllowLyricsContribution(allow);
        AllLyricsContribute.Content = send.Text;
    }

    private void PlainFormat_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetPreferredLyricsFormat(allow);
        PreferredLyricsFormat.Content = send.Text;
    }

    private void SynchronizedFormat_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetPreferredLyricsFormat(allow);
        PreferredLyricsFormat.Content = send.Text;
    }

    private void WizardFlipView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var pointerProps = e.GetCurrentPoint(WizardFlipView).Properties;
        if(pointerProps.IsXButton1Pressed)
        {
            if (WizardFlipView.SelectedIndex > 0)
            {
                WizardFlipView.SelectedIndex -= 1;
            }
        }
        else if (pointerProps.IsXButton2Pressed)
        {
            if (WizardFlipView.SelectedIndex < WizardFlipView.Items.Count - 1)
            {
                WizardFlipView.SelectedIndex += 1;
            }
        }
    }
}
