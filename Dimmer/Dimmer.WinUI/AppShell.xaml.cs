using CommunityToolkit.Maui.Behaviors;

using Dimmer.DimmerSearch;
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.AlbumsPage;
using Dimmer.WinUI.Views.DimmerLiveUI;
using Dimmer.WinUI.Views.PlaylistPages;
using Dimmer.WinUI.Views.TQLCentric;
using Dimmer.WinUI.Views.WinUIPages;

using Microsoft.UI.Xaml.Media.Animation;

using Vanara.PInvoke;


namespace Dimmer.WinUI;

public partial class AppShell : Shell
{
    public AppShell(BaseViewModelWin baseViewModel)
    {
        InitializeComponent();
        MyViewModel = baseViewModel;

        Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        Routing.RegisterRoute(nameof(SingleSongPage), typeof(SingleSongPage));
        Routing.RegisterRoute(nameof(OnlinePageManagement), typeof(OnlinePageManagement));
        Routing.RegisterRoute(nameof(ArtistsPage), typeof(ArtistsPage));
        Routing.RegisterRoute(nameof(DimmerLivePage), typeof(DimmerLivePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(LibSanityPage), typeof(LibSanityPage));
        Routing.RegisterRoute(nameof(ExperimentsPage), typeof(ExperimentsPage));
        Routing.RegisterRoute(nameof(SocialView), typeof(SocialView));
        Routing.RegisterRoute(nameof(AllArtistsPage), typeof(AllArtistsPage));
        Routing.RegisterRoute(nameof(AllPlaylists), typeof(AllPlaylists));

        Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
        Routing.RegisterRoute(nameof(TqlTutorialPage), typeof(TqlTutorialPage));
        Routing.RegisterRoute(nameof(SessionTransferView), typeof(SessionTransferView));
        Routing.RegisterRoute(nameof(SingleAlbumPage), typeof(SingleAlbumPage));
        Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));

