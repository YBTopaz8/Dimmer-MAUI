using Windows.Graphics;

using UiThreads = Dimmer.WinUI.Utils.StaticUtils.UiThreads;

namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{

    public AppUtil(BaseViewModelWin baseViewModelWin, IWinUIWindowMgrService winuimgt)
    {

        BaseViewModelWin = baseViewModelWin;
        this.dimmerMAUIWin ??= baseViewModelWin.MainMAUIWindow;
        this.winUIWindowMgrService = winuimgt;
    }
    Microsoft.Maui.Controls.Window? dimmerMAUIWin;
    IWinUIWindowMgrService winUIWindowMgrService;
    public Shell GetShell()
    {
        return new AppShell(BaseViewModelWin)
        ;
    }
    public Microsoft.Maui.Controls.Window LoadWindow()
    {

        if (this.dimmerWinUI is null)
        {
            dimmerWinUI = winUIWindowMgrService.GetOrCreateUniqueWindow<DimmerWin>(BaseViewModelWin, () => new DimmerWin());

            UiThreads.InitializeWinUIDispatcher(dimmerWinUI!.DispatcherQueue);
        }
        else
        {
            UiThreads.InitializeWinUIDispatcher(dimmerWinUI.DispatcherQueue);
            dimmerMAUIWin = BaseViewModelWin.MainMAUIWindow;

        }
        
        

        if (dimmerWinUI == null)
        {
            throw new Exception("DimmerWin is null");
        }
        dimmerWinUI.LoadWindowAndPassVM(BaseViewModelWin, this);
       
        dimmerWinUI.NavigateToPage(typeof(AllSongsListPage));
        PlatUtils.MoveAndResizeCenter(dimmerWinUI, new Windows.Graphics.SizeInt32() { Height = 1200, Width = 1360 });

            dimmerMAUIWin ??= new DimmerMAUIWin(BaseViewModelWin, this);
        return dimmerMAUIWin;
    }

    private async void MauiWin_Created(object? sender, EventArgs e)
    {
        var mauiWin = sender as Microsoft.Maui.Controls.Window;
        if (mauiWin?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWin)
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(nativeWin);
            WindowId id = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(id);


            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter is not null)
            {
                // Start minimized or hidden
                //presenter.Minimize();

                // OR start invisible
               
                appWindow.Hide();
            }

            // optional: resize before it shows
            appWindow.Resize(new SizeInt32(1080, 1200));
        }
        await Task.Delay(1400);
        var concernedWindow = sender as Microsoft.Maui.Controls.Window;
        if (concernedWindow != null)
        {
            Microsoft.Maui.Controls.Application.Current!.CloseWindow(concernedWindow);
        }
    }

    public DimmerWin? dimmerWinUI { get; set; }
    public BaseViewModelWin BaseViewModelWin { get; }

    public enum SongTransitionAnimation
    {
        Fade,
        Slide,
        Scale,
        Spring
    }
}

