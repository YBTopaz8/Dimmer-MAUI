using System.Numerics;

using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.WinUI;


//using Dimmer.DimmerLive;
//using Dimmer.DimmerSearch;
using Dimmer.WinUI.Views.WinuiPages;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;


using Application = Microsoft.Maui.Controls.Application;
using Border = Microsoft.Maui.Controls.Border;
using Button = Microsoft.Maui.Controls.Button;
using Colors = Microsoft.Maui.Graphics.Colors;
//using Microsoft.UI.Xaml.Controls;
using Label = Microsoft.Maui.Controls.Label;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using Slider = Microsoft.Maui.Controls.Slider;

//using SortOrder = Dimmer.Utilities.SortOrder;
using ToggleMenuFlyoutItem = Microsoft.UI.Xaml.Controls.ToggleMenuFlyoutItem;
using ToolTip = Microsoft.UI.Xaml.Controls.ToolTip;
using View = Microsoft.Maui.Controls.View;



namespace Dimmer.WinUI.Views.MAUIPages;


public partial class HomePage : ContentPage
{

    public BaseViewModelWin MyViewModel { get; internal set; }
    private readonly Compositor _compositor = PlatUtils.MainWindowCompositor;
    public HomePage(BaseViewModelWin vm, IWinUIWindowMgrService windowManagerService)
    {
        InitializeComponent();
        BindingContext = vm;
        MyViewModel = vm;
        windowMgrService = windowManagerService;
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


    private SongModelView? _storedSong;
    private readonly IWinUIWindowMgrService windowMgrService;

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
        await MyViewModel.PlaySong(song, CurrentPage.HomePage);
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
    private DispatcherQueue? GetDispatcherQueue()
    {
#if WINDOWS
        var nativeWindow = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        return nativeWindow?.DispatcherQueue;
#else
    return null;
#endif
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
                await MyViewModel.PlaySong(song, CurrentPage.RecentPage, MyViewModel.TopTrackDashBoard?.Where(s => s is not null).Select(x => x!.Song));
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
        await MyViewModel.PlaySong(song, CurrentPage.HomePage);
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

        var dimmerWindow = IPlatformApplication.Current!
            .Services.GetRequiredService<DimmerWin>()!;

      
        dimmerWindow.NavigateToPage(typeof(SettingsPage));
        
        
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


    private void AllLyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {


        var currentList = e.CurrentSelection as IReadOnlyList<object>;
        var current = currentList?.FirstOrDefault() as Dimmer.Data.ModelView.LyricPhraseModelView;
        if (current != null)
        {
            var pastList = e.PreviousSelection as IReadOnlyList<object>;
            if (pastList.Count > 0 && pastList?[0] is Dimmer.Data.ModelView.LyricPhraseModelView past)
            {
                past?.NowPlayingLyricsFontSize = 25;
                past?.HighlightColor = Microsoft.Maui.Graphics.Colors.White;
            }
            current?.NowPlayingLyricsFontSize = 30;
            current?.HighlightColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;

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

            var label = (Label)sender;
            label.ShowContextMenu();
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

    private void NowPlayingQueueBtnClicked(object sender, EventArgs e)
    {
        MyViewModel.NowPlayingQueueBtnClickedCommand.Execute(null);
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



    private void WinUIArtistChip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var nativeElement = (Microsoft.UI.Xaml.UIElement)sender;
        var properties = e.GetCurrentPoint(nativeElement).Properties;


        var point = e.GetCurrentPoint(nativeElement);

        if (properties.IsRightButtonPressed) //also properties.IsXButton2Pressed for mouse 5
        {
            // --- Source data & guards ---
            var song = MyViewModel?.CurrentPlayingSongView;
            var otherArtistsRaw = song?.OtherArtistsName ?? string.Empty;

            // Parse artists by multiple dividers
            var dividers = new[] { ',', ';', ':', '|' };
            var namesList = otherArtistsRaw
                .Split(dividers, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (namesList.Length == 0)
            {
                // Fallback: allow acting on primary artist if you have it
                if (!string.IsNullOrWhiteSpace(song?.ArtistName))
                    namesList = new[] { song.ArtistName!.Trim() };
                else
                    return; // nothing to show
            }

            // Build flyout
            var flyout = new Microsoft.UI.Xaml.Controls.MenuFlyout();

            // ===== Top info block (non-interactive) =====
            var title = song?.Title ?? "(Unknown song)";
            var album = song?.AlbumName ?? "(Unknown album)";
            var artistLine = namesList.Length == 1 ? namesList[0] : $"{namesList.Length} artists";

            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = $"♪  {title}",
                IsEnabled = false
            });
            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = $"💿  {album}",
                IsEnabled = false
            });
            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = $"👤  {artistLine}",
                IsEnabled = false
            });

            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

            // ===== Build per-artist submenus =====
            foreach (var artistName in namesList)
            {
                var artistRoot = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = $"Artist: {artistName}" };

                // Quick View (internal)
                var quickView = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quick View" };
                quickView.Click += (_, __) => TryVM(a => a.QuickViewArtist(artistName));

                // View By...
                var viewBy = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "View By..." };
                var viewAlbums = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Albums" };
                viewAlbums.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(artistName));
                var viewGenres = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Genres" };
                viewGenres.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(artistName)); // customize

                viewBy.Items.Add(viewAlbums);
                viewBy.Items.Add(viewGenres);

                // Play Songs...
                var play = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Play / Queue" };

                var playInAlbum = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Play Songs In This Album" };
                playInAlbum.Click += (_, __) => TryVM(a => a.PlaySongsByArtistInCurrentAlbum(artistName));

                var playAll = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Play All by Artist" };
                playAll.Click += (_, __) => TryVM(a => a.PlayAllSongsByArtist(artistName));

                var queueAll = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Queue All by Artist" };
                queueAll.Click += (_, __) => TryVM(a => a.QueueAllSongsByArtist(artistName));

                play.Items.Add(playInAlbum);
                play.Items.Add(playAll);
                play.Items.Add(queueAll);

                // Stats (non-interactive)
                var stats = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Stats" };
                var playCount = SafeVM(a => a.GetArtistPlayCount(artistName), 0);
                var isFollowed = SafeVM(a => a.IsArtistFollowed(artistName), false);

                stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Total plays: {playCount}", IsEnabled = false });
                stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Followed: {(isFollowed ? "Yes" : "No")}", IsEnabled = false });

                // Favorite toggle (if supported)
                var favSupported = HasVM(out IArtistActions? actions);
                bool isFav = favSupported && actions!.IsArtistFavorite(artistName);
                var favToggle = new ToggleMenuFlyoutItem { Text = "Favorite", IsChecked = isFav };
                favToggle.Click += (_, __) =>
                {
                    if (HasVM(out var a))
                        a!.ToggleFavoriteArtist(artistName, favToggle.IsChecked);
                };

                // Find On...
                var findOn = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Find On..." };
                findOn.Items.Add(MakeExternalLink("Spotify", $"https://open.spotify.com/search/{Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("YouTube Music", $"https://music.youtube.com/search?q={Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("Bandcamp", $"https://bandcamp.com/search?q={Uri.EscapeDataString(artistName)}&item_type=b"));
                findOn.Items.Add(MakeExternalLink("SoundCloud", $"https://soundcloud.com/search?q={Uri.EscapeDataString(artistName)}"));
                findOn.Items.Add(MakeExternalLink("MusicBrainz", $"https://musicbrainz.org/search?query={Uri.EscapeDataString(artistName)}&type=artist&advanced=0"));
                findOn.Items.Add(MakeExternalLink("Discogs", $"https://www.discogs.com/search/?q={Uri.EscapeDataString(artistName)}&type=artist"));

                // Utilities
                var utils = new Microsoft.UI.Xaml.Controls.MenuFlyoutSubItem { Text = "Utilities" };
                var copyName = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Copy Artist Name" };
                copyName.Click += (_, __) =>
                {
                    var dp = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    dp.SetText(artistName);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dp);

                };

                utils.Items.Add(copyName);

                // Assemble artist root
                artistRoot.Items.Add(quickView);
                artistRoot.Items.Add(viewBy);
                artistRoot.Items.Add(play);
                artistRoot.Items.Add(stats);
                artistRoot.Items.Add(favToggle);
                artistRoot.Items.Add(findOn);
                artistRoot.Items.Add(utils);

                flyout.Items.Add(artistRoot);
            }

            var openArtistPage = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
            {
                Text = "Open Artist Page…"
            };
            openArtistPage.Click += (_, __) => TryVM(a => a.NavigateToArtistPage(namesList[0]));
            flyout.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());
            flyout.Items.Add(openArtistPage);

            // Show at pointer
            try
            {
                // Overload requires FrameworkElement + Point
                flyout.ShowAt(nativeElement, point.Position);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MenuFlyout.ShowAt failed: {ex.Message}");
                // fallback: anchor without position
                //flyout.ShowAt(nativeElement);
            }

            // --- local helpers ---

            bool HasVM(out IArtistActions? a)
            {
                a = MyViewModel as IArtistActions;
                return a != null;
            }

            void TryVM(Action<IArtistActions> action)
            {
                if (MyViewModel is IArtistActions a) action(a);
                else Debug.WriteLine("IArtistActions not implemented on MyViewModel. No-op.");
            }

            T SafeVM<T>(Func<IArtistActions, T> getter, T fallback)
            {
                try
                {
                    if (MyViewModel is IArtistActions a) return getter(a);
                    return fallback;
                }
                catch { return fallback; }
            }

            static Microsoft.UI.Xaml.Controls.MenuFlyoutItem MakeExternalLink(string label, string url)
            {
                var item = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = label };
                item.Click += async (_, __) =>
                {
                    try { await Windows.System.Launcher.LaunchUriAsync(new Uri(url)); }
                    catch (Exception ex) { Debug.WriteLine($"Open link failed: {ex.Message}"); }
                };
                return item;
            }

        }
    }

    bool _initialized;
    private void MyPage_Loaded(object sender, EventArgs e)
    {
        if (!_initialized)
        {
            _initialized = true;
            MyViewModel.InitializeAllVMCoreComponents();

        }
    }

    private void ButtonLoaded(object sender, EventArgs e)
    {
        Button send = (Button)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (native is not null)
        {
            native.PointerPressed += WinUIArtistChip_PointerPressed;

            ElementCompositionPreview.SetIsTranslationEnabled(native, true);

            native.PointerEntered += (s, _) =>
            {
                AnimateHover(native, true);
                send.BorderWidth= 2;
                send.BorderColor = Colors.DarkSlateBlue;

            };
            native.PointerExited += (s, _) =>
            {
                AnimateHover(native, false); 
                send.BorderWidth = 0;
                send.BorderColor = Colors.Transparent;
            };

            //stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Total plays: {playCount}", IsEnabled = false });
            //stats.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = $"Followed: {(isFollowed ? "Yes" : "No")}", IsEnabled = false });

        }
    }

    private void AnimateHover(Microsoft.UI.Xaml.UIElement element, bool isHover)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);

        // scale up / down
        var scaleAnim = _compositor.CreateVector3KeyFrameAnimation();
        scaleAnim.Duration = TimeSpan.FromMilliseconds(250);
        scaleAnim.InsertKeyFrame(1f, isHover
            ? new System.Numerics.Vector3(1.2f)
            : new System.Numerics.Vector3(1f));

        // keep scale centered
        visual.CenterPoint = new System.Numerics.Vector3(
            (float)element.RenderSize.Width / 2,
            (float)element.RenderSize.Height / 2,
            0);

        visual.StartAnimation(nameof(visual.Scale), scaleAnim);

        // OPTIONAL: subtle fade instead of full vanish
        var opacityAnim = _compositor.CreateScalarKeyFrameAnimation();
        opacityAnim.Duration = TimeSpan.FromMilliseconds(250);
        opacityAnim.InsertKeyFrame(1f, isHover ? 1f : 0.85f);   // not 0
        visual.StartAnimation(nameof(visual.Opacity), opacityAnim);
        

    }
    private async void AnimateBorderColor(Border border, bool isHover)
    {
        if (border.Stroke is not Microsoft.Maui.Controls.SolidColorBrush solid)
            return;

        var start = solid.Color;
        var end = isHover ? Colors.DarkSlateBlue : Colors.Transparent;
        const int steps = 20;
        const int durationMs = 200;

        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;

            var color = new Microsoft.Maui.Graphics.Color(
                start.Red + (end.Red - start.Red) * t,
                start.Green + (end.Green - start.Green) * t,
                start.Blue + (end.Blue - start.Blue) * t,
                start.Alpha + (end.Alpha - start.Alpha) * t);

            solid.Color = color;
            await Task.Delay(durationMs / steps);
        }
    }

    private void SetupChainedAnimations(Microsoft.UI.Xaml.UIElement[] buttons)
    {
        for (int i = 1; i < buttons.Length; i++)
        {
            var anim = _compositor.CreateExpressionAnimation(
                "(above.Scale.Y - 1) * 40 + above.Translation.Y + (40 * index)");
            anim.SetExpressionReferenceParameter("above",
                ElementCompositionPreview.GetElementVisual(buttons[i - 1]));
            anim.SetScalarParameter("index", i);
            ElementCompositionPreview.GetElementVisual(buttons[i])
                .StartAnimation("Translation.Y", anim);
        }
    }
    private readonly List<Microsoft.UI.Xaml.UIElement> _borders = [];
    private void EnableTranslation(Microsoft.UI.Xaml.UIElement element)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);
    }

    private void ButtonBorder_Loaded(object sender, EventArgs e)
    {
        Border send = (Border)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (native is not null)
        {

            ElementCompositionPreview.SetIsTranslationEnabled(native, true);

            native.PointerEntered += (s, _) =>
            {
                AnimateHover(native, true);


                //AnimateBorderColor(send, true);
            };
            native.PointerExited += (s, _) =>
            {
                AnimateHover(native, false);
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
        var send = (View)sender;
        var native = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
        if (native is null) return;
        native.PointerEntered += (s, e) =>
        {
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

        };
        native.PointerExited += (s, e2) =>
        {
            var tt = ToolTipService.GetToolTip(native) as ToolTip;
            if (tt != null)
                tt.IsOpen = false;
        };
    }

    private void AudioDevicesButton_Loaded(object sender, EventArgs e)
    {
        
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var platView = send.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (platView is null) return;
        MyViewModel.LoadAllAudioDevices();
        if (MyViewModel.AudioDevices is null) return;
        
            var audioDevicesList = MyViewModel.AudioDevices.Select(x =>
            {
                //IconElement icon = default;
                //if (x.IconString is not null)
                //{
                //    var iconElt = new BitmapIcon
                //    {
                //        UriSource = new Uri(x.IconString),
                //        ShowAsMonochrome = false
                //    };
                //    icon = iconElt;
                //}



                var menFlyOut = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem
                {
                    Text = x.Name,
                    Command = MyViewModel.SetPreferredAudioDeviceCommand,
                    CommandParameter = x
                };
                menFlyOut.MaxHeight = 150;
                //if (icon is not null)
                //{

                //    menFlyOut.Icon = icon;
                //}
                return menFlyOut;
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
