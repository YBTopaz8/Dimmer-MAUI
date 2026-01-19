using CommunityToolkit.Maui.Behaviors;

using Dimmer.WinUI.ViewModel.DimmerLiveWin;
using Dimmer.WinUI.Views.WinuiPages.DimmsSection;
using Dimmer.WinUI.Views.WinuiPages.LastFMSection;






//using Dimmer.DimmerLive;
//using Dimmer.DimmerSearch;
using Microsoft.UI.Xaml.Controls.Primitives;


using Application = Microsoft.Maui.Controls.Application;
using Border = Microsoft.Maui.Controls.Border;
using ButtonM = Microsoft.Maui.Controls.Button;
using ColorsM = Microsoft.Maui.Graphics.Colors;
//using Microsoft.UI.Xaml.Controls;
using Slider = Microsoft.Maui.Controls.Slider;

using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using View = Microsoft.Maui.Controls.View;



namespace Dimmer.WinUI.Views.MAUIPages;


public partial class HomePage : ContentPage
{

    public BaseViewModelWin MyViewModel { get; internal set; }
    private readonly Compositor _compositor = PlatUtils.MainWindowCompositor;
    public HomePage(BaseViewModelWin vm, IWinUIWindowMgrService windowManagerService, LoginViewModelWin LoginVM)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
        windowMgrService = windowManagerService;
        this.loginVM = LoginVM;
        MyViewModel.DumpCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ = InitializeAsync();



