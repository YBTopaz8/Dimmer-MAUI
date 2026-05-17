using CommunityToolkit.WinUI;
using Dimmer.DimmerLive.ParseStatics;
using Dimmer.WinUI.Views.WinuiPages.Settings;
using Dimmer.WinUI.Views.WinuiPages.SingleSongPage.SubPage;
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
    BaseViewModelWin BaseViewModel { get; set; }

    string CurrentPageTQL = string.Empty;
    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MyViewModel ??= IPlatformApplication.Current?.Services.GetService<SettingsViewModelWin>()!;
        BaseViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>()!;
        // The parameter passed from Frame.Navigate is in e.Parameter.
        // Cast it to your ViewModel type and set your properties.
        if (MyViewModel != null)
        {
            //MyViewModel.CurrentWinUIPage = this;
            // Now that the ViewModel is set, you can set the DataContext.
            this.DataContext = BaseViewModel;
        }

    }

    private void WizardFlipView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
    {
        var addedItems = e.AddedItems;
        var removedItems = e.RemovedItems;
        FrameworkElement addedGrid = (FrameworkElement)addedItems[0];
        FrameworkElement? removedGrid = removedItems.Count > 0 ? (FrameworkElement)removedItems[0]:null;
        string addedName = addedGrid.Name;
        //string? removedName = removedGrid?.Name;
        switch (addedName)
        {
            case "MusicFoldersBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Colors.DarkSlateBlue);
                
                break;
            case "LastFMBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Colors.Gray);
                
                break;
            case "UtilsBtn":
                MusicFoldersBtn.Background = new SolidColorBrush(Colors.Gray);
                
                break;
            default:
                break;
        }

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


    private void EnableMiniLyricsView_Checked(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Controls.CheckBox send = (Microsoft.UI.Xaml.Controls.CheckBox)sender;


        MyViewModel?.ToggleIsMiniLyricsViewEnableCommand.Execute(send.IsChecked);
    }

    private void PositionChange_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem send = (Microsoft.UI.Xaml.Controls.RadioMenuFlyoutItem)sender;
        string? position = send.Text?.ToString();
        if (!string.IsNullOrEmpty(position))
        {
            MyViewModel?.SetPreferredMiniLyricsViewPosition(position);
        }
    }

    private void EnableLyricsBtn_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var urlLink = "https://lrclib.net/";
                _ = Windows.System.Launcher.LaunchUriAsync(new Uri(urlLink));
    }

    private void EnableLyricsBtn_Checked(object sender, RoutedEventArgs e)
    {
        //LyricsExpander.IsExpanded = EnableLyricsBtn.IsChecked ?? false;
        
    }

    private void LrcLibSource_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("LrcLib");
        //PreferredLyricsSource.Content= "LrcLib";
    }

    private void SongFileOnly_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("SongFileOnly");
        //PreferredLyricsSource.Content = "SongFileOnly";

    }

    private void AllFormats_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel?.SetPreferredLyricsSource("AllFormats");
        //PreferredLyricsSource.Content = "AllFormats";
    }

    private void AllowLyricsContribution_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetAllowLyricsContribution(allow);

    }

    private void PlainFormat_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetPreferredLyricsFormat(allow);

    }

    private void SynchronizedFormat_Click(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem send = (RadioMenuFlyoutItem)sender;
        var allow = send.Text;
        MyViewModel?.SetPreferredLyricsFormat(allow);

    }

    private void WizardFlipView_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        

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

    



    private void OptionLogBtn_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.IsAppLoadingCovers), v => MyViewModel.IsAppLoadingCovers)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isLoadingCovers =>
            {
                if (isLoadingCovers)
                {
                    OptionLogBtn.Content = "View Songs";
                    OptionLogBtn.Visibility = WinUIVisibility.Visible;
                    OptionLogBtn.Click += OptionLogBtn_Click;
                }
                else
                {
                    OptionLogBtn.Visibility = WinUIVisibility.Collapsed;
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
                    OptionLogBtn.Visibility = WinUIVisibility.Visible;
                    OptionLogBtn.Click += OptionLogBtn_Click;
                }
                else
                {
                    OptionLogBtn.Visibility = WinUIVisibility.Collapsed;
                    OptionLogBtn.Click -= OptionLogBtn_Click;
                }
            });
    }

    private void OptionLogBtn_Click(object sender, RoutedEventArgs e)
    {
        MyViewModel.SearchToTQL(TQlStaticMethods.PresetQueries.DescAdded());
        MyViewModel.BaseViewModelWin.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
    }


    private async void BackUpData_Click(object sender, RoutedEventArgs e)
    {

        await MyViewModel.BaseViewModelWin.BackUpAppDataAsync();   
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


    private async void DownloadUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                // Opens the GitHub Release page in the default browser
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            }
        }
    }

  

    private Type pageType;
    private int previousSelectedIndex;
    public Type CurrentPageSelected => pageType;
    private void BackUpRestoreSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(RestoreBackupPage);
                break;
            case 1:
                break;
            case 2:
                //pageTy?pe = typeof(LyricsManualSyncPage);
                break;
            case 3:
                break;
            default:
                break;
        }
      



        var sliderNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0
            ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromBottom;
        
        ContentFrame.Navigate(pageType,MyViewModel,
            new SlideNavigationTransitionInfo { Effect = sliderNavigationTransitionEffect });
    }


    private void SettingsNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var SelectedNavViewItem = args.SelectedItem; 
        var SelectedNavViewItemKnown = args.SelectedItem as NavigationViewItem;
        if(SelectedNavViewItemKnown is null)return;
        var selContainer = args.SelectedItemContainer;
        switch(SelectedNavViewItemKnown.Tag?.ToString()?.ToLower())
        {
            case "folderspage":
                if(contentFrameSettings.CurrentSourcePageType != typeof(FoldersSettingsPage))
                    contentFrameSettings.Navigate(typeof(FoldersSettingsPage), MyViewModel);

                break;
            case "backuppage":
                if (contentFrameSettings.CurrentSourcePageType != typeof(RestoreBackupPage))
                    contentFrameSettings.Navigate(typeof(RestoreBackupPage), MyViewModel);

                break;
            default:
                break;
        }
    }

    private void AppPreferences_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BackupRestoreSection_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BackUpRestoreGrid_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void MusicFoldersView_Click(object sender, RoutedEventArgs e)
    {

    }

    private void SettingsNavView_Loaded(object sender, RoutedEventArgs e)
    {
        contentFrameSettings.Navigate(typeof(FoldersSettingsPage), MyViewModel);
    }
}
