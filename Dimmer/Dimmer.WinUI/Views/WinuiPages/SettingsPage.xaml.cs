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

    SettingsViewModelWin MyViewModel { get; set; }

    string CurrentPageTQL = string.Empty;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current?.Services.GetService<SettingsViewModelWin>();
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
                
                break;
            case "LastFMBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                
                break;
            case "UtilsBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                
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
        //await MyViewModel.UpdateFolderPath(path);
    }


    private async void ReScanButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        var path = button.DataContext as string;
        if (path is null) return;
        if (MyViewModel is null) return;
        MyViewModel.BaseViewModelWin.ShowIndeterminateProgressBar();
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

    private void AppPreferences_Click(object sender, RoutedEventArgs e)
    {
        WizardFlipView.SelectedIndex = 1;   
    }

    private void MusicFoldersGrid_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.GetLibState();
        if(MyViewModel.IsLibraryEmpty)
        {
            AddMusicFolderTip.IsOpen = true;
        }
        MyViewModel.BaseViewModelWin.WhenPropertyChange(nameof(MyViewModel.BaseViewModelWin.FolderPaths), x => MyViewModel.BaseViewModelWin.FolderPaths)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(obsCol =>
            {
                if (obsCol.Count >= 1)
                {
                    if (AddMusicFolderTip.IsOpen)
                    {
                        AddMusicFolderTip.IsOpen = false;
                    }
                    if (MyViewModel.SearchResults.Count < 1)
                    {
                        AddMusicFolderTip.Subtitle = AddMusicFolderTip.Subtitle + " Or Rescan existing one";
                        AddMusicFolderTip.IsOpen = true;
                    }
                }
                else
                {

                }
            });
    }

    private void OptionLogBtn_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsAppLoadingCovers), v => MyViewModel.IsAppLoadingCovers)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isLoadingCovers =>
            {
                if (isLoadingCovers)
                {
                    OptionLogBtn.Content = "View Songs";
                    OptionLogBtn.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    OptionLogBtn.Click += OptionLogBtn_Click;
                }
                else
                {
                    OptionLogBtn.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    OptionLogBtn.Click -= OptionLogBtn_Click;
                }
            });
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsAppScanning), v => MyViewModel.IsAppScanning)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(IsAppScanning =>
            {
                if (IsAppScanning && !MyViewModel.IsLibraryEmpty)
                {
                    OptionLogBtn.Content = "View Songs";
                    OptionLogBtn.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    OptionLogBtn.Click += OptionLogBtn_Click;
                }
                else
                {
                    OptionLogBtn.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    OptionLogBtn.Click -= OptionLogBtn_Click;
                }
            });
    }

    private void OptionLogBtn_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.DescAdded());
        MyViewModel.BaseViewModelWin.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
    }

    private void AutoScrobbleToggle_Checked(object sender, RoutedEventArgs e)
    {
        MyViewModel.ToggleLastFMScrobbling(true);
        AutoScrobbleToggle.Content = "Scrobble On Complete";
    }

    private void AutoScrobbleToggle_Unchecked(object sender, RoutedEventArgs e)
    {

        AutoScrobbleToggle.Content = "No Scrobble On Complete";
        MyViewModel.ToggleLastFMScrobbling(false);
    }

    private void BackUpData_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.BackUpAppData();
        
    }

    private async void RestoreData_Click(object sender, RoutedEventArgs e)
    {
        await MyViewModel.BaseViewModelWin.LoadFolderToScanForBackUpFiles();
    }

    private void FetchLyricsData_Click(object sender, RoutedEventArgs e)
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        _ = Task.Run(async () => await MyViewModel.LoadAllSongsLyricsFromOnlineAsync(cts));
    }

    private void ReloadCoversCache_Click(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(()=>MyViewModel.EnsureAllCoverArtCachedForSongsAsync());
    }

    private async void LoadAlbumAndArtistInfoFromLastFM_Click(object sender, RoutedEventArgs e)
    {
      await MyViewModel.BaseViewModelWin.LoadAlbumAndArtistDetailsFromLastFM();
    }


}