        Debug.WriteLine($"[UI VIEW] Bound to ViewModel Instance: {MyViewModel.InstanceId}");

    }

    private async Task InitializeAsync()
    {
        MyViewModel.DumpCommand.Execute(null);
        try
        {
            MyViewModel.CurrentPageContext = CurrentPage.HomePage;
            MyViewModel.CurrentMAUIPage = this;
            

            if (MyViewModel.ShowWelcomeScreen)
            {
                Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
                return;
            }

        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
    private async void ConsolidateDuplicates_Clicked(object sender, EventArgs e)
    {
        MyViewModel.IsAboutToConsolidateDupes = true;
    }

    private void SongCoverImg_Clicked(object sender, EventArgs e)
    {

    }

    private readonly IWinUIWindowMgrService windowMgrService;
    private readonly LoginViewModelWin loginVM;

    //private async void QuickFilterGest_PointerReleased(object sender, PointerEventArgs e)
    //{
    //    var send = (View)sender;

    //    var uiElt = sender as Microsoft.UI.Xaml.UIElement;
    //    var properties = e.PlatformArgs?.PointerRoutedEventArgs.GetCurrentPoint(uiElt).Properties;
    //    if (properties is null) return;
    //    var isRightBtnClicked = properties.IsRightButtonPressed;

    //    if (isRightBtnClicked) return;

    //    //if (properties.IsXButton1Pressed)
    //    //{
    //    //    Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
    //    //}
    //    //else if (properties.IsXButton2Pressed)
    //    //{

    //    //}
    //    var gest = send.GestureRecognizers[0] as PointerGestureRecognizer;
    //    if (gest is null)
    //    {
    //        return;
    //    }
    //    var field = gest.PointerReleasedCommandParameter as string;
    //    var val = gest.PointerPressedCommandParameter as string;
    //    if (field is "artist" && !string.IsNullOrEmpty(val))
    //    {
    //        char[] dividers = new char[] { ',', ';', ':', '|', '-' };

    //        var namesList = val
    //            .Split(dividers, StringSplitOptions.RemoveEmptyEntries) // Split by dividers and remove empty results
    //            .Select(name => name.Trim())                           // Trim whitespace from each name
    //            .ToArray();                                             // Convert to a List
    //        string? selectedArtist= null;
    //        if (namesList.Length > 1)
    //        {
    //            selectedArtist = await Shell.Current.DisplayActionSheet("Select Artist", "Cancel", null, namesList);

    //            if (string.IsNullOrEmpty(selectedArtist) || selectedArtist == "Cancel")
    //            {
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            selectedArtist = namesList[0];
    //        }

    //        PlatUtils.OpenAllSongsWindow(MyViewModel);
    //        MyViewModel.SearchSongSB_TextChanged(TQlStaticMethods.SetQuotedSearch("artist", selectedArtist));

    //        return;

    //    }

    //    PlatUtils.OpenAllSongsWindow(MyViewModel);
    //    MyViewModel.SearchSongSB_TextChanged(TQlStaticMethods.SetQuotedSearch(field, val));


    //    //_windowMgrService.GetOrCreateUniqueWindow(MyViewModel, windowFactory: () => new AllSongsWindow(MyViewModel));

    //}

    private async void PlaySongGestRec_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Microsoft.Maui.Controls.View)sender;
        var song = send.BindingContext as SongModelView;
        if (MyViewModel.PlaybackQueue.Count < 1)
        {
            MyViewModel.SearchSongForSearchResultHolder(">>addnext!");
        }
        await MyViewModel.PlaySongAsync(song, CurrentPage.NowPlayingPage, MyViewModel.PlaybackQueueSource.Items);
        //ScrollToSong_Clicked(sender, e);
    }

    private void CurrPlayingSongGesRec_Tapped(object sender, TappedEventArgs e)
    {
        //var song = e.Parameter as SongModelView;
        //if (song is not null)
        //{
        //    DeviceStaticUtils.SelectedSongOne = song;
        //    await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
        //    return;
        //}

        //switch (e.Parameter)
        //{
        //    case "Alb":
        //        //DeviceStaticUtils.SelectedAlbumOne = song.AlbumId;
        //        //await Shell.Current.GoToAsync(nameof(AlbumPage), true);
        //        return;
        //    default:
        //        break;
        //}
        //if (await MyViewModel.SelectedArtistAndNavtoPage(song))
        //{
        //    await Shell.Current.GoToAsync(nameof(ArtistsPage), true);
        //}
    }
    private void GlobalColView_PointerPressed(object sender, PointerEventArgs e)
    {
        var sendd = (View)sender;

        var nativeElement = sender as Microsoft.UI.Xaml.UIElement;
        var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(nativeElement).Properties;

        if (properties.IsMiddleButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            ScrollToSong_Clicked(sender, e);
            return;


        }


    }
    private void ScrollToSong_Clicked(object sender, EventArgs e)
    {
        try
        {


        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    private void SongCoverImage_Clicked(object sender, EventArgs e)
    {

    }








    private void SelectedSongChip_TouchUp(object sender, EventArgs e)
    {

    }

    private async void PlaySongTapFromRecent_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Border)sender;
        if (send is null) return;
        var stat = send.BindingContext as DimmerStats;
        if (stat != null)
        {
            var song = stat.Song;
            if (song != null)
            {
                if (MyViewModel.PlaybackQueue.Count < 1)
                {
                    MyViewModel.SearchSongForSearchResultHolder(">>addnext!");
                }
                await MyViewModel.PlaySongAsync(song, CurrentPage.RecentPage, MyViewModel.TopTrackDashBoard?.Where(s => s is not null).Select(x => x!.Song));
            }
        }
    }

    private async void PlaySongTapFromPBQueue_Tapped(object sender, TappedEventArgs e)
    {
        var send = (Border)sender;
        var song = send.BindingContext as SongModelView;
        if (MyViewModel.PlaybackQueue.Count < 1)
        {
            MyViewModel.SearchSongForSearchResultHolder(">>addnext!");
        }
        await MyViewModel.PlaySongAsync(song, CurrentPage.HomePage);
    }

    private void PlaySongTap_Tapped(object sender, TappedEventArgs e)
    {

    }

    private void ViewArtistTap_Tapped(object sender, TappedEventArgs e)
    {
        try
        {

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private async void OpenDimmerLiveSettingsChip_Clicked(object sender, EventArgs e)
    {
        //await Shell.Current.GoToAsync(nameof(DimmerLivePage));
    }




    private void DeleteBtn_Clicked(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        var param = send.CommandParameter.ToString();
        MyViewModel.DeleteFolderPath(param);
    }
    private async void AddNewMusicFolder_Clicked(object sender, EventArgs e)
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


    private async void SettingsNavChips_ChipClicked(object sender, EventArgs e)
    {

        MyViewModel.NavigateToAnyPageOfGivenType(typeof(SettingsPage));

    }

    private async void Logintolastfm_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.LoginToLastfm();
    }

    private void FindDuplicatesBtn_Clicked(object sender, EventArgs e)
    {
        //Shell.Current.NavTab.SelectedIndex = NavTab.Items.Count - 1;
    }
    private async void NavigateToSelectedSongPageContextMenuAsync(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(MyViewModel.CurrentPlayingSongView);
    }
    private void TogglePanelClicked(object sender, PointerEventArgs e)
    {
        //var properties = e.PlatformArgs.PointerRoutedEventArgs.GetCurrentPoint(send).Properties;

        //var isXB1Pressed = properties.IsXButton1Pressed;

        //if (properties.IsXButton1Pressed)
        //{
        //    Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
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
        //await Shell.Current.GoToAsync(nameof(LibSanityPage), true);
    }

    private void SfChip_Clicked(object sender, EventArgs e)
    {

    }

    private void SfChip_Clicked_1(object sender, EventArgs e)
    {

    }


    private void ViewNPQ_Clicked(object sender, EventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.CopyAllSongsInNowPlayingQueueToMainSearchResult();

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
            MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", res));

            winMgr.GetOrCreateUniqueWindow<DimmerWin>(MyViewModel, windowFactory: () => new DimmerWin());

            return;
        }

        PlatUtils.OpenAllSongsWindow(MyViewModel);
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch(field, val));

    }


    private void ToggleAppFlyoutState_Clicked(object sender, EventArgs e)
    {
        var currentState = Shell.Current.FlyoutIsPresented;
        if (currentState)
        {
            Shell.Current.FlyoutIsPresented = false;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
            //Shell.Current.FlyoutWidth = 0; // Optionally set width to 0 to hide the flyout completely
        }
        else
        {
            Shell.Current.FlyoutIsPresented = true;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
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

#if WINDOWS

#endif


    private void SetPrefdevice_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var dev = send.BindingContext as AudioOutputDevice;

        if (dev is null)
            return;

        MyViewModel.SetPreferredAudioDevice(dev);
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


    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        try
        {

            MyViewModel.NavigateToAnyPageOfGivenType(typeof(NowPlayingPage));
            return;
          

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async void NowPlayingSong_Clicked(object sender, EventArgs e)
    {

        await MyViewModel.ProcessAndMoveToViewSong(null);

    }



    private async void ShowPlaylistHistory_Clicked(object sender, EventArgs e)
    {
        try
        {

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



    private void OpenLyricsViewOnly_Clicked(object sender, EventArgs e)
    {

        MyViewModel.OpenLyricsPopUpWindow(1);

    }
    SpringVector3NaturalMotionAnimation _springAnimation;

    private void ButtonViewLoaded(object sender, EventArgs e)
    {
        Border send = (Border)sender;

        var winUIBorder = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (winUIBorder is null)
        {
            return;
        }
        winUIBorder.PointerEntered += WinUIBtn_PointerEntered;
        winUIBorder.PointerExited += WinUIBtn_PointerExited;


    }

    private void WinUIBtn_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Microsoft.Maui.Platform.ContentPanel border)
            return;
        var visual = ElementCompositionPreview.GetElementVisual(border);
        CreateOrUpdateSpringAnimation(1.0f);
        visual.StartAnimation("Scale", _springAnimation);

        border.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(0);
    }
    private void WinUIBtn_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Microsoft.Maui.Platform.ContentPanel border)
            return;


        border.FocusVisualPrimaryThickness = new Microsoft.UI.Xaml.Thickness(2);
        border.FocusVisualPrimaryBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkSlateBlue);
        // --- create a stroke animation around the panel ---
        var visual = ElementCompositionPreview.GetElementVisual(border);

        // --- scale animation ---
        CreateOrUpdateSpringAnimation(1.1f);
        visual.StartAnimation("Scale", _springAnimation);
    }


    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        if (_springAnimation == null)
        {
            _springAnimation = _compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Scale";
        }

        _springAnimation.FinalValue = new Vector3(finalValue);
    }


    private void ButtonViewUnLoaded(object sender, EventArgs e)
    {
        Border send = (Border)sender;
        var winUIBorder = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (winUIBorder is null)
        {
            return;
        }

        winUIBorder.PointerEntered -= WinUIBtn_PointerEntered;
        winUIBorder.PointerExited -= WinUIBtn_PointerExited;
    }

    private void QuickFilterBtn_Clicked_2(object sender, EventArgs e)
    {

    }

    bool _initialized;
    private void MyPage_Loaded(object sender, EventArgs e)
    {
        //FrameworkElement? send = (FrameworkElement?)MainScrollView.Handler?.PlatformView;
        //UIElement? sendUIElt = (UIElement?)MainScrollView.Handler?.PlatformView;
       
        if (!_initialized)
        {
            _initialized = true;
            MyViewModel.InitializeAllVMCoreComponents();

            MyViewModel.GetLibState();
            if (MyViewModel.IsLibraryEmpty)
            {
                
                MyViewModel.NavigateToAnyPageOfGivenType(typeof(SettingsPage));

            }
        }
    }

    private void ButtonLoaded(object sender, EventArgs e)
    {
        ButtonM send = (ButtonM)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (native is not null)
        {
            ElementCompositionPreview.SetIsTranslationEnabled(native, true);

            native.PointerEntered += (s, e) =>
            {
                Native_PointerEntered(s, e); 
                send.BorderWidth = 2;
                send.BorderColor = ColorsM.DarkSlateBlue;
            };

            native.PointerExited += (s, e) =>
            {
                Native_PointerExited(s, e); 
                send.BorderWidth = 0;
                send.BorderColor = ColorsM.Transparent;
            };

            //stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Total plays: {playCount}", IsEnabled = false });
            //stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Followed: {(isFollowed ? "Yes" : "No")}", IsEnabled = false });

        }
    }

    private void Native_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var native = sender as Microsoft.UI.Xaml.UIElement;
        if (native is null)
            return;
        PlatUtils.AnimateHoverUIElement(native, true, _compositor);
    }

    private void Native_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        var native = sender as Microsoft.UI.Xaml.UIElement;
        if ( native is null)
            return;
        PlatUtils.AnimateHoverUIElement(native, false, _compositor);
            
    }

    private readonly List<Microsoft.UI.Xaml.UIElement> _borders = [];

    private void ButtonBorder_Loaded(object sender, EventArgs e)
    {
        Border send = (Border)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (native is not null)
        {

            ElementCompositionPreview.SetIsTranslationEnabled(native, true);

            native.PointerEntered += (s, _) =>
            {
               PlatUtils.AnimateHoverUIElement(native, true, _compositor);


                //AnimateBorderColor(send, true);
            };
            native.PointerExited += (s, _) =>
            {
                PlatUtils.AnimateHoverUIElement(native, false, _compositor);
                //AnimateBorderColor(send, false);
            };


        }
    }


    private void Quickalbumsearch_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var val = send.CommandParameter as string;

        PlatUtils.OpenAllSongsWindow(MyViewModel);
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch(val, MyViewModel.CurrentPlayingSongView.AlbumName));

    }

    private async void AddFavoriteRatingToSong_Clicked(object sender, EventArgs e)
    {
        await MyViewModel.AddFavoriteRatingToSong(MyViewModel.CurrentPlayingSongView);
    }


    private void ViewLyricsChip_Clicked(object sender, EventArgs e)
    {

        MyViewModel.OpenLyricsPopUpWindow(1);
        return;
    }

    private void TooltipHSL_Loaded(object sender, EventArgs e)
    {
        var send = (ButtonM)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (native is null) return;
        native.PointerEntered += (s, e) =>
        {
            MyViewModel.GetCurrentAudioDevice();
            var CurrentVolumeAndCurrentDeviceSelected=
            $"Volume: {MyViewModel.DeviceVolumeLevel * 100:0}%\n" +
            $"Device: {MyViewModel.SelectedAudioDevice?.Name?? "Default"}";
            ToolTip volumeToolTip = new()
            {
                Content = CurrentVolumeAndCurrentDeviceSelected,
                Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Top,

            };
            
            ToolTipService.SetToolTip(native, volumeToolTip);
            volumeToolTip.IsOpen = true;

            PlatUtils.AnimateHoverUIElement(native, true, _compositor);

            send.BorderWidth = 2;
            send.BorderColor = ColorsM.DarkSlateBlue;

        };
        native.PointerExited += (s, e2) =>
        {
            var tt = ToolTipService.GetToolTip(native) as ToolTip;
            if (tt != null)
                tt.IsOpen = false; 
         
            PlatUtils.AnimateHoverUIElement(native, false, _compositor);
            
            send.BorderWidth = 0;
            send.BorderColor = ColorsM.Transparent;
           
        };
    }

    private void AudioDevicesButton_Loaded(object sender, EventArgs e)
    {
        
    }

    
    private void ArtistBtn_Clicked(object sender, EventArgs e)
    {

        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", MyViewModel.CurrentPlayingSongView.OtherArtistsName));

    }

    private void AlbumBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("album", MyViewModel.CurrentPlayingSongView.AlbumName));

    }

    private async void NowPlayingQueuePGR_PointerReleased(object sender, PointerEventArgs e)
    {
        var gridSenderAsUIElement = sender as Microsoft.UI.Xaml.UIElement;
        //detect if it's middle click
        var props = e.PlatformArgs?.PointerRoutedEventArgs.GetCurrentPoint(gridSenderAsUIElement).Properties;
        if (props != null)
        {

            if (props.PointerUpdateKind == Microsoft.UI.Input.PointerUpdateKind.MiddleButtonReleased)
            {
                if (MyViewModel.CurrentPlayingSongView is null) return;
                var items = MyViewModel.PlaybackQueueSource.Items;
                var isInItems = items.Contains(MyViewModel.CurrentPlayingSongView);
                if(!isInItems) return;

                //scroll to current song in the now playing queue
            }
        }
    }

    private void MainScrollView_Scrolled(object sender, ScrolledEventArgs e)
    {
        
    }

    private void ViewNPQ_Loaded(object sender, EventArgs e)
    {
        ButtonLoaded(sender, e);
        var senderUIElement = (sender as ButtonM).Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
        if(senderUIElement is null) return;
        ElementCompositionPreview.SetIsTranslationEnabled(senderUIElement, true);

        senderUIElement.PointerEntered += Native_PointerEntered;

        senderUIElement.PointerExited += Native_PointerExited;
    }

 
    private void ViewNPQ_Unloaded(object sender, EventArgs e)
    {
        var native = sender as Microsoft.UI.Xaml.UIElement;
        if (native is null) return;
       
        PlatUtils.AnimateHoverUIElement(native, false, _compositor);

        native.PointerExited -= Native_PointerExited;
        native.PointerEntered -= Native_PointerEntered;
    }

  
    private void ArtistBtn_ClickedFromNPQ(object sender, EventArgs e)
    {

    }

    private void ArtistBtnFromNPQ_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var artistName = send.CommandParameter as string;
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("artist", artistName));

    }

    private void AlbumBtnFromNPQ_Clicked(object sender, EventArgs e)
    {
        var send = (Button)sender;
        var albumName = send.CommandParameter as string;
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("album", albumName));
    }

    private void Button_Loaded(object sender, EventArgs e)
    {

    }

    private void ThemeToggler_Loaded(object sender, EventArgs e)
    {
        ButtonLoaded(sender, e);
        var currentTheme = Application.Current.RequestedTheme;
        MyViewModel.IsDarkModeOn = currentTheme == AppTheme.Dark;
    }

    private void UserLoginClicked(object sender, EventArgs e)
    {
        if(loginVM is not null)
        {
            loginVM.NavigateToProfilePage();
            
        }

    }

    private async void UserLoginClicked_Loaded(object sender, EventArgs e)
    {
        var send = (ImageButton)sender;
        await loginVM.InitAsync();
        if(loginVM.CurrentUserOnline is not null && !string.IsNullOrEmpty(loginVM.CurrentUserOnline.ProfileImagePath))
        {
            send.Source = loginVM.CurrentUserOnline.ProfileImagePath;
        }
    }

    private void ViewFullStatsClicked(object sender, EventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(LibraryStatsPage));
    }

    private void ViewDimmerSection_Clicked(object sender, EventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllDimsView));
    }

    private void ViewLastFM_Clicked(object sender, EventArgs e)
    {
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(LastFmPage));
    }

    private void SongTitlePointerReg_PointerPressed(object sender, PointerEventArgs e)
    {
        MyViewModel.SelectedSong = MyViewModel.CurrentPlayingSongView;
      
            MyViewModel.NavigateToAnyPageOfGivenType(typeof(SongDetailPage));
     
    }

    private void ViewAllSongs_Clicked(object sender, EventArgs e)
    {
        if(MyViewModel.IsLibraryEmpty)
        {
            MyViewModel.ShowWelcomeScreen = true;

            MyViewModel.NavigateToAnyPageOfGivenType(typeof(SettingsPage));
            return;
        }
        MyViewModel.NavigateToAnyPageOfGivenType(typeof(AllSongsListPage));
        MyViewModel.SearchSongForSearchResultHolder(TQlStaticMethods.PresetQueries.DescAdded());

    }

    private void AudioDeviceSwitcherButton_Clicked(object sender, EventArgs e)
    { 
        var send = (View)sender;
        var platView = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (platView is null) return;
        MyViewModel.LoadAllAudioDevices();
        if (MyViewModel.AudioDevices is null) return;

        var audioDevicesList = MyViewModel.AudioDevices.Select(x =>
        {
            if(MyViewModel.SelectedAudioDevice.Id == x.Id)
            {
                var menFlyOut = new Microsoft.UI.Xaml.Controls.ToggleMenuFlyoutItem
                {
                    Text = x.Name,
                    Command = MyViewModel.SetPreferredAudioDeviceCommand,
                    CommandParameter = x
                };
                menFlyOut.IsChecked = true;

                return menFlyOut;
            }
            else
            {

                var menFlyOut = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = x.Name,
                    Command = MyViewModel.SetPreferredAudioDeviceCommand,
                    CommandParameter = x
                };
                return menFlyOut;
            }
            //if (icon is not null)
            //{

            //    menFlyOut.Icon = icon;
            //}
        }).ToList();

        MenuFlyout menu = new MenuFlyout();
        menu.Items.Clear();
        foreach (var item in audioDevicesList)
        {
            
            menu.Items.Add(item);
        }
        FlyoutShowOptions flyoutPlace = new FlyoutShowOptions
        {
            Placement = (FlyoutPlacementMode)PlacementMode.Bottom
        };

        menu.ShowAt(platView, flyoutPlace);

    }

}
