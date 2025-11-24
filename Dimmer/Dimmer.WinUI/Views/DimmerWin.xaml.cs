using System.Windows.Controls.Primitives;

using CommunityToolkit.WinUI;

using Windows.Graphics;

using Button = Microsoft.UI.Xaml.Controls.Button;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using Slider = Microsoft.UI.Xaml.Controls.Slider;

using Window = Microsoft.UI.Xaml.Window;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerWin : Window
{
    private AppUtil appUtil;
    IWinUIWindowMgrService? WinUIWindowsMgr;
    public DimmerWin()
    {
        InitializeComponent();
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        WinUIWindowsMgr = IPlatformApplication.Current?.Services.GetService<IWinUIWindowMgrService>();
        MyViewModel?.MainWindow = this;
        MainGrid.DataContext = MyViewModel;
        TopMediaControlSection.DataContext = MyViewModel;

        var appWin = PlatUtils.GetAppWindow(this);
        
        
        _compositorMainGrid = ElementCompositionPreview.GetElementVisual(MainGrid).Compositor;

#if DEBUG
        this.Title = "DimmerDebug";
#endif
        //New window size: 1621 x 749
        appWin.Resize(new SizeInt32(1600, 750));
    }


    public async void NavigateToPage(Type pageType)
    {
        if (MyViewModel is not null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                ContentFrame.Navigate(pageType, MyViewModel);

            });
        }
    }
    public BaseViewModelWin? MyViewModel { get; internal set; }
    private void DimmerWindowClosed(object sender, WindowEventArgs args)
    {
        WinUIWindowsMgr?.UntrackWindow(this);
        this.Closed -= DimmerWindowClosed;

    }
    public void LoadWindowAndPassVM(BaseViewModelWin baseViewModelWin, AppUtil appUtil)
    {
        this.MyViewModel ??= baseViewModelWin;
        this.appUtil = appUtil;

    }

    private async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {

        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            return;
        }
        if (MyViewModel is null)
            return;
        await MyViewModel.CheckToCompleteActivation();
    }

    private readonly Microsoft.UI.Composition.Compositor _compositorMainGrid;
    private void CurrentlyPlayingBtn_Click(object sender, RoutedEventArgs e)
    {

    }

    private void CurrentlyPlayingBtn_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UIControlsAnims.AnimateBtnPointerEntered((Button)sender, _compositorMainGrid);

    }

    private void CurrentlyPlayingBtn_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        UIControlsAnims.AnimateBtnPointerExited((Button)sender, _compositorMainGrid);

    }

    private void CurrentlyPlayingAlbumBtn_Pressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CurrentlyPlayingTitleBtn_Pressed(object sender, PointerRoutedEventArgs e)
    {
        var nativeElement = (UIElement)sender;
        var props = e.GetCurrentPoint(nativeElement).Properties;
        if (props.IsLeftButtonPressed)
        {

        }
        else if (props.IsRightButtonPressed)
        {
            var flyout = new MenuFlyout();
            MyViewModel?.PopulateSongTitleContextMenuFlyout(flyout, MyViewModel.CurrentPlayingSongView);
        }
        else if (props.IsMiddleButtonPressed)
        {
            // Middle button pressed so we fav the song
            MyViewModel?.AddFavoriteRatingToSongCommand.Execute(MyViewModel.CurrentPlayingSongView);
        }
    }

    private void CurrentlyPlayingArtistBtn_Pressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CurrentlyPlayingImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CurrentlyPlayingImage_PointerEntered(object sender, PointerRoutedEventArgs e)
    {

    }

    private void CurrentlyPlayingImage_PointerExited(object sender, PointerRoutedEventArgs e)
    {

    }

    private async void VolumeSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {

        var slider = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        var newVolume = e.NewValue;
        MyViewModel?.SetVolumeLevel(newVolume);


        await Task.Delay(throttleDelay);
        _isThrottling = false;


    }

    private async void CurrentPositionSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        var slider = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        MyViewModel?.SeekTrackPosition(slider.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }
    private bool _isThrottling = false;
    private readonly int throttleDelay = 300; // Time in milliseconds

    private async void CurrentPositionSlider_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var slider = (Slider)sender;
        if (_isThrottling)
            return;

        _isThrottling = true;

        var props = e.GetCurrentPoint((UIElement)sender).Properties;
        if (props.MouseWheelDelta > 0)
        {
            slider.Value += 5; // Increase by 5 seconds
        }
        else
        {
            slider.Value -= 5; // Decrease by 5 seconds
        }

        MyViewModel?.SeekTrackPosition(slider.Value);


        await Task.Delay(throttleDelay);
        _isThrottling = false;
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {

    }
    private void BtnVolume_Click(object sender, RoutedEventArgs e)
    {

    }

    private void BtnDevices_Click(object sender, RoutedEventArgs e)
    {
    }

    private async void TopPanel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var props = e.GetCurrentPoint((UIElement)sender).Properties;

        var isCtrlPressed = Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        if (props.IsMiddleButtonPressed)
        {
            if(!isCtrlPressed)
            {
                MyViewModel?.windowManager.BringToFront(MyViewModel.MainMAUIWindow);
            }
            else
            {
                var currentPageInContentFrame = ContentFrame.Content.GetType();
                if (currentPageInContentFrame == typeof(Views.WinuiPages.AllSongsListPage))
                {
                    var allSongsPage = (Views.WinuiPages.AllSongsListPage?)ContentFrame.Content;
                    if (allSongsPage != null)
                    {

                        allSongsPage.ScrollToSong(MyViewModel!.CurrentPlayingSongView);
                    }
                }
            }
        }

    }

    private void CurrentPositionSlider_ValueChanged_1(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {

    }

    private void PlayPauseBtn_Loaded(object sender, RoutedEventArgs e)
    {
        ToggleButton send=  (ToggleButton)sender;
        
    }

    private void PlayPauseImg_Loaded(object sender, RoutedEventArgs e)
    {
        
       
    }

    private void PlayPauseBtn_Checked(object sender, RoutedEventArgs e)
    {
        string uri = "ms-appx:///Assets/Images/pausecircle.svg";


        //PlayPauseImg.Source = new SvgImageSource(new Uri(uri));

    }

    private void PlayPauseBtn_Unchecked(object sender, RoutedEventArgs e)
    {
        string uri = "ms-appx:///Assets/Images/playcircle.svg";


        //PlayPauseImg.Source = new SvgImageSource(new Uri(uri));



        
    

    }

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        
        Debug.WriteLine($"New window size: {args.Size.Width} x {args.Size.Height}");
    }
}