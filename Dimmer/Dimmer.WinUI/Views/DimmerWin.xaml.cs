using DevWinUI;
using Dimmer.WinUI.Views.CustomViews.WinuiViews;
using Dimmer.WinUI.Views.WinuiPages.AlbumSection;
using Dimmer.WinUI.Views.WinuiPages.Artist;
using Dimmer.WinUI.Views.WinuiPages.LastFMSection;
using Dimmer.WinUI.Views.WinuiPages.Utilities;
using Hqub.Lastfm.Entities;
using Microsoft.UI.Composition.SystemBackdrops;
using static Dimmer.DimmerSearch.TQlStaticMethods;
using Border = Microsoft.UI.Xaml.Controls.Border;
using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;
using TextBox = Microsoft.UI.Xaml.Controls.TextBox;
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

        var icon = WindowHelper.GetWindowIcon(PlatUtils.DimmerHandle);
        uint iconId = 123;
        var trayIcon = new SystemTrayIcon(iconId, icon, "Open Dimmer");
        trayIcon.LeftClick += TrayIcon_LeftClick;
        trayIcon.RightClick += TrayIcon_RightClick;
        trayIcon.IsVisible = true;


    }

    /// <summary>
    /// Navigates to page. and Snaps MAUI Panel to WinUI
    /// </summary>
    /// <param name="pageType"></param>
    /// <param name="OptionalParameter"></param>
    public async void NavigateToPage(Type pageType, IDictionary<string, object>? OptionalParameter = null)
    {
        if (MyViewModel is not null && OptionalParameter is null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                if (pageType != ContentFrame.CurrentSourcePageType)
                    ContentFrame.Navigate(pageType, MyViewModel);

            });
            //MyViewModel.DimmerMultiWindowCoordinator?.SnapAllToHomeAsync();

        }
        if (OptionalParameter is not null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                if (pageType != ContentFrame.CurrentSourcePageType)
                    ContentFrame.Navigate(pageType, OptionalParameter);

            });
        }
    }
    public BaseViewModelWin MyViewModel { get; internal set; }
    private void DimmerWindowClosed(object sender, WindowEventArgs args)
    {
        //if (SortByWithTQL.Flyout is MenuFlyout fly)
        //{
        //    foreach (var sub in fly.Items.OfType<MenuFlyoutSubItem>())
        //    {
        //        foreach (var item in sub.Items.OfType<MenuFlyoutItem>())
        //            item.RemoveClick();
        //    }
        //}
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

    private void TrayIcon_RightClick(SystemTrayIcon sender, SystemTrayIconEventArgs args)
    {

    }

    private void TrayIcon_LeftClick(SystemTrayIcon sender, SystemTrayIconEventArgs args)
    {
        WindowHelper.ShowWindow(this);
        //throw new NotImplementedException();
    }

    private bool _isDialogActive = false;
    private readonly Microsoft.UI.Composition.Compositor _compositorMainGrid;

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {

        Debug.WriteLine($"New window size: {args.Size.Width} x {args.Size.Height}");
    }


    SystemBackdropConfiguration m_configurationSource;

    

    private void DimmerStatusTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel!.DimmerStatusTextBlockView = (TextBlock)sender;
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


    //private void SmokeGrid_Loaded(object sender, RoutedEventArgs e)
    //{
    //    MyViewModel.NowPlayingView = SmokeGrid;
    //    SmokeGrid.SetBaseViewModelWin(MyViewModel);

    //    AnimationHelper.TryStart(
    //        SmokeGrid, null,
    //        AnimationHelper.Key_ToViewQueue
    //        );

    //}
    //private void SmokeGrid_DismissRequested(object sender, EventArgs e)
    //{

    //    AnimationHelper.Prepare(AnimationHelper.Key_ToViewQueue, SmokeGrid);

    //    MyViewModel.ProcessNowPlayingQueueDismiss();

    //    //    // 2. Hmyvide the Detail View
    //    SmokeGrid.Visibility = Visibility.Collapsed;
    //}



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
     
        if(pageType is not null)
            NavigateToPage(pageType, null);
    }






    private void nvSample_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if(ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
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
                pageType = typeof(DimmerToolKit);
                break;
            case 5:
                pageType = typeof(SettingsPage);
                break;

          

        }
        if (pageType is null)
            return;
        var slideNavigationTransitionEffect = currentSelectedIndex - previousSelectedIndex > 0 ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;
       
        ContentFrame.Navigate(pageType, MyViewModel, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        previousSelectedIndex = currentSelectedIndex;

    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var navPageType = e.SourcePageType;
        if(navPageType is null) return;

        if (navPageType == typeof(AllSongsListPage))
        {
            
            ScrollToCurrentSong.Visibility = Visibility.Visible;
        }
        else
        {
            ScrollToCurrentSong.Visibility = Visibility.Collapsed;

        }

        Debug.WriteLine(e.Uri);
        ContentDivider.Content = e.Uri;
        //else if (navPageType == typeof(AllArtistsPage))
        //{
        //    DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[1];
        //}
        //else if (navPageType == typeof(AllAlbumsPage))
        //{
        //    DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[2];
        //}
        //else if (navPageType == typeof(LastFmPage))
        //{
        //    DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[3];
        //}
        //else if (navPageType == typeof(DimmerToolKit))
        //{
        //    DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[4];
        //}
        //else if (navPageType == typeof(SettingsPage))
        //{
        //    DimmerAppSelectorBar.SelectedItem = DimmerAppSelectorBar.Items[5];
        //}
    }

    private void CurrentSongImg_Tapped(object sender, TappedRoutedEventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(NowPlayingPage));
    }

    private void CurrentSongImg_Loaded(object sender, RoutedEventArgs e)
    {
        //MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(curSong =>
        //    {
        //        if (!string.IsNullOrEmpty(curSong.CoverImagePath))
        //        {
        //            var imgSource = new BitmapImage(new Uri(curSong.CoverImagePath));
        //            CurrentSongImg.Source=imgSource;
        //            CurrentSongImg.Visibility = Visibility.Visible;
        //        }
        //        else
        //        {
        //            CurrentSongImg.Visibility = Visibility.Collapsed;
        //            CurrentSongImg.Source = null;
        //        }
        //        if (string.IsNullOrEmpty(curSong.TitleDurationKey))
        //        {
        //            PlaybackSection.Visibility = Visibility.Collapsed;
        //        }
        //        else
        //        {
        //            PlaybackSection.Visibility =Visibility.Visible;
        //        }
        //    });
    }

    private void ArtistsBtn_Loaded(object sender, RoutedEventArgs e)
    {
        //if (string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.TitleDurationKey)) return;

        //MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(curSong =>
        //    {

        //        if (string.IsNullOrEmpty(MyViewModel.CurrentPlayingSongView.TitleDurationKey)) return;
        //        if (MyViewModel.CurrentPlayingSongView.ArtistToSong.Count > 1)
        //        {
        //            MenuFlyout mFlyout = new MenuFlyout();
        //            foreach (var art in MyViewModel.CurrentPlayingSongView.ArtistToSong)
        //            {
        //                if (art is null) continue;
        //                MenuFlyoutItem newItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem()
        //                    ;
        //                newItem.Text = art.Name;
        //                newItem.Click += NewItem_Click;
        //                newItem.CommandParameter = art;
        //                mFlyout.Items.Add(newItem);
        //            }

        //            ArtistsBtn.ContextFlyout = mFlyout;

        //        }
        //    });
      
    }

    private void NewItem_Click(object sender, RoutedEventArgs e)
    {
        var art = ((MenuFlyoutItem)sender).CommandParameter as ArtistModelView;
        if (art is null) return;
        MyViewModel.SetSelectedArtist(art);
        MyViewModel.NavigateToArtistPageWithArtistId(art.Id);
    }

    private void ArtistsBtn_Click(object sender, RoutedEventArgs e)
    {
        
        //if count is <2 nav directly, else show context menu
    }

    private void ViewQueue_Click(object sender, RoutedEventArgs e)
    {
        if (MyViewModel.PlaybackQueue.Count < 1) return;


        MyViewModel.ProcessNowPlayingQueueShowing();
    }

    private void ShowFavSongs_Click(object sender, RoutedEventArgs e)
    {

        //var currText = SearchTextBox.Text;
        //if (string.IsNullOrEmpty(currText))
        //{
        //    SearchTextBox.Text = "my fav";
        //}
        //else
        //{
        //    SearchTextBox.Text += " add my fav";
        //}

    }
    private void MiddlePointer_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props != null)
        {
            if (props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased)
            {
                Debug.WriteLine("Show TQL pane");
            }
        }
    }

    FontIcon CaretSolidUp = new FontIcon() { Glyph = "\uEDD7" };
    FontIcon CaretSolidDown = new FontIcon() { Glyph = "\uEDD8" };
    string lastKey;
    string lastSort;
    private void SortClick(object sender, RoutedEventArgs e)
    {

        var send = sender as RadioMenuFlyoutItem;
        if (send is null) return;
        var key = send.Tag.ToString()?.ToLower();

        if (string.IsNullOrEmpty(key) || send == null)
            return;
        lastKey = key;


        //if is checked then its sorting Desc, now we maintain check and sort desc and updatetext

        bool isSortAsc;
        if (lastKey == key && lastSort == "asc")
        {
            isSortAsc = false;
            lastSort = "desc";

        }
        else if (lastKey == key && lastSort == "desc")
        {
            isSortAsc = true;
            lastSort = "asc";
        }
        else
        {
            isSortAsc = MyViewModel.CurrentTqlQueryUI.Contains("asc", StringComparison.CurrentCultureIgnoreCase);
            lastSort = isSortAsc ? "asc" : "desc";
        }


        switch (key)
        {
            case "title":
                if (isSortAsc)
                    MyViewModel.SearchToTQL(PresetQueries.SortByTitleAsc());
                else
                    MyViewModel.SearchToTQL(PresetQueries.SortByTitleDesc());
                break;
            case "artist":
                if (isSortAsc)
                    MyViewModel.SearchToTQL(PresetQueries.SortByArtistAsc());
                else
                    MyViewModel.SearchToTQL(PresetQueries.SortByArtistDesc());
                break;

            case "album":
                if (isSortAsc)
                    MyViewModel.SearchToTQL(PresetQueries.SortByAlbumAsc());
                else
                    MyViewModel.SearchToTQL(PresetQueries.SortByAlbumDesc());
                break;
            case "dims":
                if (isSortAsc)
                    MyViewModel.SearchToTQL(PresetQueries.SortByDimsAsc());
                else
                    MyViewModel.SearchToTQL(PresetQueries.SortByDimsDesc());
                break;

            default:
                break;
        }

        lastKey = key;



    }


    private void ShowSongWithLyrics_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var currentTQL = "has lyrics add " + MyViewModel.CurrentTqlQuery;
        //SearchTextBox.Text = currentTQL;

    }

    private void ShowFavSongs_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var currentTQL = "my fav add " + MyViewModel.CurrentTqlQuery;
        //SearchTextBox.Text = currentTQL;
    }

    private void ShowSongWithLyrics_Click(object sender, RoutedEventArgs e)
    {

        //var currentTQL = " has lyrics";
        //var currText = SearchTextBox.Text;
        //if (string.IsNullOrEmpty(currText))
        //{
        //    SearchTextBox.Text = currentTQL.TrimStart();
        //}
        //else
        //{
        //    SearchTextBox.Text += currentTQL;
        //}
    }
    private void SearchAutoSuggestBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        //MyViewModel.SearchToTQL(SearchTextBox.Text);

        
    }

    private void ShuffleSongs_Click(object sender, RoutedEventArgs e)
    {
        //var currText = SearchTextBox.Text;
        //if (string.IsNullOrEmpty(currText))
        //{
        //    SearchTextBox.Text = "random";
        //}
    }
    private void SortByWithTQL_Click(SplitButton sender, SplitButtonClickEventArgs args)
    {
        
        //SortByWithTQL.Flyout.ShowAt(sender);
    }

    private async void OpenHelp(object sender, RoutedEventArgs e)
    {
        var dlg = new SearchHelpDialog();
        await dlg.ShowAsync();
    }

    private void ArtistsBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void SongsBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void AlbumsBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void DimsStatsBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void SettingsBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void NvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {

    }

    private void NvSample_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

   
    

    private void SearchBoxEdit_GotFocus(object sender, RoutedEventArgs e)
    {

        QuickTQLCommandsStackPanel.Visibility = Visibility.Visible;
    }

    private void SearchBoxEdit_LostFocus(object sender, RoutedEventArgs e)
    {

        QuickTQLCommandsStackPanel.Visibility = Visibility.Collapsed;
    }

    private void DimmerLiveBtn_Tapped(object sender, TappedRoutedEventArgs e)
    {

    }

    private void SearchBoxEdit_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        MyViewModel.SearchToTQL(SearchBoxEdit.Text);
    }

    private void ScrollToCurrentSong_Tapped(object sender, TappedRoutedEventArgs e)
    {
        MyViewModel.MySongsTableView.ScrollIntoView(MyViewModel.CurrentPlayingSongView);
    }

    private async void SaveTQL_Click(object sender, RoutedEventArgs e)
    {

        StackPanel contStackPanel = new();
        TextBlock tqlQuery = new();
        tqlQuery.Text = $"Query: {SearchBoxEdit.Text}";
        TextBlock nlpQuery = new();
        nlpQuery.Text = $"{NaturalLanguageProcessor.Process}";

        TextBox usrQueryName = new();
        contStackPanel.Children.Add(tqlQuery);
        contStackPanel.Children.Add(nlpQuery);
        contStackPanel.Children.Add(usrQueryName);
        WindowedContentDialog dialog = new()
        {
            Title = "Save Query",
            Content = contStackPanel
            
        };
        ContentDialogResult result = await dialog.ShowAsync(true);

        switch (result)
        {
            case ContentDialogResult.None:
                break;
            case ContentDialogResult.Primary:
                if (string.IsNullOrEmpty(usrQueryName.Text)) return;
               await MyViewModel.AddToPlaylist(usrQueryName.Text, MyViewModel.SearchResults, SearchBoxEdit.Text);
                break;
            case ContentDialogResult.Secondary:
                break;
            default:
                break;
        }
    }

    private void ContentDivider_Loaded(object sender, RoutedEventArgs e)
    {

    }
}