        Routing.RegisterRoute(nameof(DuplicatesMgtWindow), typeof(DuplicatesMgtWindow));
    }


    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        //args.
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        base.OnNavigatedFrom(args);
    }
    protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
    {
        base.OnNavigatingFrom(args);
    }

    //protected override void OnNavigated(ShellNavigatedEventArgs args)
    //{



    //    // Get the animation service via the DI container
    //    var animationService = this.Handler?.MauiContext?.Services.GetService<IAnimationService>();
    //    if (animationService == null)
    //    {
    //        base.OnNavigated(args);
    //        return;
    //    }

    //    // Determine the target page type
    //    var targetPage = args.Current.Location.OriginalString;
    //    var targetPageType = Routing.GetRoute(args.Current);

    //    if (targetPageType == null)
    //    {
    //        base.OnNavigate(args);
    //        return;
    //    }

    //    // Load the animation profile for this specific page type
    //    var animationProfile = AnimationManager.GetPageAnimations(targetPageType, animationService);

    //    // Get the correct NavigationTransitionInfo object
    //    NavigationTransitionInfo transitionInfo;
    //    if (args.IsPopping)
    //    {
    //        transitionInfo = animationProfile.PopExit.TransitionInfo;
    //    }
    //    else
    //    {
    //        transitionInfo = animationProfile.PushEnter.TransitionInfo;
    //    }

    //    // If it's a special HomePage navigation, override with those settings
    //    if (targetPageType == typeof(HomePage)) // Assuming HomePage is the name of your page
    //    {
    //        transitionInfo = args.IsPopping
    //            ? animationService.GetHomePagePopExitAnimation().TransitionInfo
    //            : animationService.GetHomePagePushEnterAnimation().TransitionInfo;
    //    }

    //    // Finally, call the base navigation method, but provide our custom transition info!
    //    base.OnNavigate(args, transitionInfo);
    //}
    protected override void OnAppearing()
    {
        base.OnAppearing();

        MyViewModel = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;
        this.BindingContext = MyViewModel;

        MyViewModel.InitializeAllVMCoreComponentsAsync();

    }

    public BaseViewModelWin MyViewModel { get; internal set; }
    private void SidePaneChip_Clicked(object sender, EventArgs e)
    {

        var send = (SfChip)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "Artists":

                break;

            default:
                break;
        }

    }

    private async void OpenDimmerLiveSettingsChip_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(DimmerLivePage));
    }

    private void SettingsChip_Clicked(object sender, EventArgs e)
    {

        var winMgr = IPlatformApplication.Current!.Services.GetService<IMauiWindowManagerService>()!;

        winMgr.GetOrCreateUniqueWindow(() => new SettingWin(MyViewModel));
        //await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void NavTab_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        if (e.NewIndex == 1)
        {
            await MyViewModel.LoadUserLastFMInfo();
        }
    }


    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddMusicFolderViaPickerAsync();
    }

    private void FirstTimeTabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {

    }

    private void NavBtnClicked_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var param = send.CommandParameter.ToString();
        switch (param)
        {
            case "0":
                break;
            case "1":
                break;
            default:

                break;
        }

    }

    private void ShowBtmSheet_Clicked(object sender, EventArgs e)
    {
    }

    private void SettingsNavChips_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.Chips.SelectionChangedEventArgs e)
    {

    }
    private CancellationTokenSource _lyricsCts;
    private bool _isLyricsProcessing = false;


    private async void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {
        if(Shell.Current.CurrentPage.GetType() == typeof(SettingsPage))
        {
            return;
        }
        this.IsBusy = true;
        await Shell.Current.GoToAsync(nameof(SettingsPage));
        this.IsBusy = false;
    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.LoginToLastfm();
    }

    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
        //this.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }
    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(null);
    }
    private void TogglePanelClicked(object sender, PointerEventArgs e)
    {
        //var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(send).Properties;

        //var isXB1Pressed = properties.IsXButton1Pressed;

        //if (properties.IsXButton1Pressed)
        //{
        //    this.FlyoutIsPresented = !this.FlyoutIsPresented;
        //}
        //else if (properties.IsXButton2Pressed)
        //{

        //}

    }


    private void ChangeFolder_Clicked(object sender, EventArgs e)
    {

    }

    private async void FindDupes_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LibSanityPage), true);
    }

    private void SfChip_Clicked(object sender, EventArgs e)
    {

    }

    private void SfChip_Clicked_1(object sender, EventArgs e)
    {

    }

    private async void QuickSearchSfChip_Clicked(object sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var send = (SfChip)sender;
        var field = send.CommandParameter as string;
        if (field is null)
            return;
        string val = string.Empty;
        if (field is "artist")
        {
            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = MyViewModel.CurrentPlayingSongView.OtherArtistsName
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List
            string res = string.Empty;
            if (namesList.Length > 1)
            {
                res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

                if (string.IsNullOrEmpty(res) || res == "Cancel")
                {
                    return;
                }

            }
            if (namesList.Length == 1)
            {
                res = namesList[0];
            }
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", res));

            return;
        }
        if (field is "title")
        {
            val = MyViewModel.CurrentPlayingSongView.Title;
        }
        if (field is "album")
        {
            val = MyViewModel.CurrentPlayingSongView.AlbumName;
        }
        if (field is "genre")
        {
            val = MyViewModel.CurrentPlayingSongView.GenreName;
        }
        if (field is "len")
        {
            val = MyViewModel.CurrentPlayingSongView.DurationInSeconds.ToString();
        }
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));
        var win = winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));
    }


    private void ViewNPQ_Clicked(object sender, EventArgs e)
    {




    }

    private bool _isThrottling = false;
    private readonly int throttleDelay = 200; // Time in milliseconds

    private async void Slider_DragCompleted(object sender, EventArgs e)
    {
        var send = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel.SeekTrackPosition(send.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private void ViewAllSongsWindow_Clicked(object sender, EventArgs e)
    {

    }

    private async void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var ee = e.PlatformArgs?.PointerRoutedEventArgs.KeyModifiers;

        var send = (Microsoft.Maui.Controls.View)sender;
        PointerGestureRecognizer? gest = send.GestureRecognizers[0] as PointerGestureRecognizer;

        if (gest is null)
        {
            return;
        }
        var field = gest.PointerReleasedCommandParameter as string;
        var val = gest.PointerPressedCommandParameter as string;
        if (field is "artist")
        {
            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = MyViewModel.CurrentPlayingSongView.OtherArtistsName
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List
            string res = string.Empty;
            if (namesList.Length > 1)
            {
                res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

                if (string.IsNullOrEmpty(res) || res == "Cancel")
                {
                    return;
                }

            }
            if (namesList.Length == 1)
            {
                res = namesList[0];
            }
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", res));

            winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));

            return;
        }

        SearchSongSB_Clicked(sender, e);
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));

        var win = winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));
    }

    private void SearchSongSB_Clicked(object sender, EventArgs e)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        var win = winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));

        // move and resize to the center of the screen

        var pres = win?.AppWindow.Presenter;
        //window.SetTitleBar()
        if (pres is OverlappedPresenter p)
        {
            p.IsResizable = true;
            p.SetBorderAndTitleBar(true, true); // Remove title bar and border
            p.IsAlwaysOnTop = false;
        }


        Debug.WriteLine(win?.AppWindow.IsShownInSwitchers);//VERY IMPORTANT FOR WINUI 3 TO SHOW IN TASKBAR

    }
    private void ToggleAppFlyoutState_Clicked(object sender, EventArgs e)
    {
        var currentState = this.FlyoutIsPresented;
        if (currentState)
        {
            this.FlyoutIsPresented = false;
            this.FlyoutBehavior = FlyoutBehavior.Flyout;
            //this.FlyoutWidth = 0; // Optionally set width to 0 to hide the flyout completely
        }
        else
        {
            this.FlyoutIsPresented = true;
            this.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
    }


    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        var current = e.CurrentSelection as Dimmer.Data.ModelView.LyricPhraseModelView;

        var past = e.PreviousSelection as Dimmer.Data.ModelView.LyricPhraseModelView;

        if (past is not null)
        {

            past.NowPlayingLyricsFontSize = 25;


        }

        if (current != null)
        {


            current.NowPlayingLyricsFontSize = 30;

        }

        //AllLyricsColView.ScrollTo(item: current, ScrollToPosition.Start, animate: true);

    }


    private void Slider_Loaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Loaded(object sender, EventArgs e)
    {

    }

    private void VolumeSlider_Unloaded(object sender, EventArgs e)
    {

    }
    private void SfEffectsView_Loaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        var mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged += MainLayout_PointerWheelChanged;
