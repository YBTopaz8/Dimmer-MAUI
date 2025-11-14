using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CommunityToolkit.WinUI;

using Microsoft.UI.Xaml;

using Windows.Graphics;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Dimmer.WinUI.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DimmerWin : Window
{
    private BaseViewModelWin baseViewModelWin;
    private AppUtil appUtil;
    IWinUIWindowMgrService? WinUIWindowsMgr;
    public DimmerWin()
    {
        InitializeComponent();
        MyViewModel= IPlatformApplication.Current?.Services.GetService<BaseViewModelWin>();
        WinUIWindowsMgr= IPlatformApplication.Current?.Services.GetService<IWinUIWindowMgrService>();
        NavigateToPage(typeof(AllSongsListPage));

    }
    public async void NavigateToPage(Type pageType)
    {
        if (MyViewModel is not null)
        {

            await DispatcherQueue.EnqueueAsync(() =>
            {
                WinUIWindowsMgr.BringToFront(this);
                ContentFrame.Navigate(pageType, MyViewModel);

            });
        }
    }
    public BaseViewModelWin? MyViewModel { get; internal set; }
    private void DimmerWindowClosed(object sender, WindowEventArgs args)
    {
        WinUIWindowsMgr?.CloseAllWindows();
        this.Closed -= DimmerWindowClosed; 

    }
    public void LoadWindowAndPassVM(BaseViewModelWin baseViewModelWin, AppUtil appUtil)
    {
        this.baseViewModelWin = baseViewModelWin;
        this.appUtil = appUtil;
        
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                return;
            }
            if (MyViewModel is null)
                return;

            //SizeInt32 currentWindowSize = new SizeInt32(1600, 1000);
            //PlatUtils.ResizeNativeWindow(this, currentWindowSize);

            MyViewModel.CurrentWinUIPage = this;
        
        WinUIWindowsMgr?.TrackWindow(this);
       
    }
}
