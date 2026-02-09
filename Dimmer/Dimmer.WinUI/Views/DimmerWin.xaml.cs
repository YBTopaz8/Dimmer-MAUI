using System.Windows.Controls.Primitives;

using CommunityToolkit.WinUI;

using Microsoft.Extensions.Configuration;
using Microsoft.UI.Composition.SystemBackdrops;

using Windows.Graphics;
using ProgressBar = Microsoft.UI.Xaml.Controls.ProgressBar;
using Slider = Microsoft.UI.Xaml.Controls.Slider;
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
        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        WinUIWindowsMgr = IPlatformApplication.Current?.Services.GetService<IWinUIWindowMgrService>();
        MyViewModel?.MainWindow = this;
        MainGrid.DataContext = MyViewModel;
        TopMediaControlSection.DataContext = MyViewModel;

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
    public async void NavigateToPage(Type pageType, object? OptionalParameter=null)
    {
        if (MyViewModel is not null && OptionalParameter is null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr?.BringToFront(this);
                if(pageType != ContentFrame.CurrentSourcePageType)
                    ContentFrame.Navigate(pageType, MyViewModel);

            });
            MyViewModel.DimmerMultiWindowCoordinator?.SnapAllToHomeAsync();

        }
        if(OptionalParameter is not null)
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

        var typee = BaseViewModel.WindowActivationRequestTypeStatic;
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

    private void SetConfigurationSourceTheme()
    {
        if (this.Content is FrameworkElement fe)
        {
            switch (fe.ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }
    }

    private void DimmerProgressBar_Loaded(object sender, RoutedEventArgs e)
    {

        DimmerProgressBar.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        MyViewModel!.DimmerProgressBarView = (ProgressBar)sender;
    }

    private void DimmerProgressRing_Loaded(object sender, RoutedEventArgs e)
    {

        MyViewModel!.DimmerProgressRingView = (ProgressRing)sender;
    }

    private void DimmerStatusTextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel!.DimmerStatusTextBlockView = (TextBlock)sender;
    }

    private void ClearStatus_Click(object sender, RoutedEventArgs e)
    {
        DimmerStatusPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void TopMediaControlSection_Loaded(object sender, RoutedEventArgs e)
    {
        MyViewModel.WhenPropertyChange(nameof(MyViewModel.CurrentPlayingSongView), v => MyViewModel.CurrentPlayingSongView)
            .Subscribe(v =>
            {
                if (v.TitleDurationKey is null)
                {
                    TopMediaControlSection.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
                else
                {
                    TopMediaControlSection.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                }
            });
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
     
        //    // 2. Hide the Detail View
        SmokeGrid.Visibility = Visibility.Collapsed;
    }

}