#endif
    }

#if WINDOWS
    private void MainLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(null);

        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        if (mouseWheelDelta != 0)
        {
            if (mouseWheelDelta > 0)
            {
                if (MyViewModel.DeviceVolumeLevel >= 1)
                {
                    return;
                }
                MyViewModel.IncreaseVolumeLevel();
                // Handle scroll up
            }
            else
            {
                if (MyViewModel.DeviceVolumeLevel <= 0)
                {
                    return;
                }

                MyViewModel.DecreaseVolumeLevel();
                // Handle scroll down
            }
        }

        e.Handled = true;
    }

#endif

    private void SfEffectsView_Unloaded(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        Microsoft.UI.Xaml.UIElement? mainLayout = (Microsoft.UI.Xaml.UIElement)send.Handler!.PlatformView!;
        mainLayout.PointerWheelChanged -= MainLayout_PointerWheelChanged;
#endif

    }

    private void MenuFlyoutItem_Clicked(object sender, EventArgs e)
    {

    }

    private void Button_Clicked(object sender, EventArgs e)
    {

    }

    private void SetPrefdevice_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var dev = send.BindingContext as AudioOutputDevice;

        if (dev is null)
            return;

        MyViewModel.SetPreferredAudioDevice(dev);
    }

    private void ShellTabView_SelectionChanging(object sender, Syncfusion.Maui.Toolkit.TabView.SelectionChangingEventArgs e)
    {

    }

    private void ViewDeviceAudio_Clicked(object sender, EventArgs e)
    {
        if (ShellTabView.SelectedIndex == 1)
        {
            ShellTabView.SelectedIndex = 0;
            return;
        }
        ShellTabView.SelectedIndex = 1;
    }

    private async void QuickFilterBtn_Clicked(object sender, EventArgs e)
    {
        try
        {
            var send = (Button)sender;

            var field = "artist";
            var val = send.CommandParameter as string;

            char[] dividers = new char[] { ',', ';', ':', '|', '-' };

            var namesList = MyViewModel.CurrentPlayingSongView.OtherArtistsName
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
                .Select(name => name.Trim())                           // Trim whitespace from each name
                .ToArray();                                             // Convert to a List
            string res = string.Empty;
            if (namesList.Length > 1)
            {
                res = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

                if (string.IsNullOrEmpty(res) || res == "Cancel")
                {
                    return;
                }

            }
            if (namesList.Length == 1)
            {
                res = namesList[0];
            }
            MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("artist", res));
            var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

            var win = winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }

    private void Label_Loaded(object sender, EventArgs e)
    {

        View send = (View)sender;
        var touchBehavior = new TouchBehavior
        {
            HoveredAnimationDuration = 250,
            HoveredAnimationEasing = Easing.CubicOut,
            HoveredBackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue,

            PressedScale = 0.7, // Adjusted for a smoother feel
            PressedAnimationDuration = 300,
            // Add any other customizations here
        };


        send.Behaviors.Add(touchBehavior);
    }

    private void Label_Unloaded(object sender, EventArgs e)
    {
        View send = (View)sender;
        send.Behaviors.Clear();
    }

    private void QuickFilterBtn_Clicked_1(object sender, EventArgs e)
    {

    }

    private void Quickalbumsearch_Clicked(object sender, EventArgs e)
    {

        var send = (SfEffectsView)sender;

        var val = send.TouchUpCommandParameter as string;
        var field = send.TouchDownCommandParameter as string;

        SearchSongSB_Clicked(sender, e);
        MyViewModel.SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch(field, val));


        //win.Activate();
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var label = (Label)sender;
        label.ShowContextMenu();
    }

    private void NowPlayingSong_Clicked(object sender, EventArgs e)
    {


    }



    private async void ShowPlaylistHistory_Clicked(object sender, EventArgs e)
    {
        try
        {

            await GoToAsync(nameof(AllPlaylists));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }

    }

    private void ScrollVPointer_PointerEntered(object sender, PointerEventArgs e)
    {
        //create a resour style to set button behavior and change tint color to myviewmodel.currentdominantcolor

        //var send = (ScrollView)sender;



        //SideBarScrollView.Resources.
        //    Add(  
        //    );
    }

    private void ScrollVPointer_PointerExited(object sender, PointerEventArgs e)
    {

    }

    private void ViewNPQ_TouchUp(object sender, EventArgs e)
    {

    }

    private void NowPlayingQueueGestRecog_PointerReleased(object sender, PointerEventArgs e)
    {
       
    }

    private void MoreBtn_Clicked(object sender, EventArgs e)
    {

    }

    private void MoreIcon_Clicked(object sender, EventArgs e)
    {
        return;
        ImageButton btn = (ImageButton)sender;
        //btn.ShowContextMenu();
        var param = btn.CommandParameter;
        Debug.WriteLine(param);
        Debug.WriteLine(param.GetType());
    }

    private void SelectedSongChip_Clicked(object sender, EventArgs e)
    {

    }

    private void SelectedSongChip_TouchUp(object sender, EventArgs e)
    {
        var send = (SfEffectsView)sender;

        if (send is null)
            return;

        var param = send.TouchUpCommandParameter as SongModelView;

        if (param is null)
            return;


        MyViewModel.SelectedSong = param;

    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {

    }

    private async void AddFavoriteRatingToSong_TouchUp(object sender, EventArgs e)
    {

        await MyViewModel.AddFavoriteRatingToSong(MyViewModel.CurrentPlayingSongView);
    }

    private void AddFavoriteRatingToSong_Loaded(object sender, EventArgs e)
    {

    }

    private void AddFavoriteRatingToSong_Unloaded(object sender, EventArgs e)
    {

    }

    private async void AddFavoriteRatingToSongPtrGest_PointerReleased(object sender, PointerEventArgs e)
    {

        var platEvents = e.PlatformArgs;
        var routedEvents = platEvents.PointerRoutedEventArgs;


        var properties = routedEvents.GetCurrentPoint(sender as Microsoft.UI.Xaml.UIElement).Properties;
        if (properties.IsLeftButtonPressed)
        {
            await MyViewModel.AddFavoriteRatingToSong(MyViewModel.CurrentPlayingSongView);
        }
        if (properties.IsRightButtonPressed)
        {
            await MyViewModel.UnloveSong(MyViewModel.CurrentPlayingSongView);
            return;
        }
    }

    private void AddFavoriteRatingToSong_TouchUp_1(object sender, EventArgs e)
    {

    }

    private void NowPlayingQueueBtnClicked(object sender, EventArgs e)
    {
        MyViewModel.SearchSongSB_TextChanged(MyViewModel.CurrentPlaybackQuery);

       
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;


        var win = winMgr.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));


        return;
    }

    // Section for Songs With UserNotes.


}