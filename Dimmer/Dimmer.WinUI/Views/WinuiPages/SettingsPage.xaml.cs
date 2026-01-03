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
            
            // Initialize toggle states from saved preferences
            InitializeSettingsState();
        }

    }

    private void InitializeSettingsState()
    {
        if (MyViewModel == null) return;
        
        var realm = MyViewModel.RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        
        if (appModel != null)
        {
            AutoScrobbleToggle.IsChecked = appModel.ScrobbleToLastFM;
            ThemeToggle.IsChecked = appModel.IsDarkModePreference;
            BackNavM4.IsChecked = appModel.AllowBackNavigationWithMouseFour;
            EnableMiniLyricsView.IsChecked = appModel.IsMiniLyricsViewEnabled;
            
            // Set preferred mini lyrics position
            if (!string.IsNullOrEmpty(appModel.PreferredMiniLyricsViewPosition))
            {
                PreferredPosition.Content = appModel.PreferredMiniLyricsViewPosition;
            }
            
            // Set preferred lyrics source
            if (!string.IsNullOrEmpty(appModel.PreferredLyricsSource))
            {
                PreferredLyricsSource.Content = appModel.PreferredLyricsSource;
            }
            
            // Set preferred lyrics format
            if (!string.IsNullOrEmpty(appModel.PreferredLyricsFormat))
            {
                PreferredLyricsFormat.Content = appModel.PreferredLyricsFormat;
            }
            
            // Set allow lyrics contribution
            if (!string.IsNullOrEmpty(appModel.AllowLyricsContribution))
            {
                AllLyricsContribute.Content = appModel.AllowLyricsContribution;
            }
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

    private void LastFMView_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 1;
        var send = (Button)sender;

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

    private void AutoScrobbleToggle_Click(object sender, RoutedEventArgs e)
    {
        ToggleButton toggle = (ToggleButton)sender;
        MyViewModel?.ToggleLastFMScrobbling(toggle.IsChecked ?? false);
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
