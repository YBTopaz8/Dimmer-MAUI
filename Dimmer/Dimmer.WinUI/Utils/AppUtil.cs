using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.WinUI.Views;

namespace Dimmer.WinUI.Utils;
public class AppUtil : IAppUtil
{

    public AppUtil(BaseViewModelWin baseViewModelWin)
    {

        BaseViewModelWin=baseViewModelWin;
        this.dimmerWin ??=baseViewModelWin.MainMAUIWindow;

    }

    public Shell GetShell()
    {
        return new AppShell(BaseViewModelWin);
    }
    public Window LoadWindow()
    {
        this.dimmerWin ??=BaseViewModelWin.MainMAUIWindow;
        dimmerWin.LoadWindowAndPassVM(BaseViewModelWin, this);
        dimmerWin ??= dimmerWin;
        

        if (dimmerWin == null)
        {
            throw new Exception("DimmerWin is null");
        }

        dimmerWin.Activate();
        PlatUtils.ResizeNativeWindow(dimmerWin, new Windows.Graphics.SizeInt32() { Height = 1200, Width=1080});


        return new Window();
    }
    public DimmerWin? dimmerWin { get; set; }
    public BaseViewModelWin BaseViewModelWin { get; }

    public enum SongTransitionAnimation
    {
        Fade,
        Slide,
        Scale,
        Spring
    }
}

