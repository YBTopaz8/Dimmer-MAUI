using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml.Controls.Primitives;

using Grid = Microsoft.UI.Xaml.Controls.Grid;

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

    SettingsViewModel? MyViewModel { get; set; }

    string CurrentPageTQL = string.Empty;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current?.Services.GetService<SettingsViewModel>();
        var baseVM = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel != null)
        {
            //MyViewModel.CurrentWinUIPage = this;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = baseVM;
        }

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


    private void MusicFoldersView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 0;
    }

    private void LibraryHealthView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 1;
    }

    private void LastFMView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 2;
        var send = (Button)sender;

    }


    private void UtilsView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 3;
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
                MusicFoldersBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                LastFMBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                UtilsBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                break;
            case "LastFMBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                LastFMBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                UtilsBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                break;
            case "UtilsBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                LastFMBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                UtilsBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
                break;
            default:
                break;
        }

    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.AddMusicFolderViaPickerAsync();
    }

  
  

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ToggleButton send = (ToggleButton)sender;
        MyViewModel?.ToggleAppTheme();
        send.IsEnabled = MyViewModel?.IsDarkModeOn ?? false;
        var currentWinUITheme = Microsoft.UI.Xaml.Application.Current.RequestedTheme;
        // set new theme if different
        //Microsoft.UI.Xaml.Application.Current.RequestedTheme = currentWinUITheme == ApplicationTheme.Dark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        
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
        await MyViewModel.UpdateFolderPath(path);
    }


    private async void ReScanButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var path = button.DataContext as string;
        if (path is null) return;
        if (MyViewModel is null) return;
        await MyViewModel.ReScanMusicFolderByPassingToService(path);
    }

    private async void VerifyLibraryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MyViewModel is null) return;
        
        LibraryHealthStatus.Visibility = Visibility.Visible;
        LibraryHealthStatus.Text = "Verifying library...";
        LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
        
        try
        {
            var updatedCount = await MyViewModel.LibraryScannerService.VerifyExistingSongsAsync();
            
            if (updatedCount == 0)
            {
                LibraryHealthStatus.Text = "✓ All songs are available";
                LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            }
            else if (updatedCount > 0)
            {
                LibraryHealthStatus.Text = $"⚠ Found {updatedCount} unavailable song(s)";
                LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Orange);
            }
            else
            {
                LibraryHealthStatus.Text = "⚠ Verification failed";
                LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
        catch (Exception ex)
        {
            LibraryHealthStatus.Text = $"Error: {ex.Message}";
            LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    private async void CleanupUnavailableBtn_Click(object sender, RoutedEventArgs e)
    {
        if (MyViewModel is null) return;

        // Confirm deletion
        var dialog = new ContentDialog
        {
            Title = "Remove Unavailable Songs",
            Content = "This will permanently remove all unavailable songs from the database. This action cannot be undone. Continue?",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            LibraryHealthStatus.Visibility = Visibility.Visible;
            LibraryHealthStatus.Text = "Removing unavailable songs...";
            LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
            
            try
            {
                var removedCount = await MyViewModel.MusicDataServ.RemoveUnavailableSongsAsync();
                
                LibraryHealthStatus.Text = $"✓ Removed {removedCount} unavailable song(s)";
                LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
            }
            catch (Exception ex)
            {
                LibraryHealthStatus.Text = $"Error: {ex.Message}";
                LibraryHealthStatus.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
    }

    private async void DimmerSection_Click(object sender, RoutedEventArgs e)
    {
        var supNavTransInfo = new SuppressNavigationTransitionInfo();
        Type songDetailType = typeof(DimmerLivePage);
       
        FrameNavigationOptions navigationOptions = new FrameNavigationOptions
        {
            TransitionInfoOverride = supNavTransInfo,
            IsNavigationStackEnabled = true

        };

        if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {

                Frame?.NavigateToType(songDetailType, null, navigationOptions);
            });
        }
    }

   
}
