using Dimmer.WinUI.Views.WinuiPages.AlbumSection;
using Dimmer.WinUI.Views.WinuiPages.Artist;
using Dimmer.WinUI.Views.WinuiPages.LastFMSection;
using Hqub.Lastfm.Entities;
using Microsoft.UI.Composition.SystemBackdrops;
using Border = Microsoft.UI.Xaml.Controls.Border;
using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;
using Visibility = Microsoft.UI.Xaml.Visibility;
using Window = Microsoft.UI.Xaml.Window;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerWin : Window
{
    IWinUIWindowMgrService? WinUIWindowsMgr;
    public DimmerWin()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>()!;
        WinUIWindowsMgr = IPlatformApplication.Current?.Services.GetService<IWinUIWindowMgrService>();
        MyViewModel?.MainWindow = this;
        MainGrid.DataContext = MyViewModel;

        this.SystemBackdrop = new Microsoft.UI.Xaml.Media.DesktopAcrylicBackdrop();
        var appWin = PlatUtils.GetAppWindow(this);


        _compositorMainGrid = ElementCompositionPreview.GetElementVisual(MainGrid).Compositor;

#if DEBUG
        this.Title = $"{MyViewModel?.AppTitle} Debug {BaseViewModel.CurrentAppVersion} {BaseViewModel.CurrentAppStage}";
#elif RELEASE
        this.Title = $"{MyViewModel?.AppTitle} {BaseViewModel.CurrentAppVersion} {BaseViewModel.CurrentAppStage}";
#endif
    }

    /// <summary>
    /// Navigates to page. and Snaps MAUI Panel to WinUI
    /// </summary>
    /// <param name="pageType"></param>
    /// <param name="OptionalParameter"></param>
    public async void NavigateToPage(Type pageType, object? OptionalParameter = null)
    {
        if (MyViewModel is not null && OptionalParameter is null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                if (pageType != ContentFrame.CurrentSourcePageType)
                    ContentFrame.Navigate(pageType, MyViewModel);

            });
            MyViewModel.DimmerMultiWindowCoordinator?.SnapAllToHomeAsync();

        }
        if (OptionalParameter is not null)
        {
            List<object> parameters = new();
            parameters.Add(OptionalParameter);
        }
    }
    public BaseViewModelWin MyViewModel { get; internal set; }
    private void DimmerWindowClosed(object sender, WindowEventArgs args)
    {
        MyViewModel.MainWindow = null;
        WinUIWindowsMgr?.UntrackWindow(this);
        this.Closed -= DimmerWindowClosed;

    }
    public void LoadWindowAndPassVM(BaseViewModelWin baseViewModelWin)
    {
        this.MyViewModel ??= baseViewModelWin;

    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        //MyViewModel.SetCoreWindow(this.CoreWindow);
        if (m_configurationSource != null)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }
        if (args.WindowActivationState == WindowActivationState.Deactivated || MyViewModel == null)
        {
            return;
        }
        if (_isDialogActive)
            return;

        var typee = LastFMViewModel.WindowActivationRequestTypeStatic;
        if (typee == "Confirm LastFM")
        {
            _isDialogActive = true;
            try
            {
                await MyViewModel.CheckToCompleteActivation(typee);
            }
            finally
            {
                // Ensure the flag is reset even if an error occurs
                _isDialogActive = false;
            }
        }
    }
    private bool _isDialogActive = false;
    private readonly Microsoft.UI.Composition.Compositor _compositorMainGrid;

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {

        Debug.WriteLine($"New window size: {args.Size.Width} x {args.Size.Height}");
    }


    SystemBackdropConfiguration m_configurationSource;

    private void DimmerProgressBar_Loaded(object sender, RoutedEventArgs e)
    {

        DimmerProgressBar.Visibility = Visibility.Collapsed;
        MyViewModel!.DimmerProgressBarView = (ProgressBar)sender;
    }

    private void DimmerStatusTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel!.DimmerStatusTextBlockView = (TextBlock)sender;
    }

    private void ClearStatus_Click(object sender, RoutedEventArgs e)
    {
        DimmerStatusPanel.Visibility = Visibility.Collapsed;
    }


    private void DimmerStatusPanel_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.DimmerStatusPanel = DimmerStatusPanel;
    }

    private void MainGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        var pressedKey = e.Key;
        if (pressedKey == Windows.System.VirtualKey.Escape)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }
    }


    private void SmokeGrid_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.NowPlayingView = SmokeGrid;
        SmokeGrid.SetBaseViewModelWin(MyViewModel);

        AnimationHelper.TryStart(
            SmokeGrid, null,
            AnimationHelper.Key_ToViewQueue
            );

    }
    private void SmokeGrid_DismissRequested(object sender, EventArgs e)
    {

        AnimationHelper.Prepare(AnimationHelper.Key_ToViewQueue, SmokeGrid);

        MyViewModel.ProcessNowPlayingQueueDismiss();

        //    // 2. Hmyvide the Detail View
        SmokeGrid.Visibility = Visibility.Collapsed;
    }



    private void nvSample_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        FrameNavigationOptions navOptions = new FrameNavigationOptions();
        navOptions.TransitionInfoOverride = args.RecommendedNavigationTransitionInfo;
        if(sender.PaneDisplayMode == NavigationViewPaneDisplayMode.Top)
        {
            navOptions.IsNavigationStackEnabled = false;
        }
        Type? pageType=null;
        if((string)args.InvokedItemContainer.Name == "AllSongsItem"!)
        {
            pageType = typeof(AllSongsListPage);
        }
        if((string)args.InvokedItemContainer.Name == "ArtistsItem"!)
        {
            pageType = typeof(ArtistsOverViewPage);
        }
        if((string)args.InvokedItemContainer.Name == "ViewQueueItem"!)
        {
            MyViewModel.ViewQueueFromAllSongsPageGivenPage();
        }
        if(pageType is not null)
            NavigateToPage(pageType, null);
    }

    private void nvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {

    }

    private void nvSample_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void nvSample_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if(ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
    }

    private void nvSample_Tapped_1(object sender, TappedRoutedEventArgs e)
    {

    }

    //private void ArtistsItem_Loaded(object sender, RoutedEventArgs e)
    //{
    //    var artistsCount =
    //        MyViewModel.ArtistsCollection.Count;
    //    ArtistsLabelView.Text = $"Artists ({artistsCount})";
    //}

    private void SearchAutoSuggestBox_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
    {

    }

    //private void SearchAutoSuggestBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs e)
    //{

    //    MyViewModel.SearchToTQL(SearchTextBox.Text);

       
    //}

    private void LyricsChip_Tap(object sender, RoutedEventArgs e)
    {

    }

    private void PlaybackChip_TapPressed(object sender, RoutedEventArgs e)
    {

    }

    private void NowPlayingHighlightBtn_TapPressed(object sender, RoutedEventArgs e)
    {

    }

    private void RemoveSongFromQueueBtn_TapPressed(object sender, RoutedEventArgs e)
    {

    }

    private void AddSongToFav_Tap(object sender, RoutedEventArgs e)
    {

    }

    private void PlaySongInQueue_Tap(object sender, RoutedEventArgs e)
    {

    }

    private void ScrollToInPlayBackQueue_Tap(object sender, RoutedEventArgs e)
    {

    }

    private void SongTitleTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
    {

    }

    private void PlayPauseBtn_Loaded(object sender, RoutedEventArgs e)
    {
        


    }



    private static void ApplyCustomShadow(Border card)
    {
        // Get the compositor
        var compositor = ElementCompositionPreview.GetElementVisual(card).Compositor;

        // Create the drop shadow
        var dropShadow = compositor.CreateDropShadow();
        dropShadow.Color = Colors.Black;
        dropShadow.BlurRadius = 12.0f;
        dropShadow.Offset = new System.Numerics.Vector3(5, 5, 0);

        // Create a visual for the card
        var visual = compositor.CreateSpriteVisual();
        visual.Size = new System.Numerics.Vector2((float)(card.Width), (float)card.Height); // Match card size
        visual.Shadow = dropShadow;

        // Apply it to the card
        ElementCompositionPreview.SetElementChildVisual(card, visual);
    }

    int previousSelectedIndex;
    private void DimmerAppSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        SelectorBarItem selectedItem = sender.SelectedItem;
        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        Type? pageType=null;

        switch (currentSelectedIndex)
        {
            case 0:
                pageType = typeof(AllSongsListPage);
                break;
            case 1:
                pageType = typeof(AllArtistsPage);
                break;
            case 2:
                pageType = typeof(AllAlbumsPage);
                break;
            case 3:
                pageType = typeof(LastFmPage);
                break;
            case 4:
                pageType = typeof(SettingsPage);
                break;

          

        }
        if (pageType is null)
            return;
        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;
       
        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;

    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var navPageType = e.SourcePageType;
        if(navPageType is null) return;

        if(navPageType == typeof(AllSongsListPage))
        {
            DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[0];
        }
        else if (navPageType == typeof(AllArtistsPage))
        {
            DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[1];
        }
        else if (navPageType == typeof(AllAlbumsPage))
        {
            DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[2];
        }
        else if (navPageType == typeof(LastFmPage))
        {
            DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[3];
        }
        else if (navPageType == typeof(SettingsPage))
        {
            DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[4];
        }
    }